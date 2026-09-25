namespace CareHome.Api.Dtos.Common;

public class MasterDataUsageDto
{
    public int FundingContractCount { get; set; }

    public int InvoiceCount { get; set; }

    public int InvoiceLineSnapshotCount { get; set; }

    public int MiscChargeCount { get; set; }

    public int InvoiceTemplateCount { get; set; }

    public int PinnedContractCount { get; set; }

    public int TotalFinancialReferences =>
        FundingContractCount
        + InvoiceCount
        + InvoiceLineSnapshotCount
        + MiscChargeCount
        + InvoiceTemplateCount
        + PinnedContractCount;

    public bool HasFinancialReferences => TotalFinancialReferences > 0;
}
