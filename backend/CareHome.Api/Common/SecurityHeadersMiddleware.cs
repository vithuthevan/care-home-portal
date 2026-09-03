namespace CareHome.Api.Common;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    // API-only CSP for JSON/file endpoints. SPA host CSP for HTML/static assets.
    // See docs/PRODUCTION_CONFIGURATION.md.
    private const string ApiCsp =
        "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    private const string SpaCsp =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; font-src 'self'; connect-src 'self'; " +
        "frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers["Content-Security-Policy"] = IsApiOrHealthPath(context.Request.Path)
                ? ApiCsp
                : SpaCsp;
            return Task.CompletedTask;
        });

        await next(context);
    }

    private static bool IsApiOrHealthPath(PathString path) =>
        MatchesApiOrHealthPath(path);

    /// <summary>True for /api and /health routes that use the API-only CSP.</summary>
    internal static bool MatchesApiOrHealthPath(PathString path) =>
        path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
}
