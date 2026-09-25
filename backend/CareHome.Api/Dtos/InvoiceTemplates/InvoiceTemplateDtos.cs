using CareHome.Api.Dtos.Common;

namespace CareHome.Api.Dtos.InvoiceTemplates
{
    public class InvoiceTemplateDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int InvoiceCategoryId { get; set; }

        public string InvoiceCategoryName { get; set; } = string.Empty;

        public int? FundingAuthorityId { get; set; }

        public string? FundingAuthorityName { get; set; }

        public int? CareHomeId { get; set; }

        public string? CareHomeName { get; set; }

        public int? CompanyId { get; set; }

        public string? CompanyName { get; set; }

        public string? HeaderText1 { get; set; }

        public string? HeaderText2 { get; set; }

        public string? FooterText { get; set; }

        public string? BankAccountName { get; set; }

        public string? SortCode { get; set; }

        public string? AccountNumber { get; set; }

        public string? ContactName { get; set; }

        public string? ContactJobTitle { get; set; }

        public string? ContactEmail { get; set; }

        public string? ContactPhone { get; set; }

        public string? EmailSubjectTemplate { get; set; }

        public string? EmailBodyTemplate { get; set; }

        public bool IsActive { get; set; }

        public MasterDataUsageDto? Usage { get; set; }
    }

    public class UpsertInvoiceTemplateRequest
    {
        public string Name { get; set; } = string.Empty;

        public int InvoiceCategoryId { get; set; }

        public int? FundingAuthorityId { get; set; }

        public int? CareHomeId { get; set; }

        public int? CompanyId { get; set; }

        public string? HeaderText1 { get; set; }

        public string? HeaderText2 { get; set; }

        public string? FooterText { get; set; }

        public string? BankAccountName { get; set; }

        public string? SortCode { get; set; }

        public string? AccountNumber { get; set; }

        public string? ContactName { get; set; }

        public string? ContactJobTitle { get; set; }

        public string? ContactEmail { get; set; }

        public string? ContactPhone { get; set; }

        public string? EmailSubjectTemplate { get; set; }

        public string? EmailBodyTemplate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

