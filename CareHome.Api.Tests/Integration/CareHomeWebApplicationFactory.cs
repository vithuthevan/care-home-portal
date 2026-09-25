using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CareHome.Api.Tests.Integration;

public sealed class CareHomeWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = IntegrationTestDatabase.ConnectionString,
                ["Database:ApplyMigrations"] = "false",
                ["Seed:AdminEmail"] = "",
                ["Seed:AdminPassword"] = "",
                ["Jwt:Key"] = "integration-test-signing-key-32chars-min!",
                ["Telemetry:EnableConsoleExporter"] = "false",
                ["Features:CommercialRevenueEnabled"] = "true"
            });
        });
    }
}
