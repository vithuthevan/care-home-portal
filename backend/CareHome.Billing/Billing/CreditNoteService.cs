using CareHome.Api.Abstractions;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.CreditNotes;
using CareHome.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Billing
{
    public class CreditNoteService(
        CareHomeDbContext dbContext,
        IDocumentSequence sequences,
        IAuditWriter audit,
        ICareHomeAccessScope userAccess,
        ILogger<CreditNoteService> logger)
    {
        public async Task<CreditNotePreviewResponse> PreviewAsync(
            int tenantId,
            CreditNotePreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            var eligible = await LoadEligibleLinesAsync(tenantId, request, cancellationToken);
            var exceptions = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                exceptions.Add("A reason is required.");
            }

            if (request.PeriodEnd < request.PeriodStart)
            {
                exceptions.Add("Period end cannot be before start.");
            }

            if (request.InvoiceId.HasValue && eligible.Count == 0)
            {
                exceptions.Add(
                    "No invoice lines on the selected invoice match this period, or the invoice cannot be credited.");
            }

            var lines = new List<CreditNotePreviewLineDto>();
            foreach (var line in eligible)
            {
                var remaining = RemainingCreditable(line);
                if (remaining <= 0)
                {
                    continue;
                }

                var requested = request.LineAmounts?.TryGetValue(line.Id, out var amount) == true
                    ? amount
                    : remaining;

                if (requested > remaining)
                {
                    exceptions.Add(
                        $"Credit for invoice {line.Invoice.InvoiceNumber} ({line.Description}) cannot exceed the remaining invoiced amount of {remaining:0.00}.");
                }

                if (requested <= 0)
                {
                    continue;
                }

                lines.Add(new CreditNotePreviewLineDto
                {
                    InvoiceLineId = line.Id,
                    InvoiceNumber = line.Invoice.InvoiceNumber,
                    ClientName = line.SnapshotClientName,
                    Description = line.Description,
                    ServiceFrom = line.ServicePeriodStart,
                    ServiceTo = line.ServicePeriodEnd,
                    InvoicedAmount = line.LineAmount,
                    AlreadyCredited = Money.Round(line.CreditNoteLines
                        .Where(c => c.CreditNote.Status != CreditNoteStatuses.Void)
                        .Sum(c => -c.Amount)),
                    RemainingAmount = remaining,
                    CreditAmount = Money.Round(requested)
                });
            }

            if (lines.Select(x => x.InvoiceNumber).Distinct().Count() > 1)
            {
                exceptions.Add(
                    "A credit note cannot span more than one invoice. Narrow the resident or period so only one invoice is included.");
            }

            return new CreditNotePreviewResponse
            {
                Lines = lines,
                TotalCredit = Money.Round(lines.Sum(x => x.CreditAmount)),
                Exceptions = exceptions,
                CanGenerate = exceptions.Count == 0 && lines.Count > 0
            };
        }

        public async Task<(CreditNote? Note, string? Error)> GenerateAsync(
            int tenantId,
            CreditNotePreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            // Serialize credit generates per tenant, then rebuild preview under the lock so
            // remaining-balance checks cannot race with a concurrent credit.
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await SqlAppLock.AcquireExclusiveAsync(
                dbContext.Database,
                $"credit-generate-{tenantId}",
                cancellationToken);

            var preview = await PreviewAsync(tenantId, request, cancellationToken);
            if (!preview.CanGenerate)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogWarning(
                    "Credit generate blocked. TenantId={TenantId} Reason={Reason}",
                    tenantId,
                    preview.Exceptions.FirstOrDefault() ?? "Credit note cannot be generated.");
                return (null, preview.Exceptions.FirstOrDefault() ?? "Credit note cannot be generated.");
            }

            var lineIds = preview.Lines.Select(l => l.InvoiceLineId).ToList();
            var freshLines = await dbContext.InvoiceLines
                .Include(x => x.Invoice)
                .Include(x => x.CreditNoteLines)
                    .ThenInclude(x => x.CreditNote)
                .Where(x => lineIds.Contains(x.Id) && x.Invoice.TenantId == tenantId)
                .ToListAsync(cancellationToken);

            if (freshLines.Count != lineIds.Count)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (null, "One or more invoice lines are no longer available for credit. Refresh and try again.");
            }

            var invoiceIds = freshLines.Select(x => x.InvoiceId).Distinct().ToList();
            if (invoiceIds.Count != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (null, "Credit lines must belong to a single invoice. Narrow the resident or period and generate again.");
            }

            var invoice = freshLines[0].Invoice;
            if (invoice.Status == InvoiceStatuses.Void)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (null, "Cannot credit a void invoice.");
            }

            foreach (var previewLine in preview.Lines)
            {
                var fresh = freshLines.First(x => x.Id == previewLine.InvoiceLineId);
                var remaining = RemainingCreditable(fresh);
                if (previewLine.CreditAmount > remaining)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return (null,
                        $"Credit for invoice {invoice.InvoiceNumber} ({fresh.Description}) cannot exceed the remaining invoiced amount of {remaining:0.00}.");
                }
            }

            var number = await sequences.NextAsync(tenantId, DocumentTypes.CreditNote, cancellationToken);
            var now = DateTimeOffset.UtcNow;

            var note = new CreditNote
            {
                TenantId = tenantId,
                CreditNoteNumber = number,
                InvoiceId = invoice.Id,
                CreditNoteDate = request.CreditNoteDate == default
                    ? DateOnly.FromDateTime(DateTime.UtcNow.Date)
                    : request.CreditNoteDate,
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                Reason = request.Reason.Trim(),
                Status = CreditNoteStatuses.Generated,
                CreatedAt = now,
                GeneratedAt = now,
                RecipientEmail = invoice.RecipientEmail
            };

            foreach (var line in preview.Lines)
            {
                note.Lines.Add(new CreditNoteLine
                {
                    InvoiceLineId = line.InvoiceLineId,
                    ServicePeriodStart = line.ServiceFrom,
                    ServicePeriodEnd = line.ServiceTo,
                    Amount = Money.Round(-line.CreditAmount),
                    Description = line.Description
                });
            }

            note.TotalAmount = Money.Round(note.Lines.Sum(x => x.Amount));
            dbContext.CreditNotes.Add(note);
            await dbContext.SaveChangesAsync(cancellationToken);

            await audit.LogAsync(
                "CreditNote",
                note.Id.ToString(),
                "Generate",
                null,
                new { note.CreditNoteNumber, note.TotalAmount, request.Reason },
                $"Generated credit note {note.CreditNoteNumber}.",
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation(
                "Credit generate completed. TenantId={TenantId} CreditNoteNumber={CreditNoteNumber} InvoiceId={InvoiceId}",
                tenantId,
                note.CreditNoteNumber,
                invoice.Id);
            return (note, null);
        }

        private async Task<List<InvoiceLine>> LoadEligibleLinesAsync(
            int tenantId,
            CreditNotePreviewRequest request,
            CancellationToken cancellationToken)
        {
            var query = dbContext.InvoiceLines
                .Include(x => x.Invoice)
                .Include(x => x.CreditNoteLines)
                    .ThenInclude(x => x.CreditNote)
                .Where(x => x.Invoice.TenantId == tenantId)
                .Where(x => x.Invoice.Status != InvoiceStatuses.Void)
                .Where(x => x.ServicePeriodStart <= request.PeriodEnd && x.ServicePeriodEnd >= request.PeriodStart);

            if (request.InvoiceId.HasValue)
            {
                query = query.Where(x => x.InvoiceId == request.InvoiceId.Value);
            }

            var allowedHomes = await userAccess.GetAllowedCareHomeIdsAsync(cancellationToken);
            if (allowedHomes is not null)
            {
                query = query.Where(x => allowedHomes.Contains(x.Invoice.CareHomeId));
            }

            if (request.ClientId.HasValue)
            {
                query = query.Where(x => x.ClientId == request.ClientId.Value);
            }

            if (request.FundingAuthorityId.HasValue)
            {
                query = query.Where(x => x.Invoice.FundingAuthorityId == request.FundingAuthorityId.Value);
            }

            if (request.InvoiceCategoryId.HasValue)
            {
                query = query.Where(x => x.Invoice.InvoiceCategoryId == request.InvoiceCategoryId.Value);
            }

            return await query.ToListAsync(cancellationToken);
        }

        private static decimal RemainingCreditable(InvoiceLine line)
        {
            var credited = line.CreditNoteLines
                .Where(x => x.CreditNote.Status != CreditNoteStatuses.Void)
                .Sum(x => x.Amount);

            return Money.Round(line.LineAmount + credited);
        }
    }
}

