using System.Globalization;

namespace CareHome.Api.Email;

public static class CollectionReminderEmailRenderer
{
    private static readonly CultureInfo UkCulture = CultureInfo.GetCultureInfo("en-GB");

    public static (string Subject, string Body, bool IsBodyHtml) Render(
        string? subjectTemplate,
        string? bodyTemplate,
        string invoiceNumber,
        DateOnly dueDate,
        int daysOverdue,
        decimal outstandingAmount,
        string currencySymbol)
    {
        var subject = string.IsNullOrWhiteSpace(subjectTemplate)
            ? $"Payment reminder: invoice {invoiceNumber}"
            : Apply(subjectTemplate, invoiceNumber, dueDate, daysOverdue, outstandingAmount, currencySymbol);
        var body = string.IsNullOrWhiteSpace(bodyTemplate)
            ? $"This is a reminder that invoice {invoiceNumber} was due on {dueDate:dd MMM yyyy}. Outstanding amount: {currencySymbol}{outstandingAmount:N2}."
            : Apply(bodyTemplate, invoiceNumber, dueDate, daysOverdue, outstandingAmount, currencySymbol);
        var isHtml = body.Contains('<', StringComparison.Ordinal) && body.Contains('>', StringComparison.Ordinal);
        return (subject, body, isHtml);
    }

    private static string Apply(
        string template,
        string invoiceNumber,
        DateOnly dueDate,
        int daysOverdue,
        decimal outstandingAmount,
        string currencySymbol)
    {
        return template
            .Replace("{{InvoiceNumber}}", invoiceNumber, StringComparison.OrdinalIgnoreCase)
            .Replace("{{DueDate}}", dueDate.ToString("dd MMM yyyy", UkCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{{DaysOverdue}}", daysOverdue.ToString(UkCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{{OutstandingAmount}}", $"{currencySymbol}{outstandingAmount:N2}", StringComparison.OrdinalIgnoreCase);
    }
}
