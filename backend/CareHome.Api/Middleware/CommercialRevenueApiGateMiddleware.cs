using CareHome.Api.Data;
using CareHome.Api.Features;
using CareHome.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CareHome.Api.Middleware;

public sealed class CommercialRevenueApiGateMiddleware(
    RequestDelegate next,
    IOptions<CommercialRevenueFeature> feature)
{
    public async Task InvokeAsync(HttpContext context, CareHomeDbContext dbContext)
    {
        var enabled = feature.Value.CommercialRevenueEnabled;
        if (enabled
            && int.TryParse(context.User.FindFirst(TenantClaimTypes.TenantId)?.Value, out var tenantId))
        {
            var tenantEnabled = await dbContext.TenantSettings.AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .Select(x => (bool?)x.FinanceModuleEnabled)
                .FirstOrDefaultAsync();
            enabled = tenantEnabled == true;
        }

        if (CommercialRevenueFeature.IsApiPathBlocked(context.Request.Path, enabled))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { message = "Not found." });
            return;
        }

        await next(context);
    }
}
