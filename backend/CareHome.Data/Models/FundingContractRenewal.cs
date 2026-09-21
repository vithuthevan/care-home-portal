using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class FundingContractRenewal : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    public Tenant Tenant { get; set; } = null!;

    public int ContractId { get; set; }

    public DateOnly CurrentEndDate { get; set; }

    public decimal CurrentRate { get; set; }

    public decimal? ProposedRate { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = FundingContractRenewalStatuses.Upcoming;

    [MaxLength(450)]
    public string? AssignedUserId { get; set; }

    public DateOnly? NextActionDate { get; set; }

    public DateTimeOffset? RequestedAt { get; set; }

    public DateTimeOffset? RespondedAt { get; set; }

    public DateTimeOffset? AgreedAt { get; set; }

    public DateOnly? EffectiveFrom { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ClientFundingContract Contract { get; set; } = null!;
}

public static class FundingContractRenewalStatuses
{
    public const string Upcoming = "Upcoming";
    public const string ContactRequired = "ContactRequired";
    public const string RequestSent = "RequestSent";
    public const string Negotiating = "Negotiating";
    public const string Agreed = "Agreed";
    public const string Rejected = "Rejected";
    public const string Completed = "Completed";
}
