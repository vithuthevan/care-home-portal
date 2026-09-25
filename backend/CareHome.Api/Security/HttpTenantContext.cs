using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace CareHome.Api.Security;

public static class TenantClaimTypes
{
    public const string TenantId = "tenant_id";

    public const string TenantPublicId = "tenant_public_id";

    public const string TenantName = "tenant_name";

    public const string MustChangePassword = "must_change_password";
}

public class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public int TenantId
    {
        get
        {
            if (!HasTenant)
            {
                throw new InvalidOperationException(
                    "This operation requires an organisation context.");
            }

            return int.Parse(TenantIdValue!);
        }
    }

    public bool HasTenant =>
        int.TryParse(TenantIdValue, out var id) && id > 0;

    public Guid? TenantPublicId
    {
        get
        {
            var value = User?.FindFirst(TenantClaimTypes.TenantPublicId)?.Value;
            return Guid.TryParse(value, out var guid) ? guid : null;
        }
    }

    public string? TenantName =>
        User?.FindFirst(TenantClaimTypes.TenantName)?.Value;

    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    private string? TenantIdValue =>
        User?.FindFirst(TenantClaimTypes.TenantId)?.Value;
}

