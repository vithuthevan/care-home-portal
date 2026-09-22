namespace CareHome.Api.Dtos.Invoices
{
    public class InvoiceListDto
    {
        public int Id { get; set; }

        public Guid PublicId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string CareHomeName { get; set; } = string.Empty;

        public string FundingAuthorityName { get; set; } = string.Empty;

        public string InvoiceCategoryName { get; set; } = string.Empty;

        public DateOnly InvoiceDate { get; set; }

        public DateOnly PeriodStart { get; set; }

        public DateOnly PeriodEnd { get; set; }

        public string Status { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        /// <summary>Derived collection status from receivables (authoritative for revenue UI).</summary>
        public string? CollectionStatus { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal CreditedAmount { get; set; }

        public decimal OutstandingAmount { get; set; }

        public bool IsOverdue { get; set; }

        public bool IsDue { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTimeOffset? SentAt { get; set; }
    }

    public class InvoiceDetailDto : InvoiceListDto
    {
        public int CompanyId { get; set; }

        public Guid CompanyPublicId { get; set; }

        public int CareHomeId { get; set; }

        public Guid CareHomePublicId { get; set; }

        public int FundingAuthorityId { get; set; }

        public Guid FundingAuthorityPublicId { get; set; }

        public int InvoiceCategoryId { get; set; }

        public DateOnly DueDate { get; set; }

        public string? RecipientEmail { get; set; }

        public List<InvoiceLineDto> Lines { get; set; } = [];
    }

    public class InvoiceLineDto
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        public Guid ClientPublicId { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public string ClientReference { get; set; } = string.Empty;

        public string SageId { get; set; } = string.Empty;

        public string NominalCode { get; set; } = string.Empty;

        public DateOnly ServicePeriodStart { get; set; }

        public DateOnly ServicePeriodEnd { get; set; }

        public int EligibleDays { get; set; }

        public string RateFrequency { get; set; } = string.Empty;

        public decimal RateAmount { get; set; }

        public decimal LineAmount { get; set; }

        public string Description { get; set; } = string.Empty;

        /// <summary>Human-readable basis captured when the line was generated (not a recalculation).</summary>
        public string? AmountBasis { get; set; }
    }

    public class UpdatePaymentStatusRequest
    {
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class BulkPaymentStatusRequest
    {
        public List<int> InvoiceIds { get; set; } = [];

        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class BulkSendRequest
    {
        public List<int> InvoiceIds { get; set; } = [];
    }

    public class BulkSendResultDto
    {
        public int Succeeded { get; set; }

        public int Failed { get; set; }

        public int Skipped { get; set; }

        public List<BulkSendItemDto> Items { get; set; } = [];
    }

    public class BulkSendItemDto
    {
        public int InvoiceId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public string Outcome { get; set; } = string.Empty;

        public string? Reason { get; set; }
    }
}

