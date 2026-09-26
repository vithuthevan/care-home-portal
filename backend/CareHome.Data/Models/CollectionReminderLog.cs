using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class CollectionReminderLog : ITenantOwned
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public int InvoiceId { get; set; }

    /// <summary>0 = pre-due reminder; otherwise overdue threshold days (7, 14, 30).</summary>
    public int ReminderStage { get; set; }

    public DateTimeOffset SentAt { get; set; }

    public bool Success { get; set; }

    [MaxLength(300)]
    public string? Recipient { get; set; }

    public Tenant Tenant { get; set; } = null!;

    public Invoice Invoice { get; set; } = null!;
}
