namespace CareHome.Api.Funding.Contracts;

/// <summary>
/// Read-side contract for billing and future revenue modules. Does not expose EF entities.
/// </summary>
public interface IFundingContractQuery
{
    Task<bool> HasOverlappingActiveContractAsync(
        int tenantId,
        int clientId,
        int fundingAuthorityId,
        int invoiceCategoryId,
        DateOnly contractStart,
        DateOnly? contractEnd,
        int? excludeContractId,
        CancellationToken cancellationToken = default);

    Task<bool> IsContractUsedOnNonVoidInvoiceAsync(
        int tenantId,
        int contractId,
        CancellationToken cancellationToken = default);
}
