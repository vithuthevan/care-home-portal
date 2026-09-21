namespace CareHome.Api.Payments.Domain;

/// <summary>
/// Mirrors receivable outstanding logic for allocation validation (keeps Payments independent of Receivables).
/// </summary>
public static class InvoiceAllocationCapacity
{
    public static decimal RemainingCollectible(
        decimal grossInvoiceAmount,
        decimal effectiveCredits,
        string storedInvoicePaymentStatus,
        decimal activeAllocatedPayments)
    {
        var gross = Money.Round(Math.Max(0m, grossInvoiceAmount));
        var credits = Money.Round(Math.Max(0m, effectiveCredits));
        var allocated = Money.Round(Math.Max(0m, activeAllocatedPayments));
        var netBeforePayments = Money.Round(Math.Max(0m, gross - credits));

        var legacyFullyPaid = string.Equals(storedInvoicePaymentStatus, PaymentStatuses.Paid, StringComparison.Ordinal)
            || string.Equals(storedInvoicePaymentStatus, "Paid", StringComparison.Ordinal);

        var paid = allocated;
        if (legacyFullyPaid && paid == 0m && netBeforePayments > 0m)
        {
            paid = netBeforePayments;
        }

        paid = Money.Round(Math.Min(paid, netBeforePayments));
        return Money.Round(Math.Max(0m, netBeforePayments - paid));
    }
}
