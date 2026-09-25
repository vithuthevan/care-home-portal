namespace CareHome.Api.Abstractions;

/// <summary>
/// Read-only view of active payment allocations for receivables and invoice enrichment.
/// </summary>
public interface IAllocatedPaymentQuery
{
    /// <summary>
    /// Sum of non-reversed allocations on non-reversed payments per invoice.
    /// Missing invoice IDs return no entry (treat as zero).
    /// </summary>
    Task<IReadOnlyDictionary<int, decimal>> GetActiveAllocatedAmountsForInvoicesAsync(
        int tenantId,
        IReadOnlyCollection<int> invoiceIds,
        CancellationToken cancellationToken = default);
}
