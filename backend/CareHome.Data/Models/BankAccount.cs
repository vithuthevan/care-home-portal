using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class BankAccount : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? BankName { get; set; }

    [MaxLength(50)]
    public string? AccountReference { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "GBP";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;

    public ICollection<BankImportBatch> ImportBatches { get; set; } = new List<BankImportBatch>();

    public ICollection<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();
}
