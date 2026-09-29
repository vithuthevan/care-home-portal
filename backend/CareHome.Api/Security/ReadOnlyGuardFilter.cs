using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using CareHome.Api.Common;

namespace CareHome.Api.Security;

public class ReadOnlyGuardFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var method = context.HttpContext.Request.Method;
        if (context.HttpContext.Request.Path.StartsWithSegments("/api/auth/change-password"))
        {
            await next();
            return;
        }

        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
        {
            await next();
            return;
        }

        var user = context.HttpContext.User;
        var isReadOnly = user.IsInRole(AppRoles.ReadOnly)
            && !user.IsInRole(AppRoles.PlatformAdmin)
            && !user.IsInRole(AppRoles.SuperAdmin)
            && !user.IsInRole(AppRoles.TenantAdmin)
            && !user.IsInRole(AppRoles.Administrator);

        if (isReadOnly)
        {
            context.Result = new ObjectResult(new
            {
                message = "Read-only users cannot change data."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}

