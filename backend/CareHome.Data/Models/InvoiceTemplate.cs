using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class InvoiceTemplate : ITenantOwned
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public int InvoiceCategoryId { get; set; }

        public int? FundingAuthorityId { get; set; }

        public int? CareHomeId { get; set; }

        public int? CompanyId { get; set; }

        [MaxLength(500)]
        public string? AuthorityLogoPath { get; set; }

        [MaxLength(500)]
        public string? CompanyLogoPath { get; set; }

        [MaxLength(300)]
        public string? HeaderText1 { get; set; }

        [MaxLength(300)]
        public string? HeaderText2 { get; set; }

        [MaxLength(1000)]
        public string? FooterText { get; set; }

        [MaxLength(150)]
        public string? BankAccountName { get; set; }

        [MaxLength(20)]
        public string? SortCode { get; set; }

        [MaxLength(20)]
        public string? AccountNumber { get; set; }

        [MaxLength(150)]
        public string? ContactName { get; set; }

        [MaxLength(150)]
        public string? ContactJobTitle { get; set; }

        [MaxLength(150)]
        public string? ContactEmail { get; set; }

        [MaxLength(30)]
        public string? ContactPhone { get; set; }

        [MaxLength(300)]
        public string? EmailSubjectTemplate { get; set; }

        [MaxLength(4000)]
        public string? EmailBodyTemplate { get; set; }

        public bool IsActive { get; set; } = true;

        public Tenant Tenant { get; set; } = null!;

        public InvoiceCategory InvoiceCategory { get; set; } = null!;

        public FundingAuthority? FundingAuthority { get; set; }

        public CareHomeLocation? CareHome { get; set; }

        public Company? Company { get; set; }
    }
}

