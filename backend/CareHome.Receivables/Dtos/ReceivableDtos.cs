using CareHome.Api.Receivables.Domain;

namespace CareHome.Api.Receivables.Dtos;

public class ReceivableInvoiceDto
{
    public int InvoiceId { get; set; }

    public Guid PublicId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public int CareHomeId { get; set; }

    public string CareHomeName { get; set; } = string.Empty;

    public string CareHomeCode { get; set; } = string.Empty;

    public int? CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public int FundingAuthorityId { get; set; }

    public string FunderName { get; set; } = string.Empty;

    public string FunderCode { get; set; } = string.Empty;

    public string? ResidentName { get; set; }

    public DateOnly InvoiceDate { get; set; }

    public DateOnly DueDate { get; set; }

    public string DocumentStatus { get; set; } = string.Empty;

    public decimal OriginalAmount { get; set; }

    public decimal CreditedAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal OutstandingAmount { get; set; }

    public int DaysOverdue { get; set; }

    public string PaymentStatus { get; set; } = string.Empty;

    public ReceivableAgeingBucket AgeingBucket { get; set; }
}

public class ReceivablesAgeingDto
{
    public decimal Current { get; set; }

    public decimal Days1To30 { get; set; }

    public decimal Days31To60 { get; set; }

    public decimal Days61To90 { get; set; }

    public decimal Days90Plus { get; set; }
}

public class ReceivablesSummaryDto
{
    public decimal TotalInvoiced { get; set; }

    public decimal TotalOutstanding { get; set; }

    public decimal TotalOverdue { get; set; }

    public decimal DueThisWeek { get; set; }

    public decimal Days90Plus { get; set; }

    public ReceivablesAgeingDto Ageing { get; set; } = new();

    public int OpenInvoiceCount { get; set; }
}

public class FunderReceivableSummaryDto
{
    public int FundingAuthorityId { get; set; }

    public string FunderName { get; set; } = string.Empty;

    public string FunderCode { get; set; } = string.Empty;

    public decimal TotalInvoiced { get; set; }

    public decimal TotalOutstanding { get; set; }

    public ReceivablesAgeingDto Ageing { get; set; } = new();

    public DateOnly? OldestUnpaidDueDate { get; set; }

    public string? OldestUnpaidInvoiceNumber { get; set; }
}

public class CareHomeReceivableSummaryDto
{
    public int CareHomeId { get; set; }

    public string CareHomeName { get; set; } = string.Empty;

    public string CareHomeCode { get; set; } = string.Empty;

    public decimal TotalInvoiced { get; set; }

    public decimal TotalOutstanding { get; set; }

    public decimal TotalOverdue { get; set; }

    public ReceivablesAgeingDto Ageing { get; set; } = new();

    public int OpenInvoiceCount { get; set; }
}

public class ReceivableInvoiceQuery
{
    public int? CompanyId { get; set; }

    public int? CareHomeId { get; set; }

    public int? FundingAuthorityId { get; set; }

    public string? DocumentStatus { get; set; }

    public string? PaymentStatus { get; set; }

    public bool? OverdueOnly { get; set; }

    public ReceivableAgeingBucket? AgeingBucket { get; set; }

    public DateOnly? InvoiceDateFrom { get; set; }

    public DateOnly? InvoiceDateTo { get; set; }

    public DateOnly? DueDateFrom { get; set; }

    public DateOnly? DueDateTo { get; set; }

    public string? InvoiceNumber { get; set; }

    public bool OpenReceivablesOnly { get; set; } = true;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public DateOnly? AsOfDate { get; set; }
}
