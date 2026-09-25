using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Funding.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Funding;

public sealed class FundingContractQuery(CareHomeDbContext dbContext) : IFundingContractQuery
{
    public async Task<bool> HasOverlappingActiveContractAsync(
        int tenantId,
        int clientId,
        int fundingAuthorityId,
        int invoiceCategoryId,
        DateOnly contractStart,
        DateOnly? contractEnd,
        int? excludeContractId,
        CancellationToken cancellationToken = default)
    {
        var others = await dbContext.ClientFundingContracts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId
                && x.ClientId == clientId
                && x.FundingAuthorityId == fundingAuthorityId
                && x.InvoiceCategoryId == invoiceCategoryId
                && x.Status == FundingContractStatuses.Active
                && (excludeContractId == null || x.Id != excludeContractId))
            .Select(x => new { x.ContractStartDate, x.ContractEndDate })
            .ToListAsync(cancellationToken);

        return others.Any(x => FundingContractOverlap.PeriodsOverlap(
            x.ContractStartDate,
            x.ContractEndDate,
            contractStart,
            contractEnd));
    }

    public Task<bool> IsContractUsedOnNonVoidInvoiceAsync(
        int tenantId,
        int contractId,
        CancellationToken cancellationToken = default) =>
        dbContext.InvoiceLines.AnyAsync(
            x => x.ClientFundingContractId == contractId
                && x.Invoice.TenantId == tenantId
                && x.Invoice.Status != InvoiceStatuses.Void,
            cancellationToken);
}
