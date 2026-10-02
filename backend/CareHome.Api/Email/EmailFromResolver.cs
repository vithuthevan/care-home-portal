using CareHome.Api.Common;
using CareHome.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CareHome.Api.Email;

public class EmailFromResolver(CareHomeDbContext dbContext, IOptions<EmailOptions> emailOptions)
{
    private readonly EmailOptions _defaults = emailOptions.Value;

    public async Task<(string FromAddress, string FromName)> ResolveAsync(
        int? tenantId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId is int id)
        {
            var settings = await dbContext.TenantSettings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == id, cancellationToken);

            var name = string.IsNullOrWhiteSpace(settings?.EmailFromName)
                ? _defaults.FromName
                : settings.EmailFromName.Trim();

            if (!string.IsNullOrWhiteSpace(settings?.EmailFromAddress))
            {
                return (settings.EmailFromAddress.Trim(), name);
            }

            var admin = await (
                from user in dbContext.Users.AsNoTracking()
                join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where user.TenantId == id
                    && user.IsActive
                    && role.Name == AppRoles.TenantAdmin
                    && user.Email != null
                    && user.Email != ""
                orderby user.Email
                select new { user.Email, user.DisplayName }
            ).FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(admin?.Email))
            {
                if (string.IsNullOrWhiteSpace(settings?.EmailFromName)
                    && !string.IsNullOrWhiteSpace(admin.DisplayName))
                {
                    name = admin.DisplayName.Trim();
                }

                return (admin.Email.Trim(), name);
            }

            return (string.Empty, name);
        }

        return (_defaults.FromAddress ?? string.Empty, _defaults.FromName);
    }
}
