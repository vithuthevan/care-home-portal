namespace CareHome.Api.Security;

public interface ITenantContext
{
    int TenantId { get; }

    bool HasTenant { get; }

    Guid? TenantPublicId { get; }

    string? TenantName { get; }
}
