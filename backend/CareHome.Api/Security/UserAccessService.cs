using CareHome.Api.Abstractions;
using CareHome.Api.Common;
using CareHome.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Security
{
    public class UserAccessService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        CareHomeDbContext dbContext) : ICareHomeAccessScope
    {
        public bool IsPlatformOperator
        {
            get
            {
                var user = httpContextAccessor.HttpContext?.User;
                if (user is null)
                {
                    return false;
                }

                return user.IsInRole(AppRoles.PlatformAdmin)
                    || user.IsInRole(AppRoles.SuperAdmin);
            }
        }

        public bool IsTenantWideAccess
        {
            get
            {
                var user = httpContextAccessor.HttpContext?.User;
                if (user is null)
                {
                    return false;
                }

                return user.IsInRole(AppRoles.TenantAdmin)
                    || user.IsInRole(AppRoles.Administrator)
                    || user.IsInRole(AppRoles.ReadOnly);
            }
        }

        public bool IsReadOnly
        {
            get
            {
                var user = httpContextAccessor.HttpContext?.User;
                if (user is null)
                {
                    return false;
                }

                return user.IsInRole(AppRoles.ReadOnly)
                    && !user.IsInRole(AppRoles.PlatformAdmin)
                    && !user.IsInRole(AppRoles.SuperAdmin)
                    && !user.IsInRole(AppRoles.TenantAdmin)
                    && !user.IsInRole(AppRoles.Administrator);
            }
        }

        public async Task<List<int>?> GetAllowedCareHomeIdsAsync(
            CancellationToken cancellationToken = default)
        {
            if (IsTenantWideAccess)
            {
                return null;
            }

            var userId = userManager.GetUserId(httpContextAccessor.HttpContext?.User
                ?? throw new InvalidOperationException("No authenticated user."));

            if (string.IsNullOrEmpty(userId))
            {
                return [];
            }

            return await dbContext.UserCareHomeAccess
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.CareHomeId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<int>> GetScopedCareHomeIdsAsync(
            int tenantId,
            CancellationToken cancellationToken = default)
        {
            var allowed = await GetAllowedCareHomeIdsAsync(cancellationToken);
            var query = dbContext.CareHomes
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId);

            if (allowed is not null)
            {
                query = query.Where(x => allowed.Contains(x.Id));
            }

            return await query.Select(x => x.Id).ToListAsync(cancellationToken);
        }

        public async Task<bool> CanAccessCareHomeAsync(
            int tenantId,
            int careHomeId,
            CancellationToken cancellationToken = default)
        {
            var belongs = await dbContext.CareHomes.AnyAsync(
                x => x.Id == careHomeId && x.TenantId == tenantId,
                cancellationToken);

            if (!belongs)
            {
                return false;
            }

            var allowed = await GetAllowedCareHomeIdsAsync(cancellationToken);
            return allowed?.Contains(careHomeId) != false;
        }
    }
}

