using Microsoft.AspNetCore.Identity;

namespace CareHome.Api.Security;

public class MustChangePasswordMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && await IsPasswordChangeRequiredAsync(context)
            && !IsAllowedWhilePasswordChangeRequired(context.Request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "You must change your temporary password before you can use the system."
            });
            return;
        }

        await next(context);
    }

    private static async Task<bool> IsPasswordChangeRequiredAsync(HttpContext context)
    {
        // Prefer live DB flag so admin resets bind without waiting for claim-only JWT.
        var userManager = context.RequestServices.GetService<UserManager<ApplicationUser>>();
        var userId = JwtSecurityStamp.GetUserId(context.User);
        if (userManager is not null && !string.IsNullOrEmpty(userId))
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user is not null)
            {
                return user.MustChangePassword;
            }
        }

        return string.Equals(
            context.User.FindFirst(TenantClaimTypes.MustChangePassword)?.Value,
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllowedWhilePasswordChangeRequired(PathString path)
    {
        return path.StartsWithSegments("/api/auth/change-password")
            || path.StartsWithSegments("/api/auth/login-key")
            || path.StartsWithSegments("/api/auth/me");
    }
}
