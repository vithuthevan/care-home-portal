namespace CareHome.Api.Billing;

/// <summary>
/// Pure guards for invoice void so money-path rules stay testable without HTTP.
/// </summary>
public static class InvoiceVoidRules
{
    public static string? ValidateCanVoid(string status, string paymentStatus, bool hasCreditNotes)
    {
        if (status == "Void")
        {
            return "Invoice is already void.";
        }

        if (paymentStatus == "Paid")
        {
            return "A paid invoice cannot be voided. Record a credit note instead, or mark it unpaid first if the payment was recorded in error.";
        }

        if (hasCreditNotes)
        {
            return "This invoice has credit notes and cannot be voided.";
        }

        return null;
    }
}
