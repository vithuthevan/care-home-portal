using CareHome.Api.Common;
using CareHome.Api.Security;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CareHome.Api.Tests;

public class ProductionHardeningTests
{
    [Fact]
    public void Csv_formula_values_are_neutralized()
    {
        Assert.Equal("'=1+1", CsvFormulaSanitizer.Neutralize("=1+1"));
        Assert.Equal("'+cmd", CsvFormulaSanitizer.Neutralize("+cmd"));
        Assert.Equal("'-1", CsvFormulaSanitizer.Neutralize("-1"));
        Assert.Equal("'@SUM(A1)", CsvFormulaSanitizer.Neutralize("@SUM(A1)"));
        Assert.Equal("Alice Brown", CsvFormulaSanitizer.Neutralize("Alice Brown"));
    }

    [Fact]
    public void Sage_machine_fields_are_not_prefixed()
    {
        Assert.Equal("SAGE001", CsvFormulaSanitizer.CsvField("SAGE001", neutralizeFormula: false));
        Assert.Equal("4000", CsvFormulaSanitizer.CsvField("4000", neutralizeFormula: false));
        Assert.Equal("'=HYPERLINK", CsvFormulaSanitizer.CsvField("=HYPERLINK", neutralizeFormula: true));
    }

    [Fact]
    public void Jwt_key_rejects_missing_placeholder_and_weak_values_outside_development()
    {
        Assert.Throws<InvalidOperationException>(() => JwtSigningKey.Resolve(null, isDevelopment: false));
        Assert.Throws<InvalidOperationException>(() =>
            JwtSigningKey.Resolve(JwtSigningKey.DevelopmentPlaceholder, isDevelopment: false));
        Assert.Throws<InvalidOperationException>(() => JwtSigningKey.Resolve("short-key", isDevelopment: false));
        Assert.Throws<InvalidOperationException>(() =>
            JwtSigningKey.Resolve(new string('a', 40), isDevelopment: false));
    }

    [Fact]
    public void Jwt_development_placeholder_is_allowed_in_development()
    {
        var key = JwtSigningKey.Resolve(JwtSigningKey.DevelopmentPlaceholder, isDevelopment: true);
        Assert.Equal(JwtSigningKey.DevelopmentPlaceholder, key);
    }

    [Fact]
    public void Jwt_strong_key_is_accepted()
    {
        const string key = "prod-unit-test-signing-key-32chars-min!";
        Assert.Equal(key, JwtSigningKey.Resolve(key, isDevelopment: false));
    }

    [Fact]
    public void Production_rejects_localdb_connection_strings()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ProductionStartupValidator.ValidateConnectionString(
                "Server=(localdb)\\MSSQLLocalDB;Database=CareHomeDb;Trusted_Connection=True"));
        Assert.Throws<InvalidOperationException>(() =>
            ProductionStartupValidator.ValidateConnectionString(null));
        ProductionStartupValidator.ValidateConnectionString(
            "Server=sql.example.internal;Database=CareHome;User Id=app;Password=placeholder;TrustServerCertificate=True");
    }

    [Fact]
    public void Production_seed_rejects_known_development_admin()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:AdminEmail"] = "admin@localhost",
                ["Seed:AdminPassword"] = "DevAdmin!12345"
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            ProductionStartupValidator.ValidateProductionSeed(config));
    }

    [Theory]
    [InlineData("/api/auth/login", true)]
    [InlineData("/health/ready", true)]
    [InlineData("/", false)]
    [InlineData("/index.html", false)]
    [InlineData("/main.js", false)]
    public void Security_headers_use_api_csp_only_for_api_and_health(string path, bool expectApiCsp)
    {
        Assert.Equal(expectApiCsp, SecurityHeadersMiddleware.MatchesApiOrHealthPath(path));
    }
}
