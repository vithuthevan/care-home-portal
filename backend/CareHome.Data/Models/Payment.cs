using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class Payment : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    public int? FundingAuthorityId { get; set; }

    /// <summary>When set, payment is attributed to a single care home; null allows multi-home allocation (tenant finance roles).</summary>
    public int? CareHomeId { get; set; }

    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "GBP";

    public DateOnly ReceivedDate { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(200)]
    public string? ExternalReference { get; set; }

    [Required]
    [MaxLength(30)]
    public string Source { get; set; } = "Manual";

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Received";

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }

    public DateTimeOffset? ReversedAt { get; set; }

    [MaxLength(450)]
    public string? ReversedBy { get; set; }

    [MaxLength(500)]
    public string? ReversalReason { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Tenant Tenant { get; set; } = null!;

    public FundingAuthority? FundingAuthority { get; set; }

    public CareHomeLocation? CareHome { get; set; }

    public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
}
