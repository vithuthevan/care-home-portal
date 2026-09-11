using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CareHome.Api.Security;

/// <summary>
/// Embeds and validates ASP.NET Identity <see cref="IdentityUser.SecurityStamp"/> in JWTs
/// so password changes and stamp resets invalidate previously issued tokens.
/// </summary>
public static class JwtSecurityStamp
{
    public const string ClaimType = "security_stamp";

    public static Claim CreateClaim(string? securityStamp)
    {
        if (string.IsNullOrEmpty(securityStamp))
        {
            throw new InvalidOperationException("User security stamp is missing.");
        }

        return new Claim(ClaimType, securityStamp);
    }

    public static string? GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

    public static async Task OnTokenValidated(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal is null)
        {
            context.Fail("Missing principal.");
            return;
        }

        var userId = GetUserId(principal);
        if (string.IsNullOrEmpty(userId))
        {
            context.Fail("Token is missing a user id.");
            return;
        }

        var stampFromToken = principal.FindFirstValue(ClaimType);
        if (string.IsNullOrEmpty(stampFromToken))
        {
            context.Fail("Token is missing a security stamp. Sign in again.");
            return;
        }

        var userManager = context.HttpContext.RequestServices
            .GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            context.Fail("User is inactive or was not found.");
            return;
        }

        var currentStamp = await userManager.GetSecurityStampAsync(user);
        if (!string.Equals(currentStamp, stampFromToken, StringComparison.Ordinal))
        {
            context.Fail("Security stamp mismatch.");
        }
    }
}
