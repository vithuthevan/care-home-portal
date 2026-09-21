using CareHome.Api.RevenueAssurance.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CareHome.Api.RevenueAssurance.DependencyInjection;

public static class CareHomeRevenueAssuranceServiceCollectionExtensions
{
    public static IServiceCollection AddCareHomeRevenueAssurance(this IServiceCollection services)
    {
        services.AddScoped<RevenueAssuranceService>();
        return services;
    }
}
