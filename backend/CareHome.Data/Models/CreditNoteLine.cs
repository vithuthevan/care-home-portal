using System.ComponentModel.DataAnnotations;

namespace CareHome.Api.Models
{
    public class CreditNoteLine
    {
        public int Id { get; set; }

        public int CreditNoteId { get; set; }

        public int InvoiceLineId { get; set; }

        public DateOnly ServicePeriodStart { get; set; }

        public DateOnly ServicePeriodEnd { get; set; }

        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public CreditNote CreditNote { get; set; } = null!;

        public InvoiceLine InvoiceLine { get; set; } = null!;
    }
}

