using CareHome.Api.Audit;
using CareHome.Api.Data;
using CareHome.Api.Documents;
using CareHome.Api.Email;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Services;

public sealed class DocumentEmailService(
    CareHomeDbContext dbContext,
    InvoicePdfService pdfs,
    IEmailSender email,
    UserAccessService userAccess,
    AuditService audit)
{
    public async Task<InvoiceEmailSendResult> SendInvoiceAsync(
        int tenantId,
        int invoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices
            .Include(x => x.Lines)
            .Include(x => x.InvoiceTemplate)
            .Include(x => x.CareHome)
            .Include(x => x.Company)
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.TenantId == tenantId, cancellationToken);

        if (invoice is null || !await userAccess.CanAccessCareHomeAsync(tenantId, invoice.CareHomeId))
        {
            return InvoiceEmailSendResult.NotFound();
        }

        if (invoice.Status == "Void")
        {
            return InvoiceEmailSendResult.Skipped(
                invoice.InvoiceNumber,
                "A void invoice cannot be emailed.");
        }

        if (string.IsNullOrWhiteSpace(invoice.RecipientEmail))
        {
            return InvoiceEmailSendResult.Skipped(
                invoice.InvoiceNumber,
                "This invoice has no recipient email.");
        }

        var tenantPublicId = await TenantPublicIdAsync(tenantId, cancellationToken);
        var pdf = await pdfs.GetOrCreateInvoicePdfAsync(invoice, tenantPublicId, cancellationToken);
        var (subject, body, isBodyHtml) = EmailTemplateRenderer.ForInvoice(
            invoice.InvoiceTemplate,
            invoice.InvoiceNumber);
        var result = await email.SendAsync(
            invoice.RecipientEmail,
            subject,
            body,
            $"invoice-{invoice.InvoiceNumber}.pdf",
            pdf,
            tenantId,
            isBodyHtml,
            cancellationToken);

        var sentAt = DateTimeOffset.UtcNow;
        var statusUpdated = false;
        string? statusNote = null;

        if (result.Success)
        {
            var updated = await dbContext.Invoices
                .Where(x => x.Id == invoice.Id
                    && x.TenantId == tenantId
                    && x.Status != "Void")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, "Sent")
                    .SetProperty(x => x.SentAt, sentAt)
                    .SetProperty(x => x.PdfPath, invoice.PdfPath), cancellationToken);
            statusUpdated = updated > 0;
            if (!statusUpdated)
            {
                statusNote = "Email was sent but invoice status was not updated because the invoice is now void.";
            }
        }

        dbContext.EmailSendLogs.Add(new EmailSendLog
        {
            TenantId = tenantId,
            AttemptedAt = DateTimeOffset.UtcNow,
            DocumentType = "Invoice",
            DocumentId = invoice.Id,
            Recipient = invoice.RecipientEmail,
            Success = result.Success,
            Simulated = result.Simulated,
            ErrorMessage = result.ErrorMessage ?? statusNote
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        await audit.LogAsync(
            "Invoice",
            invoice.Id.ToString(),
            "Send",
            null,
            new { invoice.InvoiceNumber, result.Success, result.Simulated, statusUpdated },
            result.Success
                ? (statusUpdated
                    ? $"Sent invoice {invoice.InvoiceNumber}."
                    : $"Sent invoice {invoice.InvoiceNumber} but status was not updated (voided concurrently).")
                : $"Failed to send invoice {invoice.InvoiceNumber}.");

        if (!result.Success)
        {
            return InvoiceEmailSendResult.Failed(invoice.InvoiceNumber, result.ErrorMessage ?? "Email failed.");
        }

        return InvoiceEmailSendResult.Succeeded(
            invoice.InvoiceNumber,
            result.Simulated,
            statusUpdated ? sentAt : null,
            statusNote);
    }

    public async Task<CreditNoteEmailSendResult> SendCreditNoteAsync(
        int tenantId,
        int creditNoteId,
        CancellationToken cancellationToken = default)
    {
        var note = await dbContext.CreditNotes
            .Include(x => x.Invoice)
            .ThenInclude(x => x!.InvoiceTemplate)
            .Include(x => x.Invoice)
            .ThenInclude(x => x!.CareHome)
            .FirstOrDefaultAsync(x => x.Id == creditNoteId && x.TenantId == tenantId, cancellationToken);

        if (note?.Invoice is null
            || !await userAccess.CanAccessCareHomeAsync(tenantId, note.Invoice.CareHomeId))
        {
            return CreditNoteEmailSendResult.NotFound();
        }

        if (string.IsNullOrWhiteSpace(note.RecipientEmail))
        {
            return CreditNoteEmailSendResult.Failed(
                note.CreditNoteNumber,
                "This credit note has no recipient email.");
        }

        var tenantPublicId = await TenantPublicIdAsync(tenantId, cancellationToken);
        var pdf = await pdfs.GetOrCreateCreditNotePdfAsync(note, tenantPublicId, cancellationToken);
        var (subject, body, isBodyHtml) = EmailTemplateRenderer.ForCreditNote(
            note.Invoice.InvoiceTemplate,
            note.CreditNoteNumber);
        var result = await email.SendAsync(
            note.RecipientEmail,
            subject,
            body,
            $"credit-note-{note.CreditNoteNumber}.pdf",
            pdf,
            tenantId,
            isBodyHtml,
            cancellationToken);

        if (result.Success)
        {
            note.SentAt = DateTimeOffset.UtcNow;
        }

        dbContext.EmailSendLogs.Add(new EmailSendLog
        {
            TenantId = tenantId,
            AttemptedAt = DateTimeOffset.UtcNow,
            DocumentType = "CreditNote",
            DocumentId = note.Id,
            Recipient = note.RecipientEmail,
            Success = result.Success,
            Simulated = result.Simulated,
            ErrorMessage = result.ErrorMessage
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        await audit.LogAsync(
            "CreditNote",
            note.Id.ToString(),
            "Send",
            null,
            new { note.CreditNoteNumber, result.Success, result.Simulated },
            result.Success
                ? $"Sent credit note {note.CreditNoteNumber}."
                : $"Failed to send credit note {note.CreditNoteNumber}.");

        if (!result.Success)
        {
            return CreditNoteEmailSendResult.Failed(
                note.CreditNoteNumber,
                result.ErrorMessage ?? "Email failed.");
        }

        return CreditNoteEmailSendResult.Succeeded(note.CreditNoteNumber, result.Simulated);
    }

    public async Task<BulkInvoiceEmailSummary> SendInvoicesAsync(
        int tenantId,
        IEnumerable<int> invoiceIds,
        CancellationToken cancellationToken = default)
    {
        var summary = new BulkInvoiceEmailSummary();
        foreach (var id in invoiceIds.Distinct())
        {
            var outcome = await SendInvoiceAsync(tenantId, id, cancellationToken);
            var item = new BulkInvoiceEmailItem
            {
                InvoiceId = id,
                InvoiceNumber = outcome.InvoiceNumber ?? string.Empty,
                Outcome = outcome.BulkOutcome,
                Reason = outcome.Reason
            };
            summary.Items.Add(item);
            switch (outcome.BulkOutcome)
            {
                case "Succeeded":
                case "Simulated":
                    summary.Succeeded++;
                    break;
                case "Failed":
                    summary.Failed++;
                    break;
                default:
                    summary.Skipped++;
                    break;
            }
        }

        return summary;
    }

    private async Task<Guid> TenantPublicIdAsync(int tenantId, CancellationToken cancellationToken)
    {
        return await dbContext.Tenants
            .Where(x => x.Id == tenantId)
            .Select(x => x.PublicId)
            .FirstAsync(cancellationToken);
    }
}

public sealed class InvoiceEmailSendResult
{
    public string? InvoiceNumber { get; init; }

    public string BulkOutcome { get; init; } = "Skipped";

    public string? Reason { get; init; }

    public bool Simulated { get; init; }

    public DateTimeOffset? SentAt { get; init; }

    public static InvoiceEmailSendResult NotFound() => new()
    {
        BulkOutcome = "Skipped",
        Reason = "Not found."
    };

    public static InvoiceEmailSendResult Skipped(string invoiceNumber, string message) => new()
    {
        InvoiceNumber = invoiceNumber,
        BulkOutcome = "Skipped",
        Reason = message
    };

    public static InvoiceEmailSendResult Failed(string invoiceNumber, string message) => new()
    {
        InvoiceNumber = invoiceNumber,
        BulkOutcome = "Failed",
        Reason = message
    };

    public static InvoiceEmailSendResult Succeeded(
        string invoiceNumber,
        bool simulated,
        DateTimeOffset? sentAt,
        string? warning) => new()
    {
        InvoiceNumber = invoiceNumber,
        BulkOutcome = simulated ? "Simulated" : "Succeeded",
        Reason = warning,
        Simulated = simulated,
        SentAt = sentAt
    };
}

public sealed class CreditNoteEmailSendResult
{
    public string? CreditNoteNumber { get; init; }

    public bool IsNotFound { get; init; }

    public bool Success { get; init; }

    public bool Simulated { get; init; }

    public string? ErrorMessage { get; init; }

    public static CreditNoteEmailSendResult NotFound() => new() { IsNotFound = true };

    public static CreditNoteEmailSendResult Failed(string number, string message) => new()
    {
        CreditNoteNumber = number,
        Success = false,
        ErrorMessage = message
    };

    public static CreditNoteEmailSendResult Succeeded(string number, bool simulated) => new()
    {
        CreditNoteNumber = number,
        Success = true,
        Simulated = simulated
    };
}

public sealed class BulkInvoiceEmailSummary
{
    public int Succeeded { get; set; }

    public int Failed { get; set; }

    public int Skipped { get; set; }

    public List<BulkInvoiceEmailItem> Items { get; set; } = [];
}

public sealed class BulkInvoiceEmailItem
{
    public int InvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string Outcome { get; set; } = string.Empty;

    public string? Reason { get; set; }
}
