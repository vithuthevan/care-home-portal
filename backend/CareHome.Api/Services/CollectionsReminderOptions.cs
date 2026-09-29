namespace CareHome.Api.Services;

public class CollectionsReminderOptions
{
    public const string SectionName = "Collections";

    /// <summary>
    /// When true, a background job sends due/overdue reminders for tenants with reminders enabled.
    /// </summary>
    public bool ScheduledRemindersEnabled { get; set; }

    /// <summary>
    /// UTC hour (0–23) when the daily job runs. Default 6 (06:00 UTC).
    /// </summary>
    public int ScheduledRunHourUtc { get; set; } = 6;
}
