namespace CareHome.Api.Common;

public static class OptionalContactFields
{
    public static string? NormalizeEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
