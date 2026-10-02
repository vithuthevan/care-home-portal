namespace CareHome.Api.Funding.Contracts;

/// <summary>
/// Read-side contract for billing and future revenue modules. Does not expose EF entities.
/// </summary>
public interface IFundingContractQuery
{
    /// <summary>
    /// True when this resident already has another active contract for the same
    /// funding authority and the same invoice category. The same authority is
    /// allowed again when the invoice category is different.
    /// </summary>
    Task<bool> HasActiveAuthorityInCategoryAsync(
        int tenantId,
        int clientId,
        int fundingAuthorityId,
        int invoiceCategoryId,
        int? excludeContractId,
        CancellationToken cancellationToken = default);

    Task<bool> IsContractUsedOnNonVoidInvoiceAsync(
        int tenantId,
        int contractId,
        CancellationToken cancellationToken = default);
}
