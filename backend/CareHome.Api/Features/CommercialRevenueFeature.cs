namespace CareHome.Api.Features;

public sealed class CommercialRevenueFeature
{
    public const string SectionName = "Features";

    public bool CommercialRevenueEnabled { get; set; }

    public static bool IsApiPathBlocked(PathString path, bool enabled)
    {
        if (enabled || !path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return path.StartsWithSegments("/api/receivables", StringComparison.OrdinalIgnoreCase)
               || path.StartsWithSegments("/api/payments", StringComparison.OrdinalIgnoreCase)
               || path.StartsWithSegments("/api/banking", StringComparison.OrdinalIgnoreCase)
               || path.StartsWithSegments("/api/remittances", StringComparison.OrdinalIgnoreCase)
               || path.StartsWithSegments("/api/collections", StringComparison.OrdinalIgnoreCase)
               || path.StartsWithSegments("/api/disputes", StringComparison.OrdinalIgnoreCase)
               || path.StartsWithSegments("/api/revenue-assurance", StringComparison.OrdinalIgnoreCase)
               || path.StartsWithSegments("/api/contract-renewals", StringComparison.OrdinalIgnoreCase)
               || path.StartsWithSegments("/api/finance", StringComparison.OrdinalIgnoreCase);
    }
}
