using System.Text;
using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Documents;
using CareHome.Api.Dtos.Sage;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Export
{
    public class SageExportService(
        CareHomeDbContext dbContext,
        IDocumentStore documents,
        AuditService audit,
        Sage50ColumnMap columnMap,
        UserAccessService userAccess,
        ILogger<SageExportService> logger)
    {
        public async Task<(SageExportPreviewResponse Preview, List<Invoice> Invoices)> PreviewAsync(
            int tenantId,
            SageExportRequest request,
            CancellationToken cancellationToken = default)
        {
            var invoices = await LoadEligibleInvoicesAsync(tenantId, request, cancellationToken);
            return (BuildPreview(invoices, request.IncludeAlreadyExported), invoices);
        }

        public async Task<(SageExportBatch? Batch, string? Error)> ExportAsync(
            int tenantId,
            Guid tenantPublicId,
            SageExportRequest request,
            string? userId,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await SqlAppLock.AcquireExclusiveAsync(
                dbContext.Database,
                $"sage-export-{tenantId}",
                cancellationToken);

            // Re-query under the lock so concurrent exports cannot claim the same invoices.
            var invoices = await LoadEligibleInvoicesAsync(tenantId, request, cancellationToken);
            var preview = BuildPreview(invoices, request.IncludeAlreadyExported);
            if (!preview.CanExport)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogWarning(
                    "Sage export blocked. TenantId={TenantId} Reason={Reason}",
                    tenantId,
                    preview.Errors.FirstOrDefault() ?? "Export is blocked until validation errors are resolved.");
                return (null, preview.Errors.FirstOrDefault() ?? "Export is blocked until validation errors are resolved.");
            }

            var eligibleInvoices = invoices
                .Where(i => preview.Rows.Any(r => r.InvoiceId == i.Id && r.Eligible))
                .ToList();

            if (eligibleInvoices.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (null, "There are no eligible invoices to export.");
            }

            var exportedAt = DateTimeOffset.UtcNow;
            var fileName = $"sage50-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            var batch = new SageExportBatch
            {
                TenantId = tenantId,
                ExportedAt = exportedAt,
                ExportedByUserId = userId,
                DateFrom = request.DateFrom,
                DateTo = request.DateTo,
                CompanyId = request.CompanyId,
                RecordCount = eligibleInvoices.Sum(x => x.Lines.Count),
                FileName = fileName,
                FilePath = string.Empty,
                Status = "Completed"
            };

            dbContext.SageExportBatches.Add(batch);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var invoice in eligibleInvoices)
            {
                invoice.SageExportBatchId = batch.Id;
                invoice.SageExportedAt = exportedAt;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Write file after durable DB marks so a crash leaves a re-exportable batch, not unmarked invoices.
            var csv = columnMap.BuildCsv(eligibleInvoices);
            try
            {
                var path = await documents.SaveAsync(
                    TenantDocumentPaths.Folder(tenantPublicId, "sage-exports"),
                    fileName,
                    Encoding.UTF8.GetBytes(csv),
                    cancellationToken);

                batch.FilePath = path;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Sage CSV write failed after DB commit. TenantId={TenantId} BatchId={BatchId}",
                    tenantId,
                    batch.Id);
                batch.Status = "FileMissing";
                await dbContext.SaveChangesAsync(cancellationToken);
                return (batch, null);
            }

            await audit.LogAsync(
                "SageExport",
                batch.Id.ToString(),
                "Export",
                null,
                new { batch.FileName, batch.RecordCount },
                $"Exported {batch.RecordCount} Sage50 rows.",
                cancellationToken);

            logger.LogInformation(
                "Sage export completed. TenantId={TenantId} BatchId={BatchId} RecordCount={RecordCount}",
                tenantId,
                batch.Id,
                batch.RecordCount);

            return (batch, null);
        }

        public async Task<(SageExportBatch? Batch, string? Error)> RetryFileWriteAsync(
            int tenantId,
            Guid tenantPublicId,
            int batchId,
            CancellationToken cancellationToken = default)
        {
            var batch = await dbContext.SageExportBatches
                .FirstOrDefaultAsync(x => x.Id == batchId && x.TenantId == tenantId, cancellationToken);

            if (batch is null)
            {
                return (null, "Export batch was not found.");
            }

            if (!string.Equals(batch.Status, "FileMissing", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(batch.FilePath)
                && await documents.ReadAsync(batch.FilePath, cancellationToken) is not null)
            {
                return (null, "This export batch already has a CSV file.");
            }

            var invoices = await dbContext.Invoices
                .Include(x => x.Lines)
                .Where(x => x.TenantId == tenantId && x.SageExportBatchId == batchId)
                .OrderBy(x => x.InvoiceNumber)
                .ToListAsync(cancellationToken);

            if (invoices.Count == 0)
            {
                return (null, "No invoices are linked to this export batch.");
            }

            var csv = columnMap.BuildCsv(invoices);
            try
            {
                var fileName = string.IsNullOrWhiteSpace(batch.FileName)
                    ? $"sage50-{batch.Id}.csv"
                    : batch.FileName;
                var path = await documents.SaveAsync(
                    TenantDocumentPaths.Folder(tenantPublicId, "sage-exports"),
                    fileName,
                    Encoding.UTF8.GetBytes(csv),
                    cancellationToken);

                batch.FilePath = path;
                batch.FileName = fileName;
                batch.Status = "Completed";
                await dbContext.SaveChangesAsync(cancellationToken);

                await audit.LogAsync(
                    "SageExport",
                    batch.Id.ToString(),
                    "RetryFile",
                    null,
                    new { batch.FileName },
                    "Regenerated Sage50 CSV after a prior file write failure.",
                    cancellationToken);

                return (batch, null);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Sage CSV retry write failed. TenantId={TenantId} BatchId={BatchId}",
                    tenantId,
                    batchId);
                batch.Status = "FileMissing";
                await dbContext.SaveChangesAsync(cancellationToken);
                return (null, "The CSV file could not be written. The export remains recorded. Try Retry CSV again.");
            }
        }

        private async Task<List<Invoice>> LoadEligibleInvoicesAsync(
            int tenantId,
            SageExportRequest request,
            CancellationToken cancellationToken)
        {
            var allowedHomes = await userAccess.GetAllowedCareHomeIdsAsync(cancellationToken);

            var query = dbContext.Invoices
                .Include(x => x.Lines)
                    .ThenInclude(l => l.CreditNoteLines)
                    .ThenInclude(c => c.CreditNote)
                .Where(x => x.TenantId == tenantId)
                .Where(x => x.Status != "Void")
                .Where(x => x.InvoiceDate >= request.DateFrom && x.InvoiceDate <= request.DateTo);

            if (allowedHomes is not null)
            {
                query = query.Where(x => allowedHomes.Contains(x.CareHomeId));
            }

            if (request.CompanyId.HasValue)
            {
                query = query.Where(x => x.CompanyId == request.CompanyId.Value);
            }

            if (request.CareHomeId.HasValue)
            {
                // Only honour requested home if the caller is allowed to see it.
                if (allowedHomes is null || allowedHomes.Contains(request.CareHomeId.Value))
                {
                    query = query.Where(x => x.CareHomeId == request.CareHomeId.Value);
                }
                else
                {
                    query = query.Where(_ => false);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(x => x.Status == request.Status);
            }

            return await query.OrderBy(x => x.InvoiceNumber).ToListAsync(cancellationToken);
        }

        private static SageExportPreviewResponse BuildPreview(List<Invoice> invoices, bool includeAlreadyExported)
        {
            var rows = new List<SageExportRowDto>();
            var errors = new List<string>();

            foreach (var invoice in invoices)
            {
                if (invoice.SageExportBatchId is not null && !includeAlreadyExported)
                {
                    rows.Add(new SageExportRowDto
                    {
                        InvoiceId = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        Eligible = false,
                        Reason = "Already exported."
                    });
                    continue;
                }

                foreach (var line in invoice.Lines)
                {
                    var issues = new List<string>();
                    if (string.IsNullOrWhiteSpace(line.SnapshotSageId))
                    {
                        issues.Add("Sage ID is missing.");
                    }

                    if (string.IsNullOrWhiteSpace(line.SnapshotNominalCode))
                    {
                        issues.Add("Nominal code is missing.");
                    }

                    var eligible = issues.Count == 0;
                    if (!eligible)
                    {
                        var lineRef = string.IsNullOrWhiteSpace(line.Description)
                            ? $"Invoice {invoice.InvoiceNumber}"
                            : $"Invoice {invoice.InvoiceNumber} ({line.Description})";
                        errors.Add($"{lineRef}: {string.Join(" ", issues)}");
                    }

                    var netAmount = Sage50ColumnMap.NetLineAmount(line);
                    if (netAmount == 0m)
                    {
                        continue;
                    }

                    rows.Add(new SageExportRowDto
                    {
                        InvoiceId = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        SageId = line.SnapshotSageId,
                        NominalCode = line.SnapshotNominalCode,
                        Amount = netAmount,
                        Eligible = eligible,
                        Reason = eligible ? null : string.Join(" ", issues)
                    });
                }
            }

            return new SageExportPreviewResponse
            {
                Rows = rows,
                EligibleCount = rows.Count(x => x.Eligible),
                BlockedCount = rows.Count(x => !x.Eligible),
                Errors = errors,
                CanExport = rows.Any(x => x.Eligible) && errors.Count == 0
            };
        }
    }
}
