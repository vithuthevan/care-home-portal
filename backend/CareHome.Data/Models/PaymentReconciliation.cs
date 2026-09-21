using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class PaymentReconciliation : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    public int BankTransactionId { get; set; }

    public int PaymentId { get; set; }

    public int? MatchGroupId { get; set; }

    public int ConfidenceScore { get; set; }

    [MaxLength(4000)]
    public string? ExplanationJson { get; set; }

    public DateTimeOffset ConfirmedAt { get; set; }

    [MaxLength(450)]
    public string? ConfirmedBy { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Confirmed";

    public DateTimeOffset? ReversedAt { get; set; }

    [MaxLength(450)]
    public string? ReversedBy { get; set; }

    [MaxLength(500)]
    public string? ReversalReason { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Tenant Tenant { get; set; } = null!;

    public BankTransaction BankTransaction { get; set; } = null!;

    public Payment Payment { get; set; } = null!;

    public ReconciliationMatchGroup? MatchGroup { get; set; }
}
