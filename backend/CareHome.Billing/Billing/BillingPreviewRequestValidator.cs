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

        var hasCareHome = request.CareHomeId is > 0;
        var hasCompany = request.CompanyId > 0;
        var unassignedCompany = request.CompanyId < 0;
        if (!hasCareHome && !hasCompany && !unassignedCompany)
        {
            return "Select a company, a care home, or homes with no company.";
        }

        if (request.InvoiceTemplateId is > 0 && request.InvoiceCategoryId is not > 0)
        {
            return "Select an invoice category when using a template override for this billing run.";
        }

        return null;
    }
}
