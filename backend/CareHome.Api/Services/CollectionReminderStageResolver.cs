using CareHome.Api.Models;

namespace CareHome.Api.Services;

public static class CollectionReminderStageResolver
{
    /// <summary>
    /// Resolves the reminder stage for an invoice on the given date, or null when no reminder applies.
    /// Stage 0 is the pre-due reminder; other stages use the configured day threshold as the stage id.
    /// </summary>
    public static int? Resolve(
        DateOnly dueDate,
        DateOnly today,
        CollectionPolicy policy,
        IReadOnlySet<int>? sentStages)
    {
        var preDue = TryResolvePreDue(dueDate, today, policy, sentStages);
        if (preDue.HasValue)
        {
            return preDue;
        }

        if (today <= dueDate)
        {
            return null;
        }

        var daysOverdue = today.DayNumber - dueDate.DayNumber;
        if (policy.EscalationDays > 0 && daysOverdue >= policy.EscalationDays)
        {
            return policy.EscalationDays;
        }

        if (daysOverdue >= policy.Overdue30Days)
        {
            return policy.Overdue30Days;
        }

        if (daysOverdue >= policy.Overdue14Days)
        {
            return policy.Overdue14Days;
        }

        if (daysOverdue >= policy.Overdue7Days)
        {
            return policy.Overdue7Days;
        }

        return null;
    }

    private static int? TryResolvePreDue(
        DateOnly dueDate,
        DateOnly today,
        CollectionPolicy policy,
        IReadOnlySet<int>? sentStages)
    {
        if (policy.DueReminderDaysBefore <= 0)
        {
            return null;
        }

        var daysUntilDue = dueDate.DayNumber - today.DayNumber;
        if (daysUntilDue < 0 || daysUntilDue > policy.DueReminderDaysBefore)
        {
            return null;
        }

        if (sentStages is not null && sentStages.Contains(0))
        {
            return null;
        }

        return 0;
    }
}
