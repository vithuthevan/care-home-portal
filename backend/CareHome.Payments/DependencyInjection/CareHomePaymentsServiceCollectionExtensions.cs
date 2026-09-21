using CareHome.Api.Abstractions;
using CareHome.Api.Payments.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CareHome.Api.Payments.DependencyInjection;

public static class CareHomePaymentsServiceCollectionExtensions
{
    public static IServiceCollection AddCareHomePayments(this IServiceCollection services)
    {
        services.AddScoped<PaymentService>();
        services.AddScoped<IAllocatedPaymentQuery, AllocatedPaymentQuery>();
        return services;
    }
}
