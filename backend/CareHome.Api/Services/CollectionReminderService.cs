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
    TimeProvider timeProvider)
{
    public async Task<CollectionReminderRunResult> SendDueRemindersAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var policy = await dbContext.CollectionPolicies
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.IsDefault, cancellationToken);

        if (policy is null || !policy.RemindersEnabled)
        {
            return new CollectionReminderRunResult();
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
                && x.PaymentStatus != "Paid"
                && !string.IsNullOrWhiteSpace(x.RecipientEmail))
            .ToListAsync(cancellationToken);

        var sentRows = await dbContext.CollectionReminderLogs
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Success)
            .Select(x => new { x.InvoiceId, x.ReminderStage })
            .ToListAsync(cancellationToken);
        var sentStages = sentRows
            .GroupBy(x => x.InvoiceId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ReminderStage).ToHashSet());

        var result = new CollectionReminderRunResult();

        foreach (var invoice in invoices)
        {
            var stage = ResolveStage(invoice, today, policy);
            if (stage is null)
            {
                continue;
            }

            if (sentStages.TryGetValue(invoice.Id, out var existing) && existing.Contains(stage.Value))
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
                invoice.TotalAmount,
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

            await dbContext.SaveChangesAsync(cancellationToken);

            if (sendResult.Success)
            {
                result.Succeeded++;
            }
            else
            {
                result.Failed++;
            }
        }

        return result;
    }

    private static int? ResolveStage(Invoice invoice, DateOnly today, CollectionPolicy policy)
    {
        if (policy.DueReminderDaysBefore > 0)
        {
            var daysUntilDue = invoice.DueDate.DayNumber - today.DayNumber;
            if (daysUntilDue == policy.DueReminderDaysBefore && invoice.PaymentStatus != "Paid")
            {
                return 0;
            }
        }

        if (today <= invoice.DueDate)
        {
            return null;
        }

        var daysOverdue = today.DayNumber - invoice.DueDate.DayNumber;
        if (daysOverdue >= policy.Overdue30Days)
        {
            return policy.Overdue30Days;
        }

        if (daysOverdue >= policy.Overdue14Days)
        {
            return policy.Overdue14Days;
        }

        if (daysOverdue >= policy.Overdue7Days)
        {
            return policy.Overdue7Days;
        }

        return null;
    }
}

public sealed class CollectionReminderRunResult
{
    public int Succeeded { get; set; }

    public int Failed { get; set; }

    public int Skipped { get; set; }
}
