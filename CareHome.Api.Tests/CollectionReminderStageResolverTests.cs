using CareHome.Api.Models;
using CareHome.Api.Services;
using Xunit;

namespace CareHome.Api.Tests;

public class CollectionReminderStageResolverTests
{
    private static CollectionPolicy DefaultPolicy() => new()
    {
        DueReminderDaysBefore = 7,
        Overdue7Days = 7,
        Overdue14Days = 14,
        Overdue30Days = 30,
        EscalationDays = 60,
    };

    [Fact]
    public void Pre_due_fires_on_configured_day()
    {
        var due = new DateOnly(2026, 4, 10);
        var today = new DateOnly(2026, 4, 3);
        Assert.Equal(0, CollectionReminderStageResolver.Resolve(due, today, DefaultPolicy(), null));
    }

    [Fact]
    public void Pre_due_catch_up_when_job_missed_ideal_day()
    {
        var due = new DateOnly(2026, 4, 10);
        var today = new DateOnly(2026, 4, 8);
        Assert.Equal(0, CollectionReminderStageResolver.Resolve(due, today, DefaultPolicy(), null));
    }

    [Fact]
    public void Pre_due_not_sent_again_after_stage_zero_logged()
    {
        var due = new DateOnly(2026, 4, 10);
        var today = new DateOnly(2026, 4, 9);
        var sent = new HashSet<int> { 0 };
        Assert.Null(CollectionReminderStageResolver.Resolve(due, today, DefaultPolicy(), sent));
    }

    [Fact]
    public void Overdue_escalation_stage_when_past_escalation_days()
    {
        var due = new DateOnly(2026, 1, 1);
        var today = new DateOnly(2026, 3, 15);
        Assert.Equal(60, CollectionReminderStageResolver.Resolve(due, today, DefaultPolicy(), null));
    }

    [Fact]
    public void Overdue_uses_thirty_day_stage_before_escalation()
    {
        var due = new DateOnly(2026, 2, 1);
        var today = new DateOnly(2026, 3, 5);
        Assert.Equal(30, CollectionReminderStageResolver.Resolve(due, today, DefaultPolicy(), null));
    }
}
