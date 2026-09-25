using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class Invoice : ITenantOwned
    {
        public int Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public int TenantId { get; set; }

        [Required]
        [MaxLength(40)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public int CompanyId { get; set; }

        public int CareHomeId { get; set; }

        public int FundingAuthorityId { get; set; }

        public int InvoiceCategoryId { get; set; }

        public int? InvoiceTemplateId { get; set; }

        public DateOnly InvoiceDate { get; set; }

        public DateOnly DueDate { get; set; }

        public DateOnly PeriodStart { get; set; }

        public DateOnly PeriodEnd { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Generated";

        [Required]
        [MaxLength(30)]
        public string PaymentStatus { get; set; } = "NotPaid";

        public decimal TotalAmount { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset GeneratedAt { get; set; }

        public DateTimeOffset? SentAt { get; set; }

        [MaxLength(300)]
        public string? RecipientEmail { get; set; }

        [MaxLength(500)]
        public string? PdfPath { get; set; }

        public int? SageExportBatchId { get; set; }

        public DateTimeOffset? SageExportedAt { get; set; }

        [MaxLength(150)]
        public string SnapshotTenantName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string SnapshotCompanyName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string SnapshotCareHomeName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string SnapshotCareHomeCode { get; set; } = string.Empty;

        [MaxLength(150)]
        public string SnapshotFundingAuthorityName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string SnapshotFundingAuthorityCode { get; set; } = string.Empty;

        [MaxLength(100)]
        public string SnapshotInvoiceCategoryName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string SnapshotInvoiceCategoryCode { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? SnapshotTemplateName { get; set; }

        [MaxLength(300)]
        public string? SnapshotHeaderText1 { get; set; }

        [MaxLength(300)]
        public string? SnapshotHeaderText2 { get; set; }

        [MaxLength(1000)]
        public string? SnapshotFooterText { get; set; }

        [MaxLength(150)]
        public string? SnapshotBankAccountName { get; set; }

        [MaxLength(20)]
        public string? SnapshotSortCode { get; set; }

        [MaxLength(20)]
        public string? SnapshotAccountNumber { get; set; }

        [MaxLength(150)]
        public string? SnapshotContactName { get; set; }

        [MaxLength(150)]
        public string? SnapshotContactJobTitle { get; set; }

        [MaxLength(150)]
        public string? SnapshotContactEmail { get; set; }

        [MaxLength(30)]
        public string? SnapshotContactPhone { get; set; }

        public Tenant Tenant { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public CareHomeLocation CareHome { get; set; } = null!;

        public FundingAuthority FundingAuthority { get; set; } = null!;

        public InvoiceCategory InvoiceCategory { get; set; } = null!;

        public InvoiceTemplate? InvoiceTemplate { get; set; }

        public SageExportBatch? SageExportBatch { get; set; }

        public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();

        public ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();
    }
}

