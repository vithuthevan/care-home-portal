namespace CareHome.Api.Dtos.Billing
{
    public class BillingPreviewRequest
    {
        public int CompanyId { get; set; }

        public int? CareHomeId { get; set; }

        public int? InvoiceCategoryId { get; set; }

        public DateOnly PeriodStart { get; set; }

        public DateOnly PeriodEnd { get; set; }

        public List<int>? ClientIds { get; set; }
    }

    public class BillingPreviewResponse
    {
        public DateOnly PeriodStart { get; set; }

        public DateOnly PeriodEnd { get; set; }

        public DateOnly RequestedPeriodStart { get; set; }

        public DateOnly RequestedPeriodEnd { get; set; }

        public int SkippedAlreadyBilledDays { get; set; }

        public List<BillingCoverageDto> Coverage { get; set; } = [];

        public List<BillingPreviewLineDto> Lines { get; set; } = [];

        public List<BillingExceptionDto> Exceptions { get; set; } = [];

        public decimal TotalAmount { get; set; }

        public bool CanGenerate { get; set; }
    }

    public class BillingCoverageDto
    {
        public int ClientId { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public int ClientFundingContractId { get; set; }

        public DateOnly RequestedPeriodStart { get; set; }

        public DateOnly RequestedPeriodEnd { get; set; }

        public List<BillingDateRangeDto> AlreadyBilledPeriods { get; set; } = [];

        public List<BillingDateRangeDto> RemainingBillablePeriods { get; set; } = [];

        public int SkippedAlreadyBilledDays { get; set; }
    }

    public class BillingDateRangeDto
    {
        public DateOnly Start { get; set; }

        public DateOnly End { get; set; }

        public int Days { get; set; }
    }

    public class BillingPreviewLineDto
    {
        public int ClientId { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public string ClientReference { get; set; } = string.Empty;

        public string SageId { get; set; } = string.Empty;

        public int CareHomeId { get; set; }

        public string CareHomeName { get; set; } = string.Empty;

        public int CompanyId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public int FundingAuthorityId { get; set; }

        public string FundingAuthorityName { get; set; } = string.Empty;

        public int InvoiceCategoryId { get; set; }

        public string InvoiceCategoryName { get; set; } = string.Empty;

        public int NominalCodeId { get; set; }

        public string NominalCode { get; set; } = string.Empty;

        public string? NominalCodeName { get; set; }

        public int ClientFundingContractId { get; set; }

        public int? FundingRateId { get; set; }

        public int? MiscChargeId { get; set; }

        public DateOnly ServiceFrom { get; set; }

        public DateOnly ServiceTo { get; set; }

        public int EligibleDays { get; set; }

        public string Frequency { get; set; } = string.Empty;

        public decimal Rate { get; set; }

        public decimal Amount { get; set; }

        public string Description { get; set; } = string.Empty;

        public int? InvoiceTemplateId { get; set; }
    }

    public class BillingExceptionDto
    {
        public string Severity { get; set; } = "Error";

        public string Code { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public int? ClientId { get; set; }

        public string? ClientName { get; set; }

        public int? CareHomeId { get; set; }

        public int? ClientFundingContractId { get; set; }

        public List<int>? ContractIds { get; set; }

        public string? FundingAuthorityName { get; set; }

        public string? InvoiceCategoryName { get; set; }

        public DateOnly? OverlapStart { get; set; }

        public DateOnly? OverlapEnd { get; set; }
    }

    public class BillingGenerateResponse
    {
        public List<int> InvoiceIds { get; set; } = [];

        public int InvoiceCount { get; set; }

        public decimal TotalAmount { get; set; }

        public List<BillingExceptionDto> Exceptions { get; set; } = [];
    }
}

