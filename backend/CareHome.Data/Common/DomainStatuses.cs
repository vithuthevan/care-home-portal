namespace CareHome.Api.Common;

public static class InvoiceStatuses
{
    public const string Generated = "Generated";
    public const string Sent = "Sent";
    public const string Void = "Void";
}

public static class PaymentStatuses
{
    /// <summary>Legacy value persisted on invoices at generation; AR exposes <see cref="Unpaid"/>.</summary>
    public const string NotPaid = "NotPaid";

    public const string Unpaid = "Unpaid";
    public const string Paid = "Paid";
}

public static class CreditNoteStatuses
{
    public const string Generated = "Generated";
    public const string Void = "Void";
}

public static class FundingContractStatuses
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
}
