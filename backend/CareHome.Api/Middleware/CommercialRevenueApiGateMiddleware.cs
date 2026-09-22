using CareHome.Api.Features;
using Microsoft.Extensions.Options;

namespace CareHome.Api.Middleware;

public sealed class CommercialRevenueApiGateMiddleware(
    RequestDelegate next,
    IOptions<CommercialRevenueFeature> feature)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (CommercialRevenueFeature.IsApiPathBlocked(
                context.Request.Path,
                feature.Value.CommercialRevenueEnabled))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { message = "Not found." });
            return;
        }

        await next(context);
    }
}
