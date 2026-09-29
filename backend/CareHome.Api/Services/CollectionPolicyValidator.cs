using CareHome.Api.Dtos.Collections;

namespace CareHome.Api.Services;

public static class CollectionPolicyValidator
{
    public static string? Validate(UpdateCollectionPolicyRequest request)
    {
        if (request.Overdue7Days < 1)
        {
            return "Overdue 7+ days threshold must be at least 1.";
        }

        if (request.Overdue14Days < request.Overdue7Days)
        {
            return "Overdue 14+ days must be greater than or equal to the 7+ days threshold.";
        }

        if (request.Overdue30Days < request.Overdue14Days)
        {
            return "Overdue 30+ days must be greater than or equal to the 14+ days threshold.";
        }

        if (request.EscalationDays > 0 && request.EscalationDays < request.Overdue30Days)
        {
            return "Escalation days must be greater than or equal to the 30+ days threshold (or set to 0 to disable).";
        }

        return null;
    }
}
