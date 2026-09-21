namespace CareHome.Api.Security.Authorization;

/// <summary>
/// Named authorization capabilities for care-provider revenue operations.
/// Fine-grained care-home scoping remains in <see cref="UserAccessService"/>.
/// </summary>
public static class CareHomePolicies
{
    public const string CanManageOrganisation = nameof(CanManageOrganisation);
    public const string CanManageCareHome = nameof(CanManageCareHome);
    public const string CanManageBilling = nameof(CanManageBilling);
    public const string CanViewFinancialReports = nameof(CanViewFinancialReports);
    public const string CanManageFunding = nameof(CanManageFunding);
    public const string CanManageReceivables = nameof(CanManageReceivables);
    public const string CanViewReceivables = nameof(CanViewReceivables);
    public const string CanManagePayments = nameof(CanManagePayments);
    public const string CanViewPayments = nameof(CanViewPayments);
    public const string CanViewBanking = nameof(CanViewBanking);
    public const string CanManageBanking = nameof(CanManageBanking);
    public const string CanReconcilePayments = nameof(CanReconcilePayments);
    public const string CanViewAudit = nameof(CanViewAudit);
    public const string PlatformOnly = nameof(PlatformOnly);
}
