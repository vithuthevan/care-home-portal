namespace CareHome.Api.Dtos.Collections;

public class CollectionsDashboardDto
{
    public decimal DueToday { get; set; }

    public decimal Overdue { get; set; }

    public decimal Overdue30 { get; set; }

    public decimal Overdue60 { get; set; }

    public decimal Overdue90 { get; set; }

    public CollectionPolicyDto Policy { get; set; } = new();
}

public class CollectionPolicyDto
{
    public Guid PublicId { get; set; }

    public int DueReminderDaysBefore { get; set; }

    public int Overdue7Days { get; set; }

    public int Overdue14Days { get; set; }

    public int Overdue30Days { get; set; }

    public int EscalationDays { get; set; }

    public bool RemindersEnabled { get; set; }

    public string? ReminderEmailSubjectTemplate { get; set; }

    public string? ReminderEmailBodyTemplate { get; set; }
}

public class UpdateCollectionPolicyRequest
{
    public int DueReminderDaysBefore { get; set; }

    public int Overdue7Days { get; set; }

    public int Overdue14Days { get; set; }

    public int Overdue30Days { get; set; }

    public int EscalationDays { get; set; }

    public bool RemindersEnabled { get; set; }

    public string? ReminderEmailSubjectTemplate { get; set; }

    public string? ReminderEmailBodyTemplate { get; set; }
}

public class CollectionReminderRunResultDto
{
    public int Succeeded { get; set; }

    public int Failed { get; set; }

    public int Skipped { get; set; }
}
