using CareHome.Api.Dtos.Billing;

namespace CareHome.Api.Billing;

public static class BillingPreviewRequestValidator
{
    public static string? Validate(BillingPreviewRequest request)
    {
        if (request.PeriodStart == default || request.PeriodEnd == default)
        {
            return "Billing period start and end are required.";
        }

        if (request.PeriodEnd < request.PeriodStart)
        {
            return "Billing period end cannot be before start.";
        }

        if (request.CompanyId <= 0)
        {
            return "Company is required.";
        }

        return null;
    }
}
