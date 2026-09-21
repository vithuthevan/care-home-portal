namespace CareHome.Api.Dtos.FundingContracts
{
    public class FundingContractDto
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        public int FundingAuthorityId { get; set; }

        public string FundingAuthorityName { get; set; } = string.Empty;

        public int InvoiceCategoryId { get; set; }

        public string InvoiceCategoryName { get; set; } = string.Empty;

        public int NominalCodeId { get; set; }

        public string NominalCode { get; set; } = string.Empty;

        public int? InvoiceTemplateId { get; set; }

        public DateOnly ContractStartDate { get; set; }

        public DateOnly? ContractEndDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public List<FundingRateDto> Rates { get; set; } = [];
    }

    public class CreateFundingContractRequest
    {
        public int FundingAuthorityId { get; set; }

        public int InvoiceCategoryId { get; set; }

        public int NominalCodeId { get; set; }

        public int? InvoiceTemplateId { get; set; }

        public DateOnly ContractStartDate { get; set; }

        public DateOnly? ContractEndDate { get; set; }
    }

    public class UpdateFundingContractRequest : CreateFundingContractRequest
    {
        public string Status { get; set; } = "Active";
    }

    public class FundingRateDto
    {
        public int Id { get; set; }

        public int ClientFundingContractId { get; set; }

        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public string Frequency { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string? Notes { get; set; }
    }

    public class CreateFundingRateRequest
    {
        public DateOnly EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        public string Frequency { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string? Notes { get; set; }

        public bool ClosePreviousOpenEnded { get; set; } = true;
    }
}

