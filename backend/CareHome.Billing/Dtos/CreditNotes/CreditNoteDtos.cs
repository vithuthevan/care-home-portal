namespace CareHome.Api.Dtos.CreditNotes
{
    public class CreditNotePreviewRequest
    {
        public int? ClientId { get; set; }

        public int? FundingAuthorityId { get; set; }

        public int? InvoiceCategoryId { get; set; }

        public DateOnly PeriodStart { get; set; }

        public DateOnly PeriodEnd { get; set; }

        public DateOnly CreditNoteDate { get; set; }

        public string Reason { get; set; } = string.Empty;

        public Dictionary<int, decimal>? LineAmounts { get; set; }
    }

    public class CreditNotePreviewLineDto
    {
        public int InvoiceLineId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateOnly ServiceFrom { get; set; }

        public DateOnly ServiceTo { get; set; }

        public decimal InvoicedAmount { get; set; }

        public decimal AlreadyCredited { get; set; }

        public decimal RemainingAmount { get; set; }

        public decimal CreditAmount { get; set; }
    }

    public class CreditNotePreviewResponse
    {
        public List<CreditNotePreviewLineDto> Lines { get; set; } = [];

        public decimal TotalCredit { get; set; }

        public List<string> Exceptions { get; set; } = [];

        public bool CanGenerate { get; set; }
    }

    public class CreditNoteDto
    {
        public int Id { get; set; }

        public string CreditNoteNumber { get; set; } = string.Empty;

        public int InvoiceId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public DateOnly CreditNoteDate { get; set; }

        public DateOnly PeriodStart { get; set; }

        public DateOnly PeriodEnd { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public DateTimeOffset? SentAt { get; set; }

        public List<CreditNoteLineDto> Lines { get; set; } = [];
    }

    public class CreditNoteLineDto
    {
        public int Id { get; set; }

        public int InvoiceLineId { get; set; }

        public string Description { get; set; } = string.Empty;

        public DateOnly ServicePeriodStart { get; set; }

        public DateOnly ServicePeriodEnd { get; set; }

        public decimal Amount { get; set; }
    }
}

