namespace CareHome.Api.Remittance.Dtos;

public class RemittanceBatchListDto
{
    public Guid PublicId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? PaymentReference { get; set; }

    public DateOnly? ReceivedDate { get; set; }

    public string? SourceFileName { get; set; }

    public string? FunderName { get; set; }

    public int LineCount { get; set; }

    public int MatchedLineCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public class RemittanceLineDto
{
    public Guid PublicId { get; set; }

    public int LineNumber { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? InvoiceReference { get; set; }

    public string? ResidentReference { get; set; }

    public decimal? GrossAmount { get; set; }

    public decimal? PaidAmount { get; set; }

    public decimal? DeductionAmount { get; set; }

    public string? DeductionReasonCode { get; set; }

    public string? Notes { get; set; }

    public Guid? MatchedInvoicePublicId { get; set; }

    public string? MatchedInvoiceNumber { get; set; }

    public int? MatchConfidence { get; set; }
}

public class RemittanceBatchDetailDto
{
    public Guid PublicId { get; set; }

    public string Status { get; set; } = string.Empty;

    public int? FundingAuthorityId { get; set; }

    public string? FunderName { get; set; }

    public string? PaymentReference { get; set; }

    public DateOnly? ReceivedDate { get; set; }

    public Guid? PaymentPublicId { get; set; }

    public List<RemittanceLineDto> Lines { get; set; } = [];
}

public class UpdateRemittanceLineRequest
{
    public Guid LinePublicId { get; set; }

    public string? InvoiceReference { get; set; }

    public decimal? PaidAmount { get; set; }

    public decimal? DeductionAmount { get; set; }

    public string? DeductionReasonCode { get; set; }

    public Guid? MatchedInvoicePublicId { get; set; }
}

public class UpdateRemittanceBatchRequest
{
    public int? FundingAuthorityId { get; set; }

    public string? PaymentReference { get; set; }

    public DateOnly? ReceivedDate { get; set; }

    public List<UpdateRemittanceLineRequest> Lines { get; set; } = [];
}

public class ConfirmRemittanceRequest
{
    public string? IdempotencyKey { get; set; }
}
