namespace CareHome.Api.Tests.Integration;

internal static class IntegrationTestConfiguration
{
    public const string JwtKey = "integration-test-signing-key-32chars-min!";
    public const string JwtIssuer = "CareHomeApi";
    public const string JwtAudience = "CareHomeWeb";

    public static IReadOnlyDictionary<string, string?> JwtSettings => new Dictionary<string, string?>
    {
        ["Jwt:Key"] = JwtKey,
        ["Jwt:Issuer"] = JwtIssuer,
        ["Jwt:Audience"] = JwtAudience
    };
}
