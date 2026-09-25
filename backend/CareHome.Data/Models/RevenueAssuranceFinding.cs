using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class RevenueAssuranceFinding : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    public int? CareHomeId { get; set; }

    public int? ClientId { get; set; }

    public int? FundingContractId { get; set; }

    public int? InvoiceId { get; set; }

    [Required]
    [MaxLength(50)]
    public string RuleCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = RevenueAssuranceSeverities.Medium;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = RevenueAssuranceFindingStatuses.Open;

    public decimal? ExpectedValue { get; set; }

    public decimal? ActualValue { get; set; }

    public decimal? EstimatedImpact { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Explanation { get; set; } = string.Empty;

    public DateTimeOffset DetectedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    [MaxLength(450)]
    public string? ResolvedBy { get; set; }

    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }

    public Tenant Tenant { get; set; } = null!;
}

public static class RevenueAssuranceFindingStatuses
{
    public const string Open = "Open";
    public const string Acknowledged = "Acknowledged";
    public const string Resolved = "Resolved";
    public const string Ignored = "Ignored";
}

public static class RevenueAssuranceSeverities
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";
}
