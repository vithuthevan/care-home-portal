namespace CareHome.Api.Common;

public interface ITenantOwned
{
    int TenantId { get; set; }
}

public static class TenantQuery
{
    public static IQueryable<T> ForTenant<T>(this IQueryable<T> query, int tenantId)
        where T : class, ITenantOwned
    {
        return query.Where(x => x.TenantId == tenantId);
    }
}
