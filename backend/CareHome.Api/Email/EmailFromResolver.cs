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
            if (settings is not null)
            {
                var address = string.IsNullOrWhiteSpace(settings.EmailFromAddress)
                    ? _defaults.FromAddress
                    : settings.EmailFromAddress.Trim();
                var name = string.IsNullOrWhiteSpace(settings.EmailFromName)
                    ? _defaults.FromName
                    : settings.EmailFromName.Trim();
                if (!string.IsNullOrWhiteSpace(address))
                {
                    return (address, name);
                }
            }
        }

        return (_defaults.FromAddress ?? string.Empty, _defaults.FromName);
    }
}
