namespace CareHome.Api.Common;

public static class DefaultNominalCodes
{
    public static readonly (string Code, string Name, string Description)[] StarterSet =
    [
        ("4000", "Care income", "General care invoice revenue."),
        ("4001", "Outreach income", "Out-reach services invoice revenue."),
        ("4002", "Rent income", "Rent invoice revenue."),
        ("4003", "Miscellaneous income", "Miscellaneous invoice revenue.")
    ];
}
