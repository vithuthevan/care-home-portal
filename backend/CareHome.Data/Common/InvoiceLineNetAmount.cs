using CareHome.Api.Models;

namespace CareHome.Api.Common;

/// <summary>
/// Net billed amount for an invoice line after non-void credit note lines (credit amounts are negative).
/// Shared by Sage export, invoice reports, and dashboard summaries.
/// </summary>
public static class InvoiceLineNetAmount
{
    public static decimal FromParts(decimal lineAmount, decimal nonVoidCreditLineSum)
    {
        return Money.Round(lineAmount + nonVoidCreditLineSum);
    }

    public static decimal FromLine(InvoiceLine line)
    {
        var credited = line.CreditNoteLines
            .Where(c => c.CreditNote.Status != CreditNoteStatuses.Void)
            .Sum(c => c.Amount);

        return FromParts(line.LineAmount, credited);
    }

    public static bool IsNonZeroExportable(decimal netAmount) => netAmount != 0m;
}
