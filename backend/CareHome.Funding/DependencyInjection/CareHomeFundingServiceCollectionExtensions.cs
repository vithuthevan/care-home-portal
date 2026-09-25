using CareHome.Api.Funding;
using CareHome.Api.Funding.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace CareHome.Api.Funding.DependencyInjection;

public static class CareHomeFundingServiceCollectionExtensions
{
    public static IServiceCollection AddCareHomeFunding(this IServiceCollection services)
    {
        services.AddScoped<IFundingContractQuery, FundingContractQuery>();
        services.AddScoped<FundingContractService>();
        return services;
    }
}
