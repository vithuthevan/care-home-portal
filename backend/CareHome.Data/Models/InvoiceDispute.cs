using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class InvoiceDispute : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    public int InvoiceId { get; set; }

    public int FundingAuthorityId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ReasonCode { get; set; } = string.Empty;

    public decimal DisputedAmount { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = InvoiceDisputeStatuses.Open;

    [MaxLength(450)]
    public string? AssignedUserId { get; set; }

    public DateOnly OpenedDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? ResolvedDate { get; set; }

    [MaxLength(30)]
    public string? Resolution { get; set; }

    public int? RemittanceLineId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Invoice Invoice { get; set; } = null!;

    public FundingAuthority FundingAuthority { get; set; } = null!;

    public RemittanceLine? RemittanceLine { get; set; }

    public ICollection<DisputeMessage> Messages { get; set; } = new List<DisputeMessage>();
}

public static class InvoiceDisputeStatuses
{
    public const string Open = "Open";
    public const string AwaitingProvider = "AwaitingProvider";
    public const string AwaitingFunder = "AwaitingFunder";
    public const string UnderReview = "UnderReview";
    public const string Resolved = "Resolved";
    public const string Rejected = "Rejected";
    public const string Closed = "Closed";
}

public class DisputeMessage : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    public int InvoiceDisputeId { get; set; }

    [Required]
    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    [MaxLength(450)]
    public string? AuthorUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public InvoiceDispute Dispute { get; set; } = null!;
}
