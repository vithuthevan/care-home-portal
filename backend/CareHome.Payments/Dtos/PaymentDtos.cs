namespace CareHome.Api.Payments.Dtos;

public class PaymentListDto
{
    public Guid PublicId { get; set; }

    public DateOnly ReceivedDate { get; set; }

    public string? Reference { get; set; }

    public string? PayerName { get; set; }

    public decimal Amount { get; set; }

    public decimal AllocatedAmount { get; set; }

    public decimal UnappliedAmount { get; set; }

    public string Currency { get; set; } = "GBP";

    public string Status { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;
}

public class PaymentDetailDto : PaymentListDto
{
    public int? FundingAuthorityId { get; set; }

    public int? CareHomeId { get; set; }

    public string? ExternalReference { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ReversedAt { get; set; }

    public string? ReversalReason { get; set; }

    public List<PaymentAllocationDto> Allocations { get; set; } = [];
}

public class PaymentAllocationDto
{
    public Guid PublicId { get; set; }

    public Guid InvoicePublicId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string? ResidentName { get; set; }

    public string CareHomeName { get; set; } = string.Empty;

    public decimal InvoiceTotal { get; set; }

    public decimal OutstandingBefore { get; set; }

    public decimal AllocatedAmount { get; set; }

    public decimal OutstandingAfter { get; set; }

    public bool IsReversed { get; set; }

    public DateTimeOffset AllocatedAt { get; set; }
}

public class CreatePaymentRequest
{
    public int? FundingAuthorityId { get; set; }

    public int? CareHomeId { get; set; }

    public DateOnly ReceivedDate { get; set; }

    public decimal Amount { get; set; }

    public string? Currency { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public List<PaymentAllocationLineRequest>? InitialAllocations { get; set; }
}

public class PaymentAllocationLineRequest
{
    public Guid InvoicePublicId { get; set; }

    public decimal Amount { get; set; }
}

public class AllocatePaymentRequest
{
    public List<PaymentAllocationLineRequest> Allocations { get; set; } = [];
}

public class ReversePaymentRequest
{
    public string? Reason { get; set; }
}

public class ReverseAllocationRequest
{
    public string? Reason { get; set; }
}

public class PaymentAllocationSuggestionDto
{
    public Guid InvoicePublicId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public decimal OutstandingAmount { get; set; }

    public decimal SuggestedAmount { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class PaymentAllocationCandidateDto
{
    public Guid InvoicePublicId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string? ResidentName { get; set; }

    public string FunderName { get; set; } = string.Empty;

    public string CareHomeName { get; set; } = string.Empty;

    public decimal OutstandingAmount { get; set; }

    public decimal DefaultAllocationAmount { get; set; }
}
