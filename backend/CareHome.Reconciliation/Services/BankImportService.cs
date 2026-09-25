using System.Security.Cryptography;
using System.Text;
using CareHome.Api.Abstractions;
using CareHome.Api.Data;
using CareHome.Api.Models;
using CareHome.Api.Reconciliation.Domain;
using CareHome.Api.Reconciliation.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Reconciliation.Services;

public sealed class BankImportService(
    CareHomeDbContext dbContext,
    BankAccountService bankAccounts,
    ReconciliationMatchingEngine matchingEngine,
    IAuditWriter audit,
    TimeProvider timeProvider)
{
    public async Task<BankImportPreviewDto> PreviewAsync(
        int tenantId,
        Guid bankAccountPublicId,
        string fileName,
        Stream csvStream,
        BankCsvColumnMapping? mapping,
        CancellationToken cancellationToken = default)
    {
        var account = await bankAccounts.RequireAsync(tenantId, bankAccountPublicId, cancellationToken);
        using var reader = new StreamReader(csvStream);
        var content = await reader.ReadToEndAsync(cancellationToken);
        var checksum = ComputeChecksum(content);
        var headers = BankCsvParser.ReadHeaders(content);
        var parseRows = BankCsvParser.ParseRows(content);

        var columnMapping = mapping ?? GuessMapping(headers);
        foreach (var row in parseRows)
        {
            BankCsvParser.ApplyMapping(row, columnMapping);
        }

        var existingHashes = await dbContext.BankTransactions.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.BankAccountId == account.Id)
            .Select(t => t.RowHash)
            .ToListAsync(cancellationToken);
        var hashSet = existingHashes.ToHashSet(StringComparer.Ordinal);

        var previewRows = new List<BankImportPreviewRowDto>();
        var valid = 0;
        var invalid = 0;
        var dup = 0;
        foreach (var row in parseRows)
        {
            var validated = BankCsvParser.ValidateRow(row, account.Currency);
            var isDup = false;
            if (validated.IsValid)
            {
                var normalized = BankTransactionRowIdentity.NormalizeReference(
                    validated.Reference,
                    validated.Description);
                var rowHash = BankTransactionRowIdentity.ComputeRowHash(
                    account.Id,
                    validated.TransactionDate,
                    validated.Amount,
                    validated.Direction,
                    validated.ExternalTransactionReference,
                    normalized,
                    validated.Counterparty);
                isDup = hashSet.Contains(rowHash);
                if (isDup)
                {
                    dup++;
                }
                else
                {
                    valid++;
                }
            }
            else
            {
                invalid++;
            }

            previewRows.Add(new BankImportPreviewRowDto
            {
                RowNumber = row.RowNumber,
                IsValid = validated.IsValid,
                Error = validated.Error,
                IsDuplicate = isDup,
                TransactionDate = validated.IsValid ? validated.TransactionDate : null,
                Amount = validated.IsValid ? validated.Amount : null,
                Direction = validated.Direction,
                Reference = validated.Reference,
                Description = validated.Description,
                Counterparty = validated.Counterparty
            });
        }

        var fileImported = await dbContext.BankImportBatches.AsNoTracking()
            .AnyAsync(
                b => b.TenantId == tenantId
                     && b.BankAccountId == account.Id
                     && b.ContentChecksum == checksum,
                cancellationToken);

        return new BankImportPreviewDto
        {
            FileName = fileName,
            ContentChecksum = checksum,
            Headers = headers,
            ColumnMapping = columnMapping,
            Rows = previewRows,
            ValidCount = valid,
            InvalidCount = invalid,
            DuplicateCount = dup,
            FileAlreadyImported = fileImported
        };
    }

    public async Task<(BankImportBatchDto? Batch, string? Error)> CommitAsync(
        int tenantId,
        Guid bankAccountPublicId,
        string fileName,
        string contentChecksum,
        string csvContent,
        BankCsvColumnMapping mapping,
        IReadOnlyList<int> acceptedRowNumbers,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var account = await bankAccounts.RequireAsync(tenantId, bankAccountPublicId, cancellationToken);
        var checksum = ComputeChecksum(csvContent);
        if (!string.Equals(checksum, contentChecksum, StringComparison.OrdinalIgnoreCase))
        {
            return (null, "File checksum mismatch. Re-upload the file.");
        }

        if (await dbContext.BankImportBatches.AnyAsync(
                b => b.TenantId == tenantId && b.BankAccountId == account.Id && b.ContentChecksum == checksum,
                cancellationToken))
        {
            return (null, "This file has already been imported.");
        }

        var parseRows = BankCsvParser.ParseRows(csvContent);
        foreach (var row in parseRows)
        {
            BankCsvParser.ApplyMapping(row, mapping);
        }

        var accepted = new HashSet<int>(acceptedRowNumbers);
        var batch = new BankImportBatch
        {
            TenantId = tenantId,
            PublicId = Guid.NewGuid(),
            BankAccountId = account.Id,
            FileName = fileName,
            ContentChecksum = checksum,
            ImportedAt = timeProvider.GetUtcNow(),
            ImportedBy = actorUserId,
            RowCount = parseRows.Count,
            Status = BankImportBatchStatuses.Committed
        };

        var existingHashes = await dbContext.BankTransactions
            .Where(t => t.TenantId == tenantId && t.BankAccountId == account.Id)
            .Select(t => t.RowHash)
            .ToListAsync(cancellationToken);
        var hashSet = existingHashes.ToHashSet(StringComparer.Ordinal);

        foreach (var row in parseRows)
        {
            if (!accepted.Contains(row.RowNumber))
            {
                batch.RejectedCount++;
                continue;
            }

            var validated = BankCsvParser.ValidateRow(row, account.Currency);
            if (!validated.IsValid)
            {
                batch.RejectedCount++;
                continue;
            }

            var normalized = BankTransactionRowIdentity.NormalizeReference(
                validated.Reference,
                validated.Description);
            var rowHash = BankTransactionRowIdentity.ComputeRowHash(
                account.Id,
                validated.TransactionDate,
                validated.Amount,
                validated.Direction,
                validated.ExternalTransactionReference,
                normalized,
                validated.Counterparty);

            if (!hashSet.Add(rowHash))
            {
                batch.DuplicateCount++;
                continue;
            }

            var txn = new BankTransaction
            {
                TenantId = tenantId,
                PublicId = Guid.NewGuid(),
                BankAccountId = account.Id,
                TransactionDate = validated.TransactionDate,
                ValueDate = validated.ValueDate,
                Amount = validated.Amount,
                Direction = validated.Direction,
                Currency = validated.Currency,
                Reference = validated.Reference,
                Description = validated.Description,
                Counterparty = validated.Counterparty,
                ExternalTransactionReference = validated.ExternalTransactionReference,
                NormalizedReference = normalized,
                Status = BankTransactionStatuses.Unreconciled,
                RowHash = rowHash,
                CreatedAt = timeProvider.GetUtcNow()
            };

            batch.Transactions.Add(txn);
            batch.AcceptedCount++;
        }

        if (batch.AcceptedCount == 0)
        {
            return (null, "No new transactions to import.");
        }

        dbContext.BankImportBatches.Add(batch);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var txn in batch.Transactions)
        {
            await matchingEngine.GenerateSuggestionsAsync(tenantId, txn.Id, cancellationToken);
        }

        await audit.LogAsync(
            "BankImportBatch",
            batch.PublicId.ToString("D"),
            "BANK_IMPORT_CREATED",
            null,
            new { batch.FileName, batch.AcceptedCount, batch.DuplicateCount },
            "Bank statement imported.",
            cancellationToken,
            tenantId);

        return (MapBatch(batch), null);
    }

    public async Task<List<BankImportBatchDto>> ListBatchesAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.BankImportBatches.AsNoTracking()
            .Where(b => b.TenantId == tenantId)
            .OrderByDescending(b => b.ImportedAt)
            .Take(50)
            .Select(b => new BankImportBatchDto
            {
                PublicId = b.PublicId,
                FileName = b.FileName,
                ImportedAt = b.ImportedAt,
                AcceptedCount = b.AcceptedCount,
                RejectedCount = b.RejectedCount,
                DuplicateCount = b.DuplicateCount,
                Status = b.Status
            })
            .ToListAsync(cancellationToken);
    }

    private static BankImportBatchDto MapBatch(BankImportBatch batch) =>
        new()
        {
            PublicId = batch.PublicId,
            FileName = batch.FileName,
            ImportedAt = batch.ImportedAt,
            AcceptedCount = batch.AcceptedCount,
            RejectedCount = batch.RejectedCount,
            DuplicateCount = batch.DuplicateCount,
            Status = batch.Status
        };

    private static string ComputeChecksum(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes);
    }

    private static BankCsvColumnMapping GuessMapping(List<string> headers)
    {
        string? Find(params string[] candidates)
        {
            foreach (var c in candidates)
            {
                var hit = headers.FirstOrDefault(h =>
                    h.Contains(c, StringComparison.OrdinalIgnoreCase));
                if (hit is not null)
                {
                    return hit;
                }
            }

            return null;
        }

        return new BankCsvColumnMapping
        {
            Date = Find("date", "transaction date"),
            ValueDate = Find("value date"),
            Amount = Find("amount"),
            Debit = Find("debit", "paid out"),
            Credit = Find("credit", "paid in"),
            Reference = Find("reference", "ref"),
            Description = Find("description", "narrative", "details"),
            Counterparty = Find("counterparty", "name", "payee"),
            ExternalId = Find("transaction id", "id"),
            Currency = Find("currency")
        };
    }
}
