using CareHome.Api.Billing;
using Microsoft.Extensions.DependencyInjection;

namespace CareHome.Api.Billing.DependencyInjection;

public static class CareHomeBillingServiceCollectionExtensions
{
    public static IServiceCollection AddCareHomeBilling(this IServiceCollection services)
    {
        services.AddScoped<RateCalculator>();
        services.AddScoped<InvoiceTemplateResolver>();
        services.AddScoped<BillingService>();
        services.AddScoped<CreditNoteService>();
        return services;
    }
}
