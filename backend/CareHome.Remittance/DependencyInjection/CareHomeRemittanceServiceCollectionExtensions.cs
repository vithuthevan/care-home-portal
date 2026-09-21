using CareHome.Api.Remittance.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CareHome.Api.Remittance.DependencyInjection;

public static class CareHomeRemittanceServiceCollectionExtensions
{
    public static IServiceCollection AddCareHomeRemittance(this IServiceCollection services)
    {
        services.AddScoped<RemittanceService>();
        return services;
    }
}
