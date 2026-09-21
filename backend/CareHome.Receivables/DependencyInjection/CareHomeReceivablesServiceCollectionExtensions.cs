using CareHome.Api.Receivables.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace CareHome.Api.Receivables.DependencyInjection;

public static class CareHomeReceivablesServiceCollectionExtensions
{
    public static IServiceCollection AddCareHomeReceivables(this IServiceCollection services)
    {
        services.AddScoped<IReceivablesService, ReceivablesService>();
        return services;
    }
}
