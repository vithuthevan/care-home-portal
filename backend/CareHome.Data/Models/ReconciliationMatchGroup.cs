using CareHome.Api.Security;

namespace CareHome.Api.Models;

public class ReconciliationMatchGroup : ITenantOwned
{
    public int Id { get; set; }

    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int TenantId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;

    public ICollection<ReconciliationMatchGroupLine> Lines { get; set; } = new List<ReconciliationMatchGroupLine>();
}

public class ReconciliationMatchGroupLine
{
    public int Id { get; set; }

    public int MatchGroupId { get; set; }

    public int InvoiceId { get; set; }

    public decimal AllocatedAmount { get; set; }

    public ReconciliationMatchGroup MatchGroup { get; set; } = null!;

    public Invoice Invoice { get; set; } = null!;
}
