using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CareHome.Api.Tests.Integration;

public static class IntegrationTestAuth
{
    public static async Task<(ApplicationUser User, string Token)> CreateTenantUserAsync(
        IServiceProvider services,
        Tenant tenant,
        string role,
        string email,
        string password = "IntegrationTest!Pass1",
        IReadOnlyList<int>? careHomeIds = null)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = email,
            IsActive = true,
            TenantId = tenant.Id
        };

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, role);

        if (careHomeIds is { Count: > 0 })
        {
            foreach (var homeId in careHomeIds)
            {
                db.UserCareHomeAccess.Add(new UserCareHomeAccess
                {
                    UserId = user.Id,
                    CareHomeId = homeId
                });
            }

            await db.SaveChangesAsync();
        }

        user = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException("User not found after create.");

        var token = CreateToken(user, tenant, role, user.SecurityStamp);
        return (user, token);
    }

    public static async Task<string> CreatePlatformAdminTokenAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        const string email = "platform-integration@carehome.test";
        const string password = "IntegrationTest!Pass1";

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is null)
        {
            existing = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = "Platform Integration",
                IsActive = true,
                TenantId = null
            };
            var created = await userManager.CreateAsync(existing, password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(existing, AppRoles.PlatformAdmin);
        }

        return CreateToken(existing, tenant: null, AppRoles.PlatformAdmin, existing.SecurityStamp);
    }

    private static string CreateToken(
        ApplicationUser user,
        Tenant? tenant,
        string role,
        string? securityStamp)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, role),
            JwtSecurityStamp.CreateClaim(securityStamp ?? user.SecurityStamp)
        };

        if (tenant is not null)
        {
            claims.Add(new Claim(TenantClaimTypes.TenantId, tenant.Id.ToString()));
            claims.Add(new Claim(TenantClaimTypes.TenantPublicId, tenant.PublicId.ToString("D")));
            claims.Add(new Claim(TenantClaimTypes.TenantName, tenant.Name));
        }

        const string key = "integration-test-signing-key-32chars-min!";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "CareHomeApi",
            audience: "CareHomeWeb",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
