using Xunit;

namespace CareHome.Api.Tests.Integration;

[CollectionDefinition(Name)]
public sealed class ApiIntegrationCollection : ICollectionFixture<ApiIntegrationFixture>
{
    public const string Name = "ApiIntegration";
}

public sealed class ApiIntegrationFixture : IAsyncLifetime
{
    public CareHomeWebApplicationFactory Factory { get; } = new();

    public async Task InitializeAsync()
    {
        if (!IntegrationTestDatabase.IsAvailable)
        {
            return;
        }

        await IntegrationTestDatabase.ResetAsync();
        _ = Factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
