using CareHome.Api.Data;
using CareHome.Api.Dtos.Finance;
using CareHome.Api.Models;
using CareHome.Api.Payments.Domain;
using CareHome.Api.Receivables.Contracts;
using CareHome.Api.Receivables.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Services;

public sealed class FinanceAttentionService(
    CareHomeDbContext dbContext,
    IReceivablesService receivables,
    TimeProvider timeProvider)
{
    public async Task<FinanceAttentionDto> GetAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var ar = await receivables.GetTenantSummaryAsync(tenantId, new ReceivableInvoiceQuery(), cancellationToken);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var unallocated = await dbContext.Payments.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.Status != PaymentEntityStatuses.Reversed)
            .Select(p => new
            {
                p.Amount,
                Allocated = p.Allocations.Where(a => !a.IsReversed).Sum(a => a.AllocatedAmount)
            })
            .ToListAsync(cancellationToken);

        var unallocatedCash = Money.Round(unallocated.Sum(p => p.Amount - p.Allocated));

        var unmatchedRemittances = await dbContext.RemittanceBatches.AsNoTracking()
            .CountAsync(
                b => b.TenantId == tenantId
                     && b.Status != RemittanceBatchStatuses.Confirmed
                     && b.Status != RemittanceBatchStatuses.Failed,
                cancellationToken);

        var contractsExpiring60 = await dbContext.ClientFundingContracts.AsNoTracking()
            .CountAsync(
                c => c.TenantId == tenantId
                     && c.Status == "Active"
                     && c.ContractEndDate != null
                     && c.ContractEndDate >= today
                     && c.ContractEndDate <= today.AddDays(60),
                cancellationToken);

        var leakage = await dbContext.RevenueAssuranceFindings.AsNoTracking()
            .Where(f => f.TenantId == tenantId && f.Status == RevenueAssuranceFindingStatuses.Open)
            .SumAsync(f => f.EstimatedImpact ?? 0, cancellationToken);

        var disputed = await dbContext.InvoiceDisputes.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.Status != InvoiceDisputeStatuses.Resolved && d.Status != InvoiceDisputeStatuses.Closed)
            .SumAsync(d => d.DisputedAmount, cancellationToken);

        var billingReview = await dbContext.RevenueAssuranceFindings.AsNoTracking()
            .CountAsync(
                f => f.TenantId == tenantId
                     && f.Status == RevenueAssuranceFindingStatuses.Open
                     && f.RuleCode == "ACTIVE_RESIDENT_NOT_BILLED",
                cancellationToken);

        var openDisputes = await dbContext.InvoiceDisputes.AsNoTracking()
            .CountAsync(
                d => d.TenantId == tenantId && d.Status != InvoiceDisputeStatuses.Resolved && d.Status != InvoiceDisputeStatuses.Closed,
                cancellationToken);

        return new FinanceAttentionDto
        {
            ReceivablesOverdue = ar.TotalOverdue,
            Ageing90Plus = ar.Ageing.Days90Plus,
            UnallocatedCash = unallocatedCash,
            UnmatchedRemittances = unmatchedRemittances,
            ContractsExpiringIn60Days = contractsExpiring60,
            PotentialRevenueLeakage = Money.Round(leakage),
            OpenDisputesAmount = Money.Round(disputed),
            OpenDisputesCount = openDisputes,
            ResidentsRequiringBillingReview = billingReview
        };
    }
}
