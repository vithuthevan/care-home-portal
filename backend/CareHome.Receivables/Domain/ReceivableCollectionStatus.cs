namespace CareHome.Api.Receivables.Domain;

/// <summary>
/// Collection-facing payment status derived from balances and due dates (not stored on the invoice).
/// </summary>
public static class ReceivableCollectionStatuses
{
    public const string Unpaid = "Unpaid";
    public const string PartiallyPaid = "PartiallyPaid";
    public const string Paid = "Paid";
    public const string Overdue = "Overdue";

    public static string Resolve(ReceivableAmounts amounts, DateOnly dueDate, DateOnly asOfDate)
    {
        if (amounts.OutstandingAmount <= 0m)
        {
            return Paid;
        }

        if (amounts.PaidAmount > 0m)
        {
            return PartiallyPaid;
        }

        if (dueDate < asOfDate)
        {
            return Overdue;
        }

        return Unpaid;
    }
}
