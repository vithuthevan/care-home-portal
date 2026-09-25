using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class ReconciliationSuggestion : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    public int BankTransactionId { get; set; }

    public int TotalScore { get; set; }

    [MaxLength(4000)]
    public string ExplanationJson { get; set; } = "[]";

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Active";

    public DateTimeOffset CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;

    public BankTransaction BankTransaction { get; set; } = null!;

    public ICollection<ReconciliationSuggestionLine> Lines { get; set; } = new List<ReconciliationSuggestionLine>();
}

public class ReconciliationSuggestionLine
{
    public int Id { get; set; }

    public int SuggestionId { get; set; }

    public int InvoiceId { get; set; }

    public decimal SuggestedAmount { get; set; }

    public ReconciliationSuggestion Suggestion { get; set; } = null!;

    public Invoice Invoice { get; set; } = null!;
}
