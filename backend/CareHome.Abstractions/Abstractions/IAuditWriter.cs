namespace CareHome.Api.Abstractions;

public interface IAuditWriter
{
    Task LogAsync(
        string entityType,
        string? entityId,
        string action,
        object? oldValues,
        object? newValues,
        string? description,
        CancellationToken cancellationToken = default,
        int? tenantId = null);
}
