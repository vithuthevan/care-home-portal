namespace CareHome.Api.Dtos.MiscCharges;

public class MiscChargePreviewResponse
{
    public string FileName { get; set; } = string.Empty;

    public int ValidCount { get; set; }

    public int InvalidCount { get; set; }

    public List<MiscChargePreviewRowDto> Rows { get; set; } = [];
}

public class MiscChargePreviewRowDto
{
    public int RowNumber { get; set; }

    public bool IsValid { get; set; }

    public string? Error { get; set; }

    public int? ClientId { get; set; }

    public string? ClientName { get; set; }

    public DateOnly? UsedDate { get; set; }

    public string? Description { get; set; }

    public decimal? Amount { get; set; }

    public int? NominalCodeId { get; set; }

    public string? NominalCode { get; set; }

    public Services.RawMiscRow Raw { get; set; } = new();
}

public class MiscChargeBatchDto
{
    public int Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public DateTimeOffset ImportedAt { get; set; }

    public int TotalRows { get; set; }

    public int AcceptedRows { get; set; }

    public string Status { get; set; } = string.Empty;
}

