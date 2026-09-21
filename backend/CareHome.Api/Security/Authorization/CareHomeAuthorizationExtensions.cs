using CareHome.Api.Common;
using Microsoft.AspNetCore.Authorization;

namespace CareHome.Api.Security.Authorization;

public static class CareHomeAuthorizationExtensions
{
    public static IServiceCollection AddCareHomeAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(CareHomePolicies.PlatformOnly, policy =>
                policy.RequireRole(AppRoles.PlatformAdmin, AppRoles.SuperAdmin));

            options.AddPolicy(CareHomePolicies.CanManageOrganisation, policy =>
                policy.RequireRole(AppRoles.TenantAdmin, AppRoles.Administrator));

            options.AddPolicy(CareHomePolicies.CanViewAudit, policy =>
                policy.RequireRole(AppRoles.TenantAdmin, AppRoles.Administrator));

            options.AddPolicy(CareHomePolicies.CanManageCareHome, policy =>
                policy.RequireRole(
                    AppRoles.TenantAdmin,
                    AppRoles.Administrator,
                    AppRoles.LocationManager));

            options.AddPolicy(CareHomePolicies.CanManageBilling, policy =>
                policy.RequireRole(
                    AppRoles.TenantAdmin,
                    AppRoles.Administrator,
                    AppRoles.LocationManager));

            options.AddPolicy(CareHomePolicies.CanManageFunding, policy =>
                policy.RequireRole(
                    AppRoles.TenantAdmin,
                    AppRoles.Administrator,
                    AppRoles.LocationManager));

            options.AddPolicy(CareHomePolicies.CanManageReceivables, policy =>
                policy.RequireRole(
                    AppRoles.TenantAdmin,
                    AppRoles.Administrator,
                    AppRoles.LocationManager));

            options.AddPolicy(CareHomePolicies.CanViewReceivables, policy =>
                policy.RequireRole(
                    AppRoles.TenantAdmin,
                    AppRoles.Administrator,
                    AppRoles.LocationManager,
                    AppRoles.ReadOnly));

            options.AddPolicy(CareHomePolicies.CanViewFinancialReports, policy =>
                policy.RequireRole(
                    AppRoles.TenantAdmin,
                    AppRoles.Administrator,
                    AppRoles.LocationManager,
                    AppRoles.ReadOnly));

            options.AddPolicy(CareHomePolicies.CanManagePayments, policy =>
                policy.RequireRole(
                    AppRoles.TenantAdmin,
                    AppRoles.Administrator,
                    AppRoles.LocationManager));

            options.AddPolicy(CareHomePolicies.CanViewPayments, policy =>
                policy.RequireRole(
                    AppRoles.TenantAdmin,
                    AppRoles.Administrator,
                    AppRoles.LocationManager,
                    AppRoles.ReadOnly));

            options.AddPolicy(CareHomePolicies.CanManageBanking, policy =>
                policy.RequireRole(
                    AppRoles.TenantAdmin,
                    AppRoles.Administrator,
                    AppRoles.LocationManager));

            options.AddPolicy(CareHomePolicies.CanViewBanking, policy =>
                policy.RequireRole(
                    AppRoles.TenantAdmin,
                    AppRoles.Administrator,
                    AppRoles.LocationManager,
                    AppRoles.ReadOnly));

            options.AddPolicy(CareHomePolicies.CanReconcilePayments, policy =>
                policy.RequireRole(
                    AppRoles.TenantAdmin,
                    AppRoles.Administrator,
                    AppRoles.LocationManager));
        });

        return services;
    }
}
