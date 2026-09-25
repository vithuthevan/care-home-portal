using CareHome.Api.Payments.Dtos;
using CareHome.Api.Reconciliation.Domain;

namespace CareHome.Api.Reconciliation.Dtos;

public class BankAccountDto
{
    public Guid PublicId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? BankName { get; set; }

    public string? AccountReference { get; set; }

    public string Currency { get; set; } = "GBP";

    public bool IsActive { get; set; }
}

public class CreateBankAccountRequest
{
    public string Name { get; set; } = string.Empty;

    public string? BankName { get; set; }

    public string? AccountReference { get; set; }

    public string? Currency { get; set; }
}

public class BankImportPreviewRowDto
{
    public int RowNumber { get; set; }

    public bool IsValid { get; set; }

    public string? Error { get; set; }

    public bool IsDuplicate { get; set; }

    public DateOnly? TransactionDate { get; set; }

    public decimal? Amount { get; set; }

    public string? Direction { get; set; }

    public string? Reference { get; set; }

    public string? Description { get; set; }

    public string? Counterparty { get; set; }
}

public class BankImportPreviewDto
{
    public string FileName { get; set; } = string.Empty;

    public string ContentChecksum { get; set; } = string.Empty;

    public List<string> Headers { get; set; } = [];

    public BankCsvColumnMapping ColumnMapping { get; set; } = new();

    public List<BankImportPreviewRowDto> Rows { get; set; } = [];

    public int ValidCount { get; set; }

    public int InvalidCount { get; set; }

    public int DuplicateCount { get; set; }

    public bool FileAlreadyImported { get; set; }
}

public class BankImportCommitRequest
{
    public Guid BankAccountPublicId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentChecksum { get; set; } = string.Empty;

    public BankCsvColumnMapping ColumnMapping { get; set; } = new();

    public List<int> AcceptedRowNumbers { get; set; } = [];
}

public class BankImportBatchDto
{
    public Guid PublicId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public DateTimeOffset ImportedAt { get; set; }

    public int AcceptedCount { get; set; }

    public int RejectedCount { get; set; }

    public int DuplicateCount { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class MatchScoreFactorDto
{
    public string Label { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public int Points { get; set; }
}

public class ReconciliationSuggestionLineDto
{
    public Guid InvoicePublicId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public decimal SuggestedAmount { get; set; }
}

public class ReconciliationSuggestionDto
{
    public Guid PublicId { get; set; }

    public int TotalScore { get; set; }

    public string ConfidenceBand { get; set; } = string.Empty;

    public List<MatchScoreFactorDto> Factors { get; set; } = [];

    public List<ReconciliationSuggestionLineDto> Lines { get; set; } = [];
}

public class BankTransactionWorkspaceDto
{
    public Guid PublicId { get; set; }

    public DateOnly TransactionDate { get; set; }

    public decimal Amount { get; set; }

    public string Direction { get; set; } = string.Empty;

    public string? Reference { get; set; }

    public string? Description { get; set; }

    public string? Counterparty { get; set; }

    public string Status { get; set; } = string.Empty;

    public ReconciliationSuggestionDto? TopSuggestion { get; set; }
}

public class ReconciliationWorkspaceSummaryDto
{
    public int Unreconciled { get; set; }

    public int Suggested { get; set; }

    public int HighConfidence { get; set; }

    public int NeedsReview { get; set; }

    public List<BankTransactionWorkspaceDto> Transactions { get; set; } = [];
}

public class ConfirmReconciliationRequest
{
    public Guid? SuggestionPublicId { get; set; }

    public List<PaymentAllocationLineRequest> ManualAllocations { get; set; } = [];

    public int? FundingAuthorityId { get; set; }

    public string? IdempotencyKey { get; set; }
}

public class CreateUnappliedPaymentFromBankRequest
{
    public int? FundingAuthorityId { get; set; }

    public string? IdempotencyKey { get; set; }
}

public class ReverseReconciliationRequest
{
    public string? Reason { get; set; }
}

public class ManualInvoiceSearchResultDto
{
    public Guid InvoicePublicId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string? ResidentName { get; set; }

    public string FunderName { get; set; } = string.Empty;

    public string CareHomeName { get; set; } = string.Empty;

    public decimal OutstandingAmount { get; set; }
}

public class BankCsvMappingTemplateDto
{
    public Guid PublicId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? BankAccountPublicId { get; set; }

    public BankCsvColumnMapping ColumnMapping { get; set; } = new();
}

public class SaveBankCsvMappingTemplateRequest
{
    public Guid? PublicId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? BankAccountPublicId { get; set; }

    public BankCsvColumnMapping ColumnMapping { get; set; } = new();
}

public class ReconciliationMatchGroupLineDto
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid InvoicePublicId { get; set; }

    public decimal AllocatedAmount { get; set; }
}

public class BankTransactionDetailDto
{
    public Guid PublicId { get; set; }

    public DateOnly TransactionDate { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Reference { get; set; }

    public string? Description { get; set; }

    public string? Counterparty { get; set; }

    public Guid? PaymentPublicId { get; set; }

    public decimal? PaymentUnappliedAmount { get; set; }

    public int? ConfidenceScore { get; set; }

    public string? ReconciliationStatus { get; set; }

    public List<ReconciliationMatchGroupLineDto> MatchGroupLines { get; set; } = [];

    public List<ReconciliationSuggestionDto> Suggestions { get; set; } = [];
}
