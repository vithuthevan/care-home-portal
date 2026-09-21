using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CareHome.Api.Abstractions;
using CareHome.Api.Data;
using CareHome.Api.Models;
using CareHome.Api.Payments.Domain;
using CareHome.Api.Payments.Dtos;
using CareHome.Api.Payments.Services;
using CareHome.Api.Reconciliation.Domain;
using CareHome.Api.Remittance.Dtos;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Remittance.Services;

public sealed class RemittanceService(
    CareHomeDbContext dbContext,
    PaymentService payments,
    IAuditWriter audit,
    TimeProvider timeProvider)
{
    public async Task<List<RemittanceBatchListDto>> ListAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.RemittanceBatches.AsNoTracking()
            .Where(b => b.TenantId == tenantId)
            .OrderByDescending(b => b.CreatedAt)
            .Take(100)
            .Select(b => new RemittanceBatchListDto
            {
                PublicId = b.PublicId,
                Status = b.Status,
                PaymentReference = b.PaymentReference,
                ReceivedDate = b.ReceivedDate,
                SourceFileName = b.SourceFileName,
                FunderName = b.FundingAuthority != null ? b.FundingAuthority.Name : null,
                LineCount = b.Lines.Count,
                MatchedLineCount = b.Lines.Count(l => l.Status == RemittanceLineStatuses.Matched),
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RemittanceBatchDetailDto?> GetAsync(
        int tenantId,
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        var batch = await dbContext.RemittanceBatches.AsNoTracking()
            .Include(b => b.FundingAuthority)
            .Include(b => b.Payment)
            .Include(b => b.Lines)
            .ThenInclude(l => l.MatchedInvoice)
            .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.PublicId == publicId, cancellationToken);

        return batch is null ? null : MapDetail(batch);
    }

    public async Task<RemittanceBatchDetailDto> ImportCsvAsync(
        int tenantId,
        string fileName,
        Stream content,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content);
        var text = await reader.ReadToEndAsync(cancellationToken);
        var checksum = ComputeChecksum(text);
        if (await dbContext.RemittanceBatches.AnyAsync(
                b => b.TenantId == tenantId && b.ContentChecksum == checksum,
                cancellationToken))
        {
            throw new InvalidOperationException("This remittance file was already imported.");
        }

        var lines = ParseCsv(text);
        return await CreateBatchAsync(tenantId, fileName, checksum, "CSV", lines, actorUserId, cancellationToken);
    }

    public async Task<RemittanceBatchDetailDto> ImportXlsxAsync(
        int tenantId,
        string fileName,
        Stream content,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(content);
        var sheet = workbook.Worksheets.First();
        var rows = sheet.RangeUsed()?.RowsUsed().Skip(1).ToList() ?? [];
        var lines = new List<ParsedRemittanceLine>();
        var lineNo = 0;
        foreach (var row in rows)
        {
            lineNo++;
            lines.Add(new ParsedRemittanceLine
            {
                LineNumber = lineNo,
                InvoiceReference = row.Cell(1).GetString().Trim(),
                ResidentReference = row.Cell(2).GetString().Trim(),
                GrossAmount = ParseDecimal(row.Cell(3).GetString()),
                PaidAmount = ParseDecimal(row.Cell(4).GetString()),
                DeductionAmount = ParseDecimal(row.Cell(5).GetString()),
                DeductionReasonCode = row.Cell(6).GetString().Trim(),
                Notes = row.Cell(7).GetString().Trim()
            });
        }

        var checksum = ComputeChecksum($"{fileName}:{lines.Count}:{lines.Sum(l => l.PaidAmount ?? 0)}");
        return await CreateBatchAsync(tenantId, fileName, checksum, "XLSX", lines, actorUserId, cancellationToken);
    }

    public async Task<RemittanceBatchDetailDto> UpdateAsync(
        int tenantId,
        Guid publicId,
        UpdateRemittanceBatchRequest request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var batch = await dbContext.RemittanceBatches
            .Include(b => b.Lines)
            .Include(b => b.FundingAuthority)
            .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.PublicId == publicId, cancellationToken)
            ?? throw new InvalidOperationException("Remittance batch not found.");

        if (batch.Status == RemittanceBatchStatuses.Confirmed)
        {
            throw new InvalidOperationException("Confirmed remittances cannot be edited.");
        }

        batch.FundingAuthorityId = request.FundingAuthorityId;
        batch.PaymentReference = request.PaymentReference?.Trim();
        batch.ReceivedDate = request.ReceivedDate;
        batch.UpdatedAt = timeProvider.GetUtcNow();

        foreach (var update in request.Lines)
        {
            var line = batch.Lines.FirstOrDefault(l => l.PublicId == update.LinePublicId)
                ?? throw new InvalidOperationException("Remittance line not found.");
            line.InvoiceReference = update.InvoiceReference?.Trim() ?? line.InvoiceReference;
            line.PaidAmount = update.PaidAmount ?? line.PaidAmount;
            line.DeductionAmount = update.DeductionAmount ?? line.DeductionAmount;
            line.DeductionReasonCode = update.DeductionReasonCode?.Trim() ?? line.DeductionReasonCode;
            if (update.MatchedInvoicePublicId is Guid invPublicId)
            {
                var inv = await dbContext.Invoices.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.PublicId == invPublicId, cancellationToken)
                    ?? throw new InvalidOperationException("Invoice not found.");
                line.MatchedInvoiceId = inv.Id;
                line.Status = RemittanceLineStatuses.Matched;
            }

            line.UpdatedAt = timeProvider.GetUtcNow();
        }

        await AutoMatchBatchAsync(tenantId, batch, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapDetail(batch);
    }

    public async Task<RemittanceBatchDetailDto> ConfirmAsync(
        int tenantId,
        Guid publicId,
        ConfirmRemittanceRequest request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var batch = await dbContext.RemittanceBatches
            .Include(b => b.Lines)
            .ThenInclude(l => l.MatchedInvoice)
            .Include(b => b.FundingAuthority)
            .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.PublicId == publicId, cancellationToken)
            ?? throw new InvalidOperationException("Remittance batch not found.");

        if (batch.Status == RemittanceBatchStatuses.Confirmed)
        {
            throw new InvalidOperationException("Remittance is already confirmed.");
        }

        var totalPaid = Money.Round(batch.Lines.Sum(l => l.PaidAmount ?? 0));
        if (totalPaid <= 0)
        {
            throw new InvalidOperationException("Remittance has no paid amounts to confirm.");
        }

        var received = batch.ReceivedDate ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var externalRef = $"remittance-batch:{batch.PublicId:D}";
        var payment = await payments.CreateRemittancePaymentAsync(
            tenantId,
            totalPaid,
            "GBP",
            received,
            externalRef,
            batch.PaymentReference,
            batch.FundingAuthorityId,
            null,
            actorUserId,
            cancellationToken);

        var allocations = batch.Lines
            .Where(l => l.MatchedInvoice is not null && l.PaidAmount is > 0)
            .Select(l => new PaymentAllocationLineRequest
            {
                InvoicePublicId = l.MatchedInvoice!.PublicId,
                Amount = Money.Round(l.PaidAmount!.Value)
            })
            .ToList();

        if (allocations.Count > 0)
        {
            await payments.AllocateAsync(
                tenantId,
                payment.PublicId,
                new AllocatePaymentRequest { Allocations = allocations },
                actorUserId,
                cancellationToken);
        }

        var paymentEntity = await dbContext.Payments
            .FirstAsync(p => p.TenantId == tenantId && p.PublicId == payment.PublicId, cancellationToken);

        batch.PaymentId = paymentEntity.Id;
        batch.Status = RemittanceBatchStatuses.Confirmed;
        batch.ConfirmedAt = timeProvider.GetUtcNow();
        batch.ConfirmedBy = actorUserId;
        batch.UpdatedAt = timeProvider.GetUtcNow();

        foreach (var line in batch.Lines)
        {
            line.Status = line.MatchedInvoiceId is not null && line.PaidAmount is > 0
                ? RemittanceLineStatuses.Confirmed
                : RemittanceLineStatuses.Unmatched;
            line.UpdatedAt = timeProvider.GetUtcNow();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await audit.LogAsync(
            "RemittanceBatch",
            batch.PublicId.ToString("D"),
            "REMITTANCE_CONFIRMED",
            null,
            new { payment.PublicId, totalPaid },
            "Remittance confirmed and payment applied.",
            cancellationToken,
            tenantId);

        return MapDetail(batch);
    }

    private async Task<RemittanceBatchDetailDto> CreateBatchAsync(
        int tenantId,
        string fileName,
        string checksum,
        string format,
        List<ParsedRemittanceLine> parsed,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var batch = new RemittanceBatch
        {
            TenantId = tenantId,
            PublicId = Guid.NewGuid(),
            Status = RemittanceBatchStatuses.Imported,
            SourceFormat = format,
            SourceFileName = fileName,
            ContentChecksum = checksum,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actorUserId
        };

        foreach (var row in parsed)
        {
            batch.Lines.Add(new RemittanceLine
            {
                TenantId = tenantId,
                PublicId = Guid.NewGuid(),
                LineNumber = row.LineNumber,
                Status = RemittanceLineStatuses.Pending,
                InvoiceReference = row.InvoiceReference,
                ResidentReference = row.ResidentReference,
                GrossAmount = row.GrossAmount,
                PaidAmount = row.PaidAmount,
                DeductionAmount = row.DeductionAmount,
                DeductionReasonCode = row.DeductionReasonCode,
                Notes = row.Notes,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        dbContext.RemittanceBatches.Add(batch);
        await AutoMatchBatchAsync(tenantId, batch, cancellationToken);
        batch.Status = batch.Lines.All(l => l.Status == RemittanceLineStatuses.Matched)
            ? RemittanceBatchStatuses.Matched
            : batch.Lines.Any(l => l.Status == RemittanceLineStatuses.Matched)
                ? RemittanceBatchStatuses.PartiallyMatched
                : RemittanceBatchStatuses.NeedsReview;

        await dbContext.SaveChangesAsync(cancellationToken);

        await audit.LogAsync(
            "RemittanceBatch",
            batch.PublicId.ToString("D"),
            "REMITTANCE_IMPORTED",
            null,
            new { fileName, batch.Lines.Count },
            "Remittance imported.",
            cancellationToken,
            tenantId);

        return MapDetail(batch);
    }

    private async Task AutoMatchBatchAsync(
        int tenantId,
        RemittanceBatch batch,
        CancellationToken cancellationToken)
    {
        var invoiceRows = await dbContext.Invoices.AsNoTracking()
            .Where(i => i.TenantId == tenantId && i.Status != InvoiceStatuses.Void)
            .Select(i => new
            {
                i.Id,
                i.PublicId,
                i.InvoiceNumber,
                i.InvoiceDate,
                i.TotalAmount,
                i.PaymentStatus,
                FunderName = i.FundingAuthority.Name,
                i.FundingAuthorityId
            })
            .Take(2000)
            .ToListAsync(cancellationToken);

        var ids = invoiceRows.Select(i => i.Id).ToList();
        var allocated = await dbContext.PaymentAllocations.AsNoTracking()
            .Where(a => a.TenantId == tenantId && !a.IsReversed && ids.Contains(a.InvoiceId)
                        && a.Payment.Status != PaymentEntityStatuses.Reversed)
            .GroupBy(a => a.InvoiceId)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(a => a.AllocatedAmount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => Money.Round(x.Total), cancellationToken);

        var targets = invoiceRows
            .Select(i =>
            {
                var outstanding = InvoiceAllocationCapacity.RemainingCollectible(
                    i.TotalAmount,
                    0,
                    i.PaymentStatus,
                    allocated.GetValueOrDefault(i.Id));
                return new InvoiceMatchTarget(
                    i.Id,
                    i.PublicId,
                    i.InvoiceNumber,
                    outstanding,
                    i.InvoiceDate,
                    i.FunderName,
                    i.FundingAuthorityId);
            })
            .Where(t => t.Outstanding > 0)
            .ToList();

        foreach (var line in batch.Lines)
        {
            if (line.MatchedInvoiceId is not null)
            {
                continue;
            }

            var refText = line.InvoiceReference ?? string.Empty;
            var amount = line.PaidAmount ?? line.GrossAmount ?? 0;
            InvoiceMatchTarget? bestTarget = null;
            ReconciliationMatchCandidate? bestScore = null;
            foreach (var target in targets)
            {
                if (refText.Length > 0
                    && !target.InvoiceNumber.Contains(refText, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var scored = ReconciliationMatchScorer.ScoreSingleInvoice(
                    amount,
                    batch.ReceivedDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    refText,
                    batch.PaymentReference,
                    target);
                if (scored is null)
                {
                    continue;
                }

                if (bestScore is null || scored.TotalScore > bestScore.TotalScore)
                {
                    bestTarget = target;
                    bestScore = scored;
                }
            }

            if (bestTarget is null || bestScore is null)
            {
                line.Status = RemittanceLineStatuses.Unmatched;
                continue;
            }

            line.MatchedInvoiceId = bestTarget.InvoiceId;
            line.MatchConfidence = bestScore.TotalScore;
            line.Status = RemittanceLineStatuses.Matched;
        }
    }

    private static RemittanceBatchDetailDto MapDetail(RemittanceBatch batch) =>
        new()
        {
            PublicId = batch.PublicId,
            Status = batch.Status,
            FundingAuthorityId = batch.FundingAuthorityId,
            FunderName = batch.FundingAuthority?.Name,
            PaymentReference = batch.PaymentReference,
            ReceivedDate = batch.ReceivedDate,
            PaymentPublicId = batch.Payment?.PublicId,
            Lines = batch.Lines.OrderBy(l => l.LineNumber).Select(l => new RemittanceLineDto
            {
                PublicId = l.PublicId,
                LineNumber = l.LineNumber,
                Status = l.Status,
                InvoiceReference = l.InvoiceReference,
                ResidentReference = l.ResidentReference,
                GrossAmount = l.GrossAmount,
                PaidAmount = l.PaidAmount,
                DeductionAmount = l.DeductionAmount,
                DeductionReasonCode = l.DeductionReasonCode,
                Notes = l.Notes,
                MatchedInvoicePublicId = l.MatchedInvoice?.PublicId,
                MatchedInvoiceNumber = l.MatchedInvoice?.InvoiceNumber,
                MatchConfidence = l.MatchConfidence
            }).ToList()
        };

    private static List<ParsedRemittanceLine> ParseCsv(string csv)
    {
        using var reader = new StringReader(csv);
        var header = reader.ReadLine();
        if (header is null)
        {
            return [];
        }

        var headers = header.Split(',').Select(h => h.Trim()).ToList();
        int Idx(string name) => headers.FindIndex(h => h.Contains(name, StringComparison.OrdinalIgnoreCase));

        var invIdx = Idx("invoice");
        var resIdx = Idx("resident");
        var grossIdx = Idx("gross");
        var paidIdx = Idx("paid");
        var dedIdx = Idx("deduction");
        var reasonIdx = Idx("reason");
        var notesIdx = Idx("notes");

        var lines = new List<ParsedRemittanceLine>();
        var lineNo = 0;
        string? row;
        while ((row = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(row))
            {
                continue;
            }

            lineNo++;
            var cells = row.Split(',');
            string Cell(int idx) => idx >= 0 && idx < cells.Length ? cells[idx].Trim() : string.Empty;

            lines.Add(new ParsedRemittanceLine
            {
                LineNumber = lineNo,
                InvoiceReference = Cell(invIdx),
                ResidentReference = Cell(resIdx),
                GrossAmount = ParseDecimal(Cell(grossIdx)),
                PaidAmount = ParseDecimal(Cell(paidIdx)),
                DeductionAmount = ParseDecimal(Cell(dedIdx)),
                DeductionReasonCode = Cell(reasonIdx),
                Notes = Cell(notesIdx)
            });
        }

        return lines;
    }

    private static decimal? ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            ? Money.Round(d)
            : decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out d)
                ? Money.Round(d)
                : null;
    }

    private static string ComputeChecksum(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes);
    }

    private sealed class ParsedRemittanceLine
    {
        public int LineNumber { get; init; }

        public string? InvoiceReference { get; init; }

        public string? ResidentReference { get; init; }

        public decimal? GrossAmount { get; init; }

        public decimal? PaidAmount { get; init; }

        public decimal? DeductionAmount { get; init; }

        public string? DeductionReasonCode { get; init; }

        public string? Notes { get; init; }
    }
}
