namespace CareHome.Api.Abstractions;

public interface ICareHomeAccessScope
{
    Task<List<int>?> GetAllowedCareHomeIdsAsync(CancellationToken cancellationToken = default);

    Task<List<int>> GetScopedCareHomeIdsAsync(int tenantId, CancellationToken cancellationToken = default);

    Task<bool> CanAccessCareHomeAsync(int tenantId, int careHomeId, CancellationToken cancellationToken = default);
}
