using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Auth;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(
        UserManager<ApplicationUser> userManager,
        CareHomeDbContext dbContext,
        IConfiguration configuration,
        IHostEnvironment environment,
        LoginPasswordCipher loginPasswordCipher,
        ILogger<AuthController> logger) : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet("login-key")]
        public ActionResult<LoginPublicKeyDto> LoginKey()
        {
            return Ok(loginPasswordCipher.PublicKey());
        }

        [AllowAnonymous]
        [EnableRateLimiting("login")]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            if (!TryResolvePassword(request, out var password))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var email = request.Email.Trim();
            var user = await userManager.FindByEmailAsync(email);
            if (user?.IsActive is not true)
            {
                logger.LogInformation("Login failed for {Email}: unknown or inactive user", email);
                return Unauthorized(new { message = "Invalid email or password." });
            }

            if (await userManager.IsLockedOutAsync(user))
            {
                logger.LogWarning("Login failed for user {UserId}: account locked", user.Id);
                return Unauthorized(new { message = "Invalid email or password." });
            }

            if (!await userManager.CheckPasswordAsync(user, password))
            {
                await userManager.AccessFailedAsync(user);
                logger.LogInformation("Login failed for user {UserId}: invalid password", user.Id);
                return Unauthorized(new { message = "Invalid email or password." });
            }

            if (user.TenantId is int tenantId)
            {
                var tenantActive = await dbContext.Tenants.AsNoTracking()
                    .Where(x => x.Id == tenantId)
                    .Select(x => (bool?)x.IsActive)
                    .FirstOrDefaultAsync();
                if (tenantActive != true)
                {
                    logger.LogInformation("Login failed for user {UserId}: organisation inactive", user.Id);
                    return Unauthorized(new { message = "Invalid email or password." });
                }
            }

            await userManager.ResetAccessFailedCountAsync(user);
            logger.LogInformation("Login succeeded for user {UserId}", user.Id);
            return Ok(await BuildResponse(user, includeToken: true));
        }

        [HttpPost("change-password")]
        public async Task<ActionResult<AuthResponse>> ChangePassword(ChangePasswordRequest request)
        {
            var user = await userManager.GetUserAsync(User);
            if (user?.IsActive != true)
            {
                return Unauthorized();
            }

            if (!loginPasswordCipher.TryResolve(request.CurrentPasswordCipher, request.CurrentPassword, out var currentPassword)
                || !loginPasswordCipher.TryResolve(request.NewPasswordCipher, request.NewPassword, out var newPassword))
            {
                return BadRequest(new { message = "Current password and new password are required." });
            }

            if (newPassword == currentPassword)
            {
                return BadRequest(new { message = "The new password must be different from the temporary password." });
            }

            if (KnownDevelopmentCredentials.IsForbiddenProductionPassword(newPassword))
            {
                return BadRequest(new { message = "This password is not allowed. Choose a unique password." });
            }

            var changed = await userManager.ChangePasswordAsync(
                user,
                currentPassword,
                newPassword);
            if (!changed.Succeeded)
            {
                return BadRequest(new
                {
                    message = string.Join(" ", changed.Errors.Select(e => e.Description))
                });
            }

            user.MustChangePassword = false;
            await userManager.UpdateAsync(user);
            await userManager.UpdateSecurityStampAsync(user);

            logger.LogInformation("Password changed for user {UserId}", user.Id);
            return Ok(await BuildResponse(user, includeToken: true));
        }

        [HttpGet("me")]
        public async Task<ActionResult<AuthResponse>> Me()
        {
            var user = await userManager.GetUserAsync(User);
            if (user?.IsActive != true)
            {
                return Unauthorized();
            }

            return Ok(await BuildResponse(user, includeToken: false));
        }

        private async Task<AuthResponse> BuildResponse(ApplicationUser user, bool includeToken)
        {
            var roles = (await userManager.GetRolesAsync(user)).ToList();
            if (roles.Contains(AppRoles.SuperAdmin) && !roles.Contains(AppRoles.PlatformAdmin))
            {
                roles.Add(AppRoles.PlatformAdmin);
            }

            if (roles.Contains(AppRoles.PlatformAdmin) && user.TenantId is not null)
            {
                user.TenantId = null;
                await userManager.UpdateAsync(user);
            }

            string? tenantName = null;
            Guid? tenantPublicId = null;
            if (user.TenantId is int tenantId)
            {
                var tenant = await dbContext.Tenants.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == tenantId);
                tenantName = tenant?.Name;
                tenantPublicId = tenant?.PublicId;
            }

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.DisplayName),
                new(ClaimTypes.Email, user.Email ?? ""),
                JwtSecurityStamp.CreateClaim(user.SecurityStamp)
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            if (user.MustChangePassword)
            {
                claims.Add(new Claim(TenantClaimTypes.MustChangePassword, "true"));
            }

            if (user.TenantId is int tid && !roles.Contains(AppRoles.PlatformAdmin))
            {
                claims.Add(new Claim(TenantClaimTypes.TenantId, tid.ToString()));
                if (tenantPublicId is Guid publicId)
                {
                    claims.Add(new Claim(TenantClaimTypes.TenantPublicId, publicId.ToString("D")));
                }

                if (!string.IsNullOrWhiteSpace(tenantName))
                {
                    claims.Add(new Claim(TenantClaimTypes.TenantName, tenantName));
                }
            }

            var careHomeIds = await userManager.Users
                .Where(x => x.Id == user.Id)
                .SelectMany(x => x.CareHomeAccess.Select(a => a.CareHomeId))
                .ToListAsync();

            var response = new AuthResponse
            {
                DisplayName = user.DisplayName,
                Email = user.Email ?? "",
                Roles = roles,
                CareHomeIds = careHomeIds,
                TenantName = tenantName,
                TenantPublicId = tenantPublicId,
                MustChangePassword = user.MustChangePassword
            };

            if (includeToken)
            {
                var key = JwtSigningKey.Resolve(
                    configuration["Jwt:Key"],
                    environment.IsDevelopment());
                var issuer = configuration["Jwt:Issuer"] ?? "CareHomeApi";
                var audience = configuration["Jwt:Audience"] ?? "CareHomeWeb";
                var expiryHours = 8d;
                if (double.TryParse(configuration["Jwt:ExpiryHours"], out var configuredExpiry)
                    && configuredExpiry is >= 1 and <= 12)
                {
                    expiryHours = configuredExpiry;
                }

                var token = new JwtSecurityToken(
                    issuer,
                    audience,
                    claims,
                    expires: DateTime.UtcNow.AddHours(expiryHours),
                    signingCredentials: new SigningCredentials(
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                        SecurityAlgorithms.HmacSha256));
                response.Token = new JwtSecurityTokenHandler().WriteToken(token);
            }

            return response;
        }

        private bool TryResolvePassword(LoginRequest request, out string password)
        {
            return loginPasswordCipher.TryResolve(request.PasswordCipher, request.Password, out password);
        }
    }
}
