using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CareHome.Api.Tests.Integration;

public sealed class CareHomeWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        foreach (var (key, value) in IntegrationTestConfiguration.JwtSettings)
        {
            if (value is not null)
            {
                builder.UseSetting(key, value);
            }
        }

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = IntegrationTestDatabase.ConnectionString,
                ["Database:ApplyMigrations"] = "false",
                ["Seed:AdminEmail"] = "",
                ["Seed:AdminPassword"] = "",
                ["Telemetry:EnableConsoleExporter"] = "false",
                ["Features:CommercialRevenueEnabled"] = "true"
            }.Concat(IntegrationTestConfiguration.JwtSettings)
                .ToDictionary(static pair => pair.Key, static pair => pair.Value));
        });
    }
}
