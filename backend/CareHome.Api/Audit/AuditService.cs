using System.Text.Json;
using CareHome.Api.Abstractions;
using CareHome.Api.Data;
using CareHome.Api.Models;

namespace CareHome.Api.Audit;

public class AuditService(
    CareHomeDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    ITenantContext tenantContext) : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public async Task LogAsync(
        string entityType,
        string? entityId,
        string action,
        object? oldValues,
        object? newValues,
        string? description,
        CancellationToken cancellationToken = default,
        int? tenantId = null)
    {
        var resolvedTenantId = tenantId
            ?? (tenantContext.HasTenant ? tenantContext.TenantId : (int?)null);

        if (resolvedTenantId is null)
        {
            return;
        }

        var userId = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
            ?? httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = resolvedTenantId.Value,
            UserId = userId,
            LoggedAt = DateTimeOffset.UtcNow,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions),
            Description = description
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

