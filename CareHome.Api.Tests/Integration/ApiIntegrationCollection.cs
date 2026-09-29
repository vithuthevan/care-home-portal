using Xunit;

namespace CareHome.Api.Tests.Integration;

[CollectionDefinition(Name)]
public sealed class ApiIntegrationCollection : ICollectionFixture<ApiIntegrationFixture>
{
    public const string Name = "ApiIntegration";
}

public sealed class ApiIntegrationFixture : IAsyncLifetime
{
    public CareHomeWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (!IntegrationTestDatabase.IsAvailable)
        {
            Factory = new CareHomeWebApplicationFactory();
            return;
        }

        await IntegrationTestDatabase.ResetAsync();
        Factory = new CareHomeWebApplicationFactory();
        _ = Factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
