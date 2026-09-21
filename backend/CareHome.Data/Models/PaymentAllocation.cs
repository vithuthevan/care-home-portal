using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class PaymentAllocation : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }

    public decimal AllocatedAmount { get; set; }

    public DateTimeOffset AllocatedAt { get; set; }

    [MaxLength(450)]
    public string? AllocatedBy { get; set; }

    public bool IsReversed { get; set; }

    public DateTimeOffset? ReversedAt { get; set; }

    [MaxLength(450)]
    public string? ReversedBy { get; set; }

    [MaxLength(500)]
    public string? ReversalReason { get; set; }

    public Tenant Tenant { get; set; } = null!;

    public Payment Payment { get; set; } = null!;

    public Invoice Invoice { get; set; } = null!;
}
