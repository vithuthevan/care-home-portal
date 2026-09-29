using CareHome.Api.Data;
using CareHome.Api.Documents;
using CareHome.Api.Dtos.Collections;
using CareHome.Api.Email;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Services;

public sealed class CollectionReminderService(
    CareHomeDbContext dbContext,
    InvoicePdfService pdfs,
    IEmailSender email,
    UserAccessService userAccess,
    InvoiceReceivableReadModel receivableAmounts,
    TimeProvider timeProvider)
{
    private const int SaveBatchSize = 25;

    public async Task<CollectionReminderRunResult> SendDueRemindersAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var policy = await dbContext.CollectionPolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.IsDefault, cancellationToken);

        if (policy is null || !policy.RemindersEnabled)
        {
            return new CollectionReminderRunResult { RemindersDisabled = true };
        }

        var settings = await dbContext.TenantSettings
            .AsNoTracking()
            .FirstAsync(s => s.TenantId == tenantId, cancellationToken);

        var tenantPublicId = await dbContext.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.PublicId)
            .FirstAsync(cancellationToken);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var homes = await userAccess.GetScopedCareHomeIdsAsync(tenantId);

        var invoices = await dbContext.Invoices
            .Include(x => x.InvoiceTemplate)
            .Include(x => x.CareHome)
            .Include(x => x.Company)
            .Include(x => x.Tenant)
            .Where(x => x.TenantId == tenantId
                && homes.Contains(x.CareHomeId)
                && x.Status != "Void"
                && !string.IsNullOrWhiteSpace(x.RecipientEmail))
            .ToListAsync(cancellationToken);

        if (invoices.Count == 0)
        {
            return new CollectionReminderRunResult();
        }

        var invoiceIds = invoices.Select(x => x.Id).ToList();
        var amountsByInvoice = await receivableAmounts.GetAmountsForInvoicesAsync(
            tenantId,
            invoiceIds,
            cancellationToken);

        var sentRows = await dbContext.CollectionReminderLogs
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Success)
            .Select(x => new { x.InvoiceId, x.ReminderStage })
            .ToListAsync(cancellationToken);
        var sentStages = sentRows
            .GroupBy(x => x.InvoiceId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ReminderStage).ToHashSet());

        var result = new CollectionReminderRunResult();
        var pendingSaves = 0;

        foreach (var invoice in invoices)
        {
            if (!amountsByInvoice.TryGetValue(invoice.Id, out var amounts)
                || amounts.OutstandingAmount <= 0m)
            {
                continue;
            }

            sentStages.TryGetValue(invoice.Id, out var existingStages);
            var stage = CollectionReminderStageResolver.Resolve(
                invoice.DueDate,
                today,
                policy,
                existingStages);
            if (stage is null)
            {
                continue;
            }

            if (existingStages is not null && existingStages.Contains(stage.Value))
            {
                result.Skipped++;
                continue;
            }

            var daysOverdue = Math.Max(0, today.DayNumber - invoice.DueDate.DayNumber);
            var (subject, body, isHtml) = CollectionReminderEmailRenderer.Render(
                policy.ReminderEmailSubjectTemplate,
                policy.ReminderEmailBodyTemplate,
                invoice.InvoiceNumber,
                invoice.DueDate,
                daysOverdue,
                amounts.OutstandingAmount,
                settings.CurrencySymbol);

            byte[]? pdf = null;
            try
            {
                pdf = await pdfs.GetOrCreateInvoicePdfAsync(invoice, tenantPublicId, cancellationToken);
            }
            catch
            {
                pdf = null;
            }

            var sendResult = await email.SendAsync(
                invoice.RecipientEmail!,
                subject,
                body,
                pdf is null ? null : $"invoice-{invoice.InvoiceNumber}.pdf",
                pdf,
                tenantId,
                isHtml,
                cancellationToken);

            dbContext.CollectionReminderLogs.Add(new CollectionReminderLog
            {
                TenantId = tenantId,
                InvoiceId = invoice.Id,
                ReminderStage = stage.Value,
                SentAt = timeProvider.GetUtcNow(),
                Success = sendResult.Success,
                Recipient = invoice.RecipientEmail
            });

            dbContext.EmailSendLogs.Add(new EmailSendLog
            {
                TenantId = tenantId,
                AttemptedAt = timeProvider.GetUtcNow(),
                DocumentType = "CollectionReminder",
                DocumentId = invoice.Id,
                Recipient = invoice.RecipientEmail,
                Success = sendResult.Success,
                Simulated = sendResult.Simulated,
                ErrorMessage = sendResult.ErrorMessage
            });

            pendingSaves++;
            if (pendingSaves >= SaveBatchSize)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                pendingSaves = 0;
            }

            if (sendResult.Success)
            {
                result.Succeeded++;
            }
            else
            {
                result.Failed++;
            }
        }

        if (pendingSaves > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}

public sealed class CollectionReminderRunResult
{
    public int Succeeded { get; set; }

    public int Failed { get; set; }

    public int Skipped { get; set; }

    public bool RemindersDisabled { get; set; }
}
