namespace CareHome.Api.Common;

public static class Pagination
{
    public static bool IsRequested(int? page, int? pageSize) => page.HasValue || pageSize.HasValue;

    public static (int Page, int PageSize) Normalize(int? page, int? pageSize)
    {
        var normalizedPage = Math.Max(page ?? 1, 1);
        var normalizedSize = Math.Clamp(pageSize ?? 50, 1, 200);
        return (normalizedPage, normalizedSize);
    }
}
