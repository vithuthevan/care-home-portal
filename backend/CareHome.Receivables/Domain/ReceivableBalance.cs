namespace CareHome.Api.Receivables.Domain;

public sealed record ReceivableAmounts(
    decimal OriginalAmount,
    decimal CreditedAmount,
    decimal PaidAmount,
    decimal OutstandingAmount);

/// <summary>
/// Authoritative receivable balance: gross invoice minus effective credits minus allocated payments.
/// Phase 4 uses real allocations when present; legacy invoice Paid flag applies only when allocated payments are zero.
/// </summary>
public static class ReceivableBalance
{
    public static ReceivableAmounts Calculate(
        decimal grossInvoiceAmount,
        decimal effectiveCredits,
        string storedInvoicePaymentStatus,
        decimal allocatedPayments = 0m)
    {
        var gross = Money.Round(Math.Max(0m, grossInvoiceAmount));
        var credits = Money.Round(Math.Max(0m, effectiveCredits));
        var allocated = Money.Round(Math.Max(0m, allocatedPayments));

        var netBeforePayments = Money.Round(Math.Max(0m, gross - credits));

        var legacyFullyPaid = IsLegacyPaidFlag(storedInvoicePaymentStatus);
        var paid = allocated;
        if (legacyFullyPaid && paid == 0m && netBeforePayments > 0m)
        {
            paid = netBeforePayments;
        }

        paid = Money.Round(Math.Min(paid, netBeforePayments));
        var outstanding = Money.Round(Math.Max(0m, netBeforePayments - paid));

        return new ReceivableAmounts(gross, credits, paid, outstanding);
    }

    public static bool IsLegacyPaidFlag(string storedInvoicePaymentStatus) =>
        string.Equals(storedInvoicePaymentStatus, PaymentStatuses.Paid, StringComparison.Ordinal)
        || string.Equals(storedInvoicePaymentStatus, "Paid", StringComparison.Ordinal);
}
