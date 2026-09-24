using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Security;

/// <summary>
/// Ensures each tenant has the standard starter Sage nominal codes (inserts missing codes only).
/// </summary>
public class TenantNominalCodeSeeder(
    CareHomeDbContext dbContext,
    ILogger<TenantNominalCodeSeeder> logger)
{
    public async Task BackfillAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenantIds = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            await EnsureStarterCodesAsync(tenantId, cancellationToken);
        }
    }

    public async Task EnsureStarterCodesAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var existingCodes = await dbContext.NominalCodes
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);

        var existing = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = 0;

        foreach (var (code, name, description) in DefaultNominalCodes.StarterSet)
        {
            if (existing.Contains(code))
            {
                continue;
            }

            dbContext.NominalCodes.Add(new NominalCode
            {
                TenantId = tenantId,
                Code = code,
                Name = name,
                Description = description,
                IsActive = true
            });
            added++;
        }

        if (added == 0)
        {
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Seeded {Count} starter nominal code(s) for tenant {TenantId}.",
            added,
            tenantId);
    }
}
