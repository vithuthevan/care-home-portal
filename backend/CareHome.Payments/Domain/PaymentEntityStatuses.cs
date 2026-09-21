namespace CareHome.Api.Payments.Domain;

public static class PaymentSources
{
    public const string Manual = "Manual";
    public const string BankImport = "BankImport";
    public const string Remittance = "Remittance";
    public const string PaymentGateway = "PaymentGateway";
}

public static class PaymentEntityStatuses
{
    public const string Received = "Received";
    public const string PartiallyAllocated = "PartiallyAllocated";
    public const string Allocated = "Allocated";
    public const string Reversed = "Reversed";
}
