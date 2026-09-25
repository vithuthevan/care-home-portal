using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class CollectionPolicy : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "Default";

    public bool IsDefault { get; set; } = true;

    public int DueReminderDaysBefore { get; set; } = 0;

    public int Overdue7Days { get; set; } = 7;

    public int Overdue14Days { get; set; } = 14;

    public int Overdue30Days { get; set; } = 30;

    public int EscalationDays { get; set; } = 60;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
