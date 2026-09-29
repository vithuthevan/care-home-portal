namespace CareHome.Api.Dtos.DataTransfer;

public class DataTransferEntityInfoDto
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public bool SupportsImport { get; set; }

    public bool SupportsExport { get; set; }

    public IReadOnlyList<string> Columns { get; set; } = [];

    public bool RequiresPlatform { get; set; }
}

public class DataTransferPreviewDto
{
    public string Entity { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public List<DataTransferPreviewRowDto> Rows { get; set; } = [];

    public int ValidCount { get; set; }

    public int InvalidCount { get; set; }
}

public class DataTransferPreviewRowDto
{
    public int RowNumber { get; set; }

    public bool IsValid { get; set; }

    public string? Error { get; set; }

    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class DataTransferCommitResultDto
{
    public int Created { get; set; }

    public int Updated { get; set; }

    public string? Message { get; set; }
}
