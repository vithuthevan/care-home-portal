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

    public int Overdue7Days { get; set; }

    public int Overdue14Days { get; set; }

    public int Overdue30Days { get; set; }

    public int EscalationDays { get; set; }
}

public class UpdateCollectionPolicyRequest
{
    public int Overdue7Days { get; set; }

    public int Overdue14Days { get; set; }

    public int Overdue30Days { get; set; }

    public int EscalationDays { get; set; }
}
