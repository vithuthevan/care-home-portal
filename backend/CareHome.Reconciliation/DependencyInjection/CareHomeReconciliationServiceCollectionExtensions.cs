using CareHome.Api.Reconciliation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CareHome.Api.Reconciliation.DependencyInjection;

public static class CareHomeReconciliationServiceCollectionExtensions
{
    public static IServiceCollection AddCareHomeReconciliation(this IServiceCollection services)
    {
        services.AddScoped<BankAccountService>();
        services.AddScoped<BankImportService>();
        services.AddScoped<ReconciliationMatchingEngine>();
        services.AddScoped<ReconciliationService>();
        services.AddScoped<BankCsvMappingTemplateService>();
        return services;
    }
}
