using CareHome.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Common;

public static class DatabaseMigrationStartupLogger
{
    public static async Task LogPendingMigrationsAsync(
        CareHomeDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count == 0)
            {
                return;
            }

            logger.LogError(
                "Database schema is behind the application. Pending EF migrations: {PendingMigrations}. "
                + "GET /api/dashboard (when commercial revenue is enabled), accounts receivable, collections, revenue assurance, "
                + "and contract renewals may return HTTP 500 until migrations are applied.",
                string.Join(", ", pending));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not evaluate pending EF migrations at startup.");
        }
    }
}
