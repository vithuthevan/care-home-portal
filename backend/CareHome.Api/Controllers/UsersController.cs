using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Users;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [RequireTenant]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.Administrator}")]
    public class UsersController(
        UserManager<ApplicationUser> userManager,
        CareHomeDbContext dbContext,
        AuditService audit,
        ITenantContext tenantContext,
        LoginPasswordCipher loginPasswordCipher) : ControllerBase
    {
        private static readonly string[] AssignableRoles =
        [
            AppRoles.TenantAdmin,
            AppRoles.Administrator,
            AppRoles.LocationManager,
            AppRoles.ReadOnly
        ];

        [HttpGet]
        public async Task<ActionResult> List(int? page = null, int? pageSize = null)
        {
            var tenantId = tenantContext.TenantId;
            var query = userManager.Users
                .Include(x => x.CareHomeAccess)
                .Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.Email);

            if (!Pagination.IsRequested(page, pageSize))
            {
                var allUsers = await query.ToListAsync();
                var allDtos = new List<UserDto>();
                foreach (var user in allUsers)
                {
                    allDtos.Add(await ToDto(user));
                }

                return Ok(allDtos);
            }

            var (p, ps) = Pagination.Normalize(page, pageSize);
            var total = await query.CountAsync();
            var pageUsers = await query.Skip((p - 1) * ps).Take(ps).ToListAsync();
            var items = new List<UserDto>();
            foreach (var user in pageUsers)
            {
                items.Add(await ToDto(user));
            }

            return Ok(new PagedResult<UserDto>
            {
                Items = items,
                TotalCount = total,
                Page = p,
                PageSize = ps
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> Get(string id)
        {
            var user = await FindTenantUser(id);
            return user is null ? NotFound() : Ok(await ToDto(user));
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
        {
            if (!AssignableRoles.Contains(request.Role))
            {
                return BadRequest(new { message = "Invalid role." });
            }

            var homesError = await ValidateHomes(request.Role, request.CareHomeIds);
            if (homesError is not null)
            {
                return homesError;
            }

            if (!loginPasswordCipher.TryResolve(request.PasswordCipher, request.Password, out var password))
            {
                return BadRequest(new { message = "Password is required." });
            }

            if (KnownDevelopmentCredentials.IsForbiddenProductionPassword(password))
            {
                return BadRequest(new { message = "This password is not allowed. Choose a unique password." });
            }

            var user = new ApplicationUser
            {
                TenantId = tenantContext.TenantId,
                UserName = request.Email.Trim(),
                Email = request.Email.Trim(),
                DisplayName = request.DisplayName.Trim(),
                EmailConfirmed = true,
                IsActive = true,
                MustChangePassword = true
            };

            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                return BadRequest(new { message = string.Join(" ", created.Errors.Select(e => e.Description)) });
            }

            await userManager.AddToRoleAsync(user, request.Role);
            await ReplaceHomes(user.Id, request.CareHomeIds);
            await audit.LogAsync("User", user.Id, "Create", null, new { user.Email, request.Role }, "Created user.");
            return CreatedAtAction(nameof(Get), new { id = user.Id }, await ToDto(user));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserDto>> Update(string id, UpdateUserRequest request)
        {
            var user = await FindTenantUser(id);
            if (user is null)
            {
                return NotFound();
            }

            if (!AssignableRoles.Contains(request.Role))
            {
                return BadRequest(new { message = "Invalid role." });
            }

            var currentUserId = userManager.GetUserId(User);
            if (string.Equals(currentUserId, id, StringComparison.Ordinal)
                && (!request.IsActive || request.Role is AppRoles.LocationManager or AppRoles.ReadOnly))
            {
                return BadRequest(new { message = "You cannot deactivate your own account or remove your own administrator access." });
            }

            var homesError = await ValidateHomes(request.Role, request.CareHomeIds);
            if (homesError is not null)
            {
                return homesError;
            }

            user.DisplayName = request.DisplayName.Trim();
            user.IsActive = request.IsActive;
            await userManager.UpdateAsync(user);

            var roles = await userManager.GetRolesAsync(user);
            await userManager.RemoveFromRolesAsync(user, roles);
            await userManager.AddToRoleAsync(user, request.Role);
            await ReplaceHomes(id, request.CareHomeIds);
            // Invalidate existing JWTs so role claims refresh on next login.
            await userManager.UpdateSecurityStampAsync(user);
            await audit.LogAsync("User", id, "Update", null, request, "Updated user.");
            return Ok(await ToDto(user));
        }

        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(string id)
        {
            var user = await FindTenantUser(id);
            if (user is null)
            {
                return NotFound();
            }

            var currentUserId = userManager.GetUserId(User);
            if (string.Equals(currentUserId, id, StringComparison.Ordinal))
            {
                return BadRequest(new { message = "You cannot deactivate your own account." });
            }

            user.IsActive = false;
            await userManager.UpdateAsync(user);
            await userManager.UpdateSecurityStampAsync(user);
            await audit.LogAsync("User", id, "Deactivate", null, null, "Deactivated user.");
            return NoContent();
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(string id, ResetPasswordRequest request)
        {
            var user = await FindTenantUser(id);
            if (user is null)
            {
                return NotFound();
            }

            if (!loginPasswordCipher.TryResolve(request.NewPasswordCipher, request.NewPassword, out var newPassword))
            {
                return BadRequest(new { message = "A new password is required." });
            }

            if (KnownDevelopmentCredentials.IsForbiddenProductionPassword(newPassword))
            {
                return BadRequest(new { message = "This password is not allowed. Choose a unique password." });
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });
            }

            user.MustChangePassword = true;
            await userManager.UpdateAsync(user);
            await audit.LogAsync("User", id, "ResetPassword", null, null, "Admin reset password.");
            return NoContent();
        }

        private async Task<ApplicationUser?> FindTenantUser(string id)
        {
            return await userManager.Users
                .Include(x => x.CareHomeAccess)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);
        }

        private async Task<ActionResult?> ValidateHomes(string role, List<int> careHomeIds)
        {
            if (role == AppRoles.LocationManager && careHomeIds.Count == 0)
            {
                return BadRequest(new { message = "Location managers must be assigned at least one care home." });
            }

            if (careHomeIds.Count == 0)
            {
                return null;
            }

            var tenantId = tenantContext.TenantId;
            var validCount = await dbContext.CareHomes.CountAsync(x =>
                x.TenantId == tenantId && careHomeIds.Contains(x.Id));

            if (validCount != careHomeIds.Distinct().Count())
            {
                return BadRequest(new { message = "One or more care homes are not in this organisation." });
            }

            return null;
        }

        private async Task ReplaceHomes(string userId, List<int> careHomeIds)
        {
            var existing = dbContext.UserCareHomeAccess.Where(x => x.UserId == userId);
            dbContext.UserCareHomeAccess.RemoveRange(existing);
            foreach (var homeId in careHomeIds.Distinct())
            {
                dbContext.UserCareHomeAccess.Add(new UserCareHomeAccess { UserId = userId, CareHomeId = homeId });
            }

            await dbContext.SaveChangesAsync();
        }

        private async Task<UserDto> ToDto(ApplicationUser user)
        {
            var roles = await userManager.GetRolesAsync(user);
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                DisplayName = user.DisplayName,
                IsActive = user.IsActive,
                Roles = roles.ToList(),
                CareHomeIds = user.CareHomeAccess.Select(x => x.CareHomeId).ToList()
            };
        }
    }
}

