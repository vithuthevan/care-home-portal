namespace CareHome.Api.Common;

public static class DefaultInvoiceCategories
{
    public static readonly (string Code, string Name, string Description)[] All =
    [
        ("GENERAL_CARE", "General Care Invoice", "Standard care charges."),
        ("OUTREACH", "Out-Reach Services Invoice", "Outreach service charges."),
        ("RENT", "Rent Invoice", "Client rent charges."),
        ("MISC", "Miscellaneous Invoice", "Other chargeable items.")
    ];

    public const string MiscellaneousCode = "MISC";

    public static bool IsSystemDefaultCode(string code) =>
        All.Any(x => string.Equals(x.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
}
