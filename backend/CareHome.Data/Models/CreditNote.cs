using System.ComponentModel.DataAnnotations;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class CreditNote : ITenantOwned
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        [Required]
        [MaxLength(40)]
        public string CreditNoteNumber { get; set; } = string.Empty;

        public int InvoiceId { get; set; }

        public DateOnly CreditNoteDate { get; set; }

        public DateOnly PeriodStart { get; set; }

        public DateOnly PeriodEnd { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Generated";

        public decimal TotalAmount { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset GeneratedAt { get; set; }

        public DateTimeOffset? SentAt { get; set; }

        [MaxLength(300)]
        public string? RecipientEmail { get; set; }

        [MaxLength(500)]
        public string? PdfPath { get; set; }

        public Tenant Tenant { get; set; } = null!;

        public Invoice Invoice { get; set; } = null!;

        public ICollection<CreditNoteLine> Lines { get; set; } = new List<CreditNoteLine>();
    }
}

