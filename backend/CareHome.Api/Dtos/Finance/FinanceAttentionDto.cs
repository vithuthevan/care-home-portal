namespace CareHome.Api.Dtos.Finance;

public class FinanceAttentionDto
{
    public decimal ReceivablesOverdue { get; set; }

    public decimal Ageing90Plus { get; set; }

    public decimal UnallocatedCash { get; set; }

    public int UnmatchedRemittances { get; set; }

    public int ContractsExpiringIn60Days { get; set; }

    public decimal PotentialRevenueLeakage { get; set; }

    public decimal OpenDisputesAmount { get; set; }

    public int OpenDisputesCount { get; set; }

    public int ResidentsRequiringBillingReview { get; set; }
}
