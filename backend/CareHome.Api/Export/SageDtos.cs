namespace CareHome.Api.Dtos.Sage;

public class SageExportRequest
{
    public DateOnly DateFrom { get; set; }

    public DateOnly DateTo { get; set; }

    public int? CompanyId { get; set; }

    public int? CareHomeId { get; set; }

    public string? Status { get; set; }

    public bool IncludeAlreadyExported { get; set; }
}

public class SageExportRowDto
{
    public int InvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string? SageId { get; set; }

    public string? NominalCode { get; set; }

    public decimal Amount { get; set; }

    public bool Eligible { get; set; }

    public string? Reason { get; set; }
}

public class SageExportPreviewResponse
{
    public List<SageExportRowDto> Rows { get; set; } = [];

    public int EligibleCount { get; set; }

    public int BlockedCount { get; set; }

    public List<string> Errors { get; set; } = [];

    public bool CanExport { get; set; }
}

public class SageExportBatchDto
{
    public int Id { get; set; }

    public DateTimeOffset ExportedAt { get; set; }

    public DateOnly DateFrom { get; set; }

    public DateOnly DateTo { get; set; }

    public int? CompanyId { get; set; }

    public int RecordCount { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

