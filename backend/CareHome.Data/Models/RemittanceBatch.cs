using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class RemittanceBatch : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    public int? FundingAuthorityId { get; set; }

    [MaxLength(100)]
    public string? PaymentReference { get; set; }

    public DateOnly? ReceivedDate { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = RemittanceBatchStatuses.Imported;

    [MaxLength(30)]
    public string SourceFormat { get; set; } = "CSV";

    [MaxLength(260)]
    public string? SourceFileName { get; set; }

    [MaxLength(64)]
    public string? ContentChecksum { get; set; }

    public int? PaymentId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    [MaxLength(450)]
    public string? ConfirmedBy { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;

    public FundingAuthority? FundingAuthority { get; set; }

    public Payment? Payment { get; set; }

    public ICollection<RemittanceLine> Lines { get; set; } = new List<RemittanceLine>();
}

public static class RemittanceBatchStatuses
{
    public const string Imported = "Imported";
    public const string NeedsReview = "NeedsReview";
    public const string PartiallyMatched = "PartiallyMatched";
    public const string Matched = "Matched";
    public const string Confirmed = "Confirmed";
    public const string Failed = "Failed";
}
