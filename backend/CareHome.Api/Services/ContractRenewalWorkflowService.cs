using CareHome.Api.Data;
using CareHome.Api.Dtos.FundingContracts;
using CareHome.Api.Dtos.Renewals;
using CareHome.Api.Funding;
using CareHome.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Services;

public sealed class ContractRenewalWorkflowService(
    CareHomeDbContext dbContext,
    FundingContractService fundingContracts,
    TimeProvider timeProvider)
{
    public async Task<RenewalDashboardDto> GetDashboardAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var contracts = await dbContext.ClientFundingContracts.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Status == "Active" && c.ContractEndDate != null)
            .Select(c => new { c.Id, c.ContractEndDate, c.ClientId })
            .ToListAsync(cancellationToken);

        var renewals = await dbContext.FundingContractRenewals.AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return new RenewalDashboardDto
        {
            Expiring30 = contracts.Count(c => c.ContractEndDate <= today.AddDays(30) && c.ContractEndDate >= today),
            Expiring60 = contracts.Count(c => c.ContractEndDate <= today.AddDays(60) && c.ContractEndDate >= today),
            Expiring90 = contracts.Count(c => c.ContractEndDate <= today.AddDays(90) && c.ContractEndDate >= today),
            OverdueRenewals = renewals.Count(r => r.NextActionDate != null && r.NextActionDate < today),
            NegotiationsAwaitingResponse = renewals.Count(r => r.Status == FundingContractRenewalStatuses.Negotiating)
        };
    }

    public async Task<List<RenewalListDto>> ListAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.FundingContractRenewals.AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.CurrentEndDate)
            .Take(200)
            .Select(r => new RenewalListDto
            {
                PublicId = r.PublicId,
                ContractId = r.ContractId,
                CurrentEndDate = r.CurrentEndDate,
                CurrentRate = r.CurrentRate,
                ProposedRate = r.ProposedRate,
                Status = r.Status,
                NextActionDate = r.NextActionDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RenewalListDto> CreateForContractAsync(
        int tenantId,
        int contractId,
        CancellationToken cancellationToken = default)
    {
        var contract = await dbContext.ClientFundingContracts.AsNoTracking()
            .Include(c => c.Rates)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == contractId, cancellationToken)
            ?? throw new InvalidOperationException("Contract not found.");

        var latestRate = contract.Rates.OrderByDescending(r => r.EffectiveFrom).FirstOrDefault();
        var currentRate = latestRate?.Amount ?? 0;
        var now = timeProvider.GetUtcNow();
        var renewal = new FundingContractRenewal
        {
            TenantId = tenantId,
            PublicId = Guid.NewGuid(),
            ContractId = contract.Id,
            CurrentEndDate = contract.ContractEndDate ?? contract.ContractStartDate.AddYears(1),
            CurrentRate = currentRate,
            Status = FundingContractRenewalStatuses.Upcoming,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.FundingContractRenewals.Add(renewal);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RenewalListDto
        {
            PublicId = renewal.PublicId,
            ContractId = renewal.ContractId,
            CurrentEndDate = renewal.CurrentEndDate,
            CurrentRate = renewal.CurrentRate,
            ProposedRate = renewal.ProposedRate,
            Status = renewal.Status,
            NextActionDate = renewal.NextActionDate
        };
    }

    public async Task AgreeAsync(
        int tenantId,
        Guid publicId,
        AgreeRenewalRequest request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var renewal = await dbContext.FundingContractRenewals
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.PublicId == publicId, cancellationToken)
            ?? throw new InvalidOperationException("Renewal not found.");

        if (renewal.Status == FundingContractRenewalStatuses.Completed)
        {
            throw new InvalidOperationException("Renewal is already completed.");
        }

        var contract = await dbContext.ClientFundingContracts.AsNoTracking()
            .Include(c => c.Rates)
            .FirstAsync(c => c.TenantId == tenantId && c.Id == renewal.ContractId, cancellationToken);
        var latestRate = contract.Rates.OrderByDescending(r => r.EffectiveFrom).FirstOrDefault();

        renewal.ProposedRate = request.ProposedRate;
        renewal.EffectiveFrom = request.EffectiveFrom;
        renewal.Status = FundingContractRenewalStatuses.Agreed;
        renewal.AgreedAt = timeProvider.GetUtcNow();
        renewal.UpdatedAt = timeProvider.GetUtcNow();

        var (_, error, _) = await fundingContracts.AddRateAsync(
            tenantId,
            renewal.ContractId,
            new CreateFundingRateRequest
            {
                EffectiveFrom = request.EffectiveFrom,
                Amount = request.ProposedRate,
                Frequency = latestRate?.Frequency ?? "Weekly"
            },
            cancellationToken);

        if (error is not null)
        {
            throw new InvalidOperationException(error.Message);
        }

        renewal.Status = FundingContractRenewalStatuses.Completed;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
