namespace CareHome.Api.Receivables.Domain;

/// <summary>
/// Transition helper: invoices marked Paid before Phase 4 are treated as fully settled
/// until a real payment allocation exists (allocated amount &gt; 0).
/// </summary>
public static class LegacyInvoicePaymentCompatibility
{
    public static bool Applies(string storedInvoicePaymentStatus, decimal activeAllocatedPayments) =>
        ReceivableBalance.IsLegacyPaidFlag(storedInvoicePaymentStatus) && activeAllocatedPayments == 0m;
}
