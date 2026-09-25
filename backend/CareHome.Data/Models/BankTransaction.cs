using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class BankTransaction : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    public int BankAccountId { get; set; }

    public int ImportBatchId { get; set; }

    public DateOnly TransactionDate { get; set; }

    public DateOnly? ValueDate { get; set; }

    public decimal Amount { get; set; }

    [Required]
    [MaxLength(10)]
    public string Direction { get; set; } = "In";

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "GBP";

    [MaxLength(200)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? Counterparty { get; set; }

    [MaxLength(200)]
    public string? ExternalTransactionReference { get; set; }

    [MaxLength(500)]
    public string? NormalizedReference { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Unreconciled";

    [Required]
    [MaxLength(64)]
    public string RowHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Tenant Tenant { get; set; } = null!;

    public BankAccount BankAccount { get; set; } = null!;

    public BankImportBatch ImportBatch { get; set; } = null!;

    public ICollection<ReconciliationSuggestion> Suggestions { get; set; } = new List<ReconciliationSuggestion>();

    public PaymentReconciliation? ActiveReconciliation { get; set; }
}
