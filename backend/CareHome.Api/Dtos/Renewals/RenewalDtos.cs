namespace CareHome.Api.Dtos.Renewals;

public class RenewalListDto
{
    public Guid PublicId { get; set; }

    public int ContractId { get; set; }

    public DateOnly CurrentEndDate { get; set; }

    public decimal CurrentRate { get; set; }

    public decimal? ProposedRate { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateOnly? NextActionDate { get; set; }
}

public class RenewalDashboardDto
{
    public int Expiring30 { get; set; }

    public int Expiring60 { get; set; }

    public int Expiring90 { get; set; }

    public int OverdueRenewals { get; set; }

    public int NegotiationsAwaitingResponse { get; set; }
}

public class AgreeRenewalRequest
{
    public decimal ProposedRate { get; set; }

    public DateOnly EffectiveFrom { get; set; }
}
