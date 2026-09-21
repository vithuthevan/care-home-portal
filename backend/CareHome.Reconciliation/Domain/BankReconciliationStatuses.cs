namespace CareHome.Api.Reconciliation.Domain;

public static class BankTransactionStatuses
{
    public const string Unreconciled = "Unreconciled";
    public const string Suggested = "Suggested";
    public const string Reconciled = "Reconciled";
    public const string Ignored = "Ignored";
}

public static class BankImportBatchStatuses
{
    public const string Committed = "Committed";
    public const string Failed = "Failed";
}

public static class PaymentReconciliationStatuses
{
    public const string Confirmed = "Confirmed";
    public const string Reversed = "Reversed";
}

public static class ReconciliationSuggestionStatuses
{
    public const string Active = "Active";
    public const string Superseded = "Superseded";
    public const string Confirmed = "Confirmed";
    public const string Rejected = "Rejected";
}

public static class BankTransactionDirections
{
    public const string In = "In";
    public const string Out = "Out";
}
