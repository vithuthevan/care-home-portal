using CareHome.Api.Abstractions;
using CareHome.Api.Data;
using CareHome.Api.Models;
using CareHome.Api.RevenueAssurance.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.RevenueAssurance.Services;

public sealed class RevenueAssuranceService(
    CareHomeDbContext dbContext,
    IAuditWriter audit,
    TimeProvider timeProvider)
{
    public async Task<RevenueAssuranceDashboardDto> GetDashboardAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var open = await dbContext.RevenueAssuranceFindings.AsNoTracking()
            .Where(f => f.TenantId == tenantId && f.Status == RevenueAssuranceFindingStatuses.Open)
            .ToListAsync(cancellationToken);

        return new RevenueAssuranceDashboardDto
        {
            OpenFindings = open.Count,
            CriticalFindings = open.Count(f => f.Severity == RevenueAssuranceSeverities.Critical),
            PotentialLeakage = Money.Round(open.Sum(f => f.EstimatedImpact ?? 0)),
            ByRule = open.GroupBy(f => f.RuleCode)
                .Select(g => new RuleCountDto { RuleCode = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList()
        };
    }

    public async Task<List<RevenueAssuranceFindingDto>> ListFindingsAsync(
        int tenantId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.RevenueAssuranceFindings.AsNoTracking()
            .Where(f => f.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(f => f.Status == status);
        }

        return await query
            .OrderByDescending(f => f.DetectedAt)
            .Take(200)
            .Select(f => new RevenueAssuranceFindingDto
            {
                PublicId = f.PublicId,
                RuleCode = f.RuleCode,
                Severity = f.Severity,
                Status = f.Status,
                EstimatedImpact = f.EstimatedImpact,
                Explanation = f.Explanation,
                DetectedAt = f.DetectedAt,
                InvoiceId = f.InvoiceId,
                CareHomeId = f.CareHomeId,
                ClientId = f.ClientId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> RunScanAsync(int tenantId, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var detectedAt = timeProvider.GetUtcNow();
        var created = 0;

        created += await DetectActiveResidentsWithoutInvoiceAsync(tenantId, detectedAt, cancellationToken);
        created += await DetectExpiredContractStillBillingAsync(tenantId, detectedAt, cancellationToken);
        created += await DetectContractsNearingExpiryWithoutRenewalAsync(tenantId, detectedAt, cancellationToken);

        await audit.LogAsync(
            "RevenueAssuranceScan",
            tenantId.ToString(),
            "REVENUE_ASSURANCE_SCAN",
            null,
            new { created },
            "Revenue assurance scan completed.",
            cancellationToken,
            tenantId);

        return created;
    }

    public async Task ResolveFindingAsync(
        int tenantId,
        Guid publicId,
        ResolveFindingRequest request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var finding = await dbContext.RevenueAssuranceFindings
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.PublicId == publicId, cancellationToken)
            ?? throw new InvalidOperationException("Finding not found.");

        finding.Status = request.Status ?? RevenueAssuranceFindingStatuses.Resolved;
        finding.ResolutionNotes = request.Notes?.Trim();
        finding.ResolvedAt = timeProvider.GetUtcNow();
        finding.ResolvedBy = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> DetectActiveResidentsWithoutInvoiceAsync(
        int tenantId,
        DateTimeOffset detectedAt,
        CancellationToken cancellationToken)
    {
        var periodStart = DateOnly.FromDateTime(detectedAt.UtcDateTime.AddDays(-35));
        var periodEnd = DateOnly.FromDateTime(detectedAt.UtcDateTime);

        var residents = await dbContext.Clients.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Status == "Current" && !c.IsArchived)
            .Select(c => new { c.Id, c.CareHomeId, c.FirstName, c.LastName })
            .ToListAsync(cancellationToken);

        var billedClientIds = await dbContext.InvoiceLines.AsNoTracking()
            .Where(l => l.Invoice.TenantId == tenantId
                        && l.Invoice.InvoiceDate >= periodStart
                        && l.Invoice.InvoiceDate <= periodEnd
                        && l.Invoice.Status != InvoiceStatuses.Void)
            .Select(l => l.ClientId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var billed = billedClientIds.ToHashSet();
        var count = 0;
        foreach (var resident in residents.Where(r => !billed.Contains(r.Id)))
        {
            if (await FindingExistsAsync(tenantId, "ACTIVE_RESIDENT_NOT_BILLED", resident.Id, null, cancellationToken))
            {
                continue;
            }

            dbContext.RevenueAssuranceFindings.Add(new RevenueAssuranceFinding
            {
                TenantId = tenantId,
                PublicId = Guid.NewGuid(),
                CareHomeId = resident.CareHomeId,
                ClientId = resident.Id,
                RuleCode = "ACTIVE_RESIDENT_NOT_BILLED",
                Severity = RevenueAssuranceSeverities.High,
                Status = RevenueAssuranceFindingStatuses.Open,
                Explanation =
                    $"Resident {resident.FirstName} {resident.LastName} is current but has no invoice dated between {periodStart:yyyy-MM-dd} and {periodEnd:yyyy-MM-dd}.",
                DetectedAt = detectedAt
            });
            count++;
        }

        if (count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return count;
    }

    private async Task<int> DetectExpiredContractStillBillingAsync(
        int tenantId,
        DateTimeOffset detectedAt,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(detectedAt.UtcDateTime);
        var expired = await dbContext.ClientFundingContracts.AsNoTracking()
            .Where(c => c.TenantId == tenantId
                        && c.Status == "Active"
                        && c.ContractEndDate != null
                        && c.ContractEndDate < today)
            .Select(c => new { c.Id, c.ClientId, EndDate = c.ContractEndDate, HomeId = c.Client.CareHomeId })
            .ToListAsync(cancellationToken);

        var count = 0;
        foreach (var contract in expired)
        {
            var recentInvoice = await dbContext.Invoices.AsNoTracking()
                .AnyAsync(
                    i => i.TenantId == tenantId
                         && i.InvoiceDate >= contract.EndDate
                         && i.Lines.Any(l => l.ClientId == contract.ClientId),
                    cancellationToken);
            if (!recentInvoice)
            {
                continue;
            }

            if (await FindingExistsAsync(tenantId, "CONTRACT_EXPIRED_BILLING_CONTINUES", contract.ClientId, contract.Id, cancellationToken))
            {
                continue;
            }

            dbContext.RevenueAssuranceFindings.Add(new RevenueAssuranceFinding
            {
                TenantId = tenantId,
                PublicId = Guid.NewGuid(),
                CareHomeId = contract.HomeId,
                ClientId = contract.ClientId,
                FundingContractId = contract.Id,
                RuleCode = "CONTRACT_EXPIRED_BILLING_CONTINUES",
                Severity = RevenueAssuranceSeverities.Critical,
                Status = RevenueAssuranceFindingStatuses.Open,
                Explanation =
                    $"Funding contract ended {contract.EndDate:yyyy-MM-dd} but billing invoices exist after that date.",
                DetectedAt = detectedAt
            });
            count++;
        }

        if (count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return count;
    }

    private async Task<int> DetectContractsNearingExpiryWithoutRenewalAsync(
        int tenantId,
        DateTimeOffset detectedAt,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(detectedAt.UtcDateTime);
        var horizon = today.AddDays(60);
        var expiring = await dbContext.ClientFundingContracts.AsNoTracking()
            .Where(c => c.TenantId == tenantId
                        && c.Status == "Active"
                        && c.ContractEndDate != null
                        && c.ContractEndDate >= today
                        && c.ContractEndDate <= horizon)
            .Select(c => new { c.Id, c.ClientId, EndDate = c.ContractEndDate, HomeId = c.Client.CareHomeId })
            .ToListAsync(cancellationToken);

        var renewalContractIds = await dbContext.FundingContractRenewals.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.Status != FundingContractRenewalStatuses.Completed)
            .Select(r => r.ContractId)
            .ToListAsync(cancellationToken);
        var hasRenewal = renewalContractIds.ToHashSet();

        var count = 0;
        foreach (var contract in expiring.Where(c => !hasRenewal.Contains(c.Id)))
        {
            if (await FindingExistsAsync(tenantId, "CONTRACT_EXPIRING_NO_RENEWAL", contract.ClientId, contract.Id, cancellationToken))
            {
                continue;
            }

            dbContext.RevenueAssuranceFindings.Add(new RevenueAssuranceFinding
            {
                TenantId = tenantId,
                PublicId = Guid.NewGuid(),
                CareHomeId = contract.HomeId,
                ClientId = contract.ClientId,
                FundingContractId = contract.Id,
                RuleCode = "CONTRACT_EXPIRING_NO_RENEWAL",
                Severity = RevenueAssuranceSeverities.Medium,
                Status = RevenueAssuranceFindingStatuses.Open,
                Explanation = $"Funding contract expires on {contract.EndDate:yyyy-MM-dd} with no renewal workflow started.",
                DetectedAt = detectedAt
            });
            count++;
        }

        if (count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return count;
    }

    private async Task<bool> FindingExistsAsync(
        int tenantId,
        string ruleCode,
        int clientId,
        int? contractId,
        CancellationToken cancellationToken)
    {
        return await dbContext.RevenueAssuranceFindings.AsNoTracking()
            .AnyAsync(
                f => f.TenantId == tenantId
                     && f.RuleCode == ruleCode
                     && f.ClientId == clientId
                     && f.FundingContractId == contractId
                     && f.Status != RevenueAssuranceFindingStatuses.Resolved
                     && f.Status != RevenueAssuranceFindingStatuses.Ignored,
                cancellationToken);
    }
}
