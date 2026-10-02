using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Funding.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Funding;

public sealed class FundingContractQuery(CareHomeDbContext dbContext) : IFundingContractQuery
{
    public Task<bool> HasActiveAuthorityInCategoryAsync(
        int tenantId,
        int clientId,
        int fundingAuthorityId,
        int invoiceCategoryId,
        int? excludeContractId,
        CancellationToken cancellationToken = default) =>
        dbContext.ClientFundingContracts
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId
                    && x.ClientId == clientId
                    && x.FundingAuthorityId == fundingAuthorityId
                    && x.InvoiceCategoryId == invoiceCategoryId
                    && x.Status == FundingContractStatuses.Active
                    && (excludeContractId == null || x.Id != excludeContractId),
                cancellationToken);

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
