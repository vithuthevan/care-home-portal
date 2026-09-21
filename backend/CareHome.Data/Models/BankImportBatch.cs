using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class BankImportBatch : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    public int BankAccountId { get; set; }

    [Required]
    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string ContentChecksum { get; set; } = string.Empty;

    public DateTimeOffset ImportedAt { get; set; }

    [MaxLength(450)]
    public string? ImportedBy { get; set; }

    public int RowCount { get; set; }

    public int AcceptedCount { get; set; }

    public int RejectedCount { get; set; }

    public int DuplicateCount { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Committed";

    public Tenant Tenant { get; set; } = null!;

    public BankAccount BankAccount { get; set; } = null!;

    public ICollection<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();
}
