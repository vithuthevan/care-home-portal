using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class RemittanceLine : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    public int RemittanceBatchId { get; set; }

    public int LineNumber { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = RemittanceLineStatuses.Pending;

    [MaxLength(50)]
    public string? InvoiceReference { get; set; }

    [MaxLength(50)]
    public string? ResidentReference { get; set; }

    public decimal? GrossAmount { get; set; }

    public decimal? PaidAmount { get; set; }

    public decimal? DeductionAmount { get; set; }

    [MaxLength(50)]
    public string? DeductionReasonCode { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int? MatchedInvoiceId { get; set; }

    public int? MatchConfidence { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public RemittanceBatch Batch { get; set; } = null!;

    public Invoice? MatchedInvoice { get; set; }
}

public static class RemittanceLineStatuses
{
    public const string Pending = "Pending";
    public const string Matched = "Matched";
    public const string Unmatched = "Unmatched";
    public const string Confirmed = "Confirmed";
    public const string Failed = "Failed";
}
