using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using CareHome.Api.Data;

namespace CareHome.Api.Tests.Integration;

public static class IntegrationTestDatabase
{
    private static readonly Lazy<bool> SqlAvailableLazy = new(ProbeSql);

    public static bool IsAvailable => SqlAvailableLazy.Value;

    public static string ConnectionString { get; } = BuildConnectionString();

    public static async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<CareHomeDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var db = new CareHomeDbContext(options);
        await db.Database.EnsureDeletedAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }

    private static string BuildConnectionString()
    {
        var fromEnv = Environment.GetEnvironmentVariable("CAREHOME_TEST_SQL");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        var databaseName = $"CareHome_Integration_{Guid.NewGuid():N}";
        return
            $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";
    }

    private static bool ProbeSql()
    {
        try
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
