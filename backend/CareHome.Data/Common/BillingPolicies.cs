namespace CareHome.Api.Common;

public static class BillingPeriodModes
{
    public const string Manual = "Manual";

    public const string FunderCycle = "FunderCycle";

    public static bool IsFunderCycle(string? mode) =>
        string.Equals(mode, FunderCycle, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? mode) =>
        IsFunderCycle(mode) ? FunderCycle : Manual;
}

public static class InvoiceGroupingModes
{
    public const string PerFunder = "PerFunder";

    public const string PerResident = "PerResident";

    public static bool IsPerResident(string? mode) =>
        string.Equals(mode, PerResident, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? mode) =>
        IsPerResident(mode) ? PerResident : PerFunder;
}
