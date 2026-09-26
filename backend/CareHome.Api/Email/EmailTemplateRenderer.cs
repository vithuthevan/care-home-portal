using CareHome.Api.Models;

namespace CareHome.Api.Email;

public static class EmailTemplateRenderer
{
    public static (string Subject, string Body, bool IsBodyHtml) ForInvoice(
        InvoiceTemplate? template,
        string invoiceNumber)
    {
        return Render(
            template?.EmailSubjectTemplate,
            template?.EmailBodyTemplate,
            $"Invoice {invoiceNumber}",
            $"Please find invoice {invoiceNumber} attached.",
            invoiceNumber,
            creditNoteNumber: null);
    }

    public static (string Subject, string Body, bool IsBodyHtml) ForCreditNote(
        InvoiceTemplate? template,
        string creditNoteNumber)
    {
        return Render(
            template?.EmailSubjectTemplate,
            template?.EmailBodyTemplate,
            $"Credit note {creditNoteNumber}",
            $"Please find credit note {creditNoteNumber} attached.",
            invoiceNumber: null,
            creditNoteNumber);
    }

    private static (string Subject, string Body, bool IsBodyHtml) Render(
        string? subjectTemplate,
        string? bodyTemplate,
        string fallbackSubject,
        string fallbackBody,
        string? invoiceNumber,
        string? creditNoteNumber)
    {
        var subject = string.IsNullOrWhiteSpace(subjectTemplate)
            ? fallbackSubject
            : Apply(subjectTemplate, invoiceNumber, creditNoteNumber);
        var body = string.IsNullOrWhiteSpace(bodyTemplate)
            ? fallbackBody
            : Apply(bodyTemplate, invoiceNumber, creditNoteNumber);
        var isHtml = LooksLikeHtml(body);
        return (subject, body, isHtml);
    }

    private static bool LooksLikeHtml(string text) =>
        text.Contains('<', StringComparison.Ordinal)
        && text.Contains('>', StringComparison.Ordinal);

    private static string Apply(string template, string? invoiceNumber, string? creditNoteNumber)
    {
        return template
            .Replace("{{InvoiceNumber}}", invoiceNumber ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{{CreditNoteNumber}}", creditNoteNumber ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
