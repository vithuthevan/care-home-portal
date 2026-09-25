namespace CareHome.Api.RevenueAssurance.Dtos;

public class RevenueAssuranceFindingDto
{
    public Guid PublicId { get; set; }

    public string RuleCode { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal? EstimatedImpact { get; set; }

    public string Explanation { get; set; } = string.Empty;

    public DateTimeOffset DetectedAt { get; set; }

    public int? InvoiceId { get; set; }

    public int? CareHomeId { get; set; }

    public int? ClientId { get; set; }
}

public class RevenueAssuranceDashboardDto
{
    public int OpenFindings { get; set; }

    public int CriticalFindings { get; set; }

    public decimal PotentialLeakage { get; set; }

    public List<RuleCountDto> ByRule { get; set; } = [];
}

public class RuleCountDto
{
    public string RuleCode { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class ResolveFindingRequest
{
    public string? Status { get; set; }

    public string? Notes { get; set; }
}
