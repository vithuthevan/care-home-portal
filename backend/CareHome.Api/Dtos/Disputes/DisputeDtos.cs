namespace CareHome.Api.Dtos.Disputes;

public class DisputeListDto
{
    public Guid PublicId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string FunderName { get; set; } = string.Empty;

    public decimal DisputedAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string ReasonCode { get; set; } = string.Empty;

    public DateOnly OpenedDate { get; set; }
}

public class DisputeDetailDto : DisputeListDto
{
    public Guid InvoicePublicId { get; set; }

    public List<DisputeMessageDto> Messages { get; set; } = [];
}

public class DisputeMessageDto
{
    public Guid PublicId { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}

public class OpenDisputeRequest
{
    public Guid InvoicePublicId { get; set; }

    public string ReasonCode { get; set; } = string.Empty;

    public decimal DisputedAmount { get; set; }

    public DateOnly? DueDate { get; set; }

    public int? RemittanceLineId { get; set; }
}

public class ResolveDisputeRequest
{
    public string? Resolution { get; set; }
}
