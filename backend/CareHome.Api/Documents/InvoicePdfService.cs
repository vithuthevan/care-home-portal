using CareHome.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CareHome.Api.Documents;

public class InvoicePdfService(IDocumentStore documents, ILogger<InvoicePdfService> logger)
{
    public async Task<byte[]> GetOrCreateInvoicePdfAsync(
        Invoice invoice,
        Guid tenantPublicId,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(invoice.PdfPath))
        {
            var existing = await documents.ReadAsync(invoice.PdfPath, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }
        }

        var logo = await TryReadFirstLogoAsync(
            [
                invoice.InvoiceTemplate?.CompanyLogoPath,
                invoice.InvoiceTemplate?.AuthorityLogoPath,
                invoice.CareHome?.LogoPath,
                invoice.Tenant?.LogoPath
            ],
            cancellationToken);
        try
        {
            var bytes = RenderInvoice(invoice, logo);
            var path = await documents.SaveAsync(
                TenantDocumentPaths.Folder(tenantPublicId, "invoices"),
                $"invoice-{Path.GetFileName(invoice.InvoiceNumber)}.pdf",
                bytes,
                cancellationToken);
            invoice.PdfPath = path;
            return bytes;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invoice PDF generation failed. InvoiceId={InvoiceId} InvoiceNumber={InvoiceNumber}", invoice.Id, invoice.InvoiceNumber);
            throw;
        }
    }

    public async Task<byte[]> GetOrCreateCreditNotePdfAsync(
        CreditNote creditNote,
        Guid tenantPublicId,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(creditNote.PdfPath))
        {
            var existing = await documents.ReadAsync(creditNote.PdfPath, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }
        }

        try
        {
            var bytes = RenderCreditNote(creditNote);
            var path = await documents.SaveAsync(
                TenantDocumentPaths.Folder(tenantPublicId, "credit-notes"),
                $"credit-note-{Path.GetFileName(creditNote.CreditNoteNumber)}.pdf",
                bytes,
                cancellationToken);
            creditNote.PdfPath = path;
            return bytes;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Credit note PDF generation failed. CreditNoteId={CreditNoteId} Number={Number}", creditNote.Id, creditNote.CreditNoteNumber);
            throw;
        }
    }

    private async Task<byte[]?> TryReadFirstLogoAsync(
        IEnumerable<string?> relativePaths,
        CancellationToken cancellationToken)
    {
        foreach (var relativePath in relativePaths)
        {
            var bytes = await TryReadLogoAsync(relativePath, cancellationToken);
            if (bytes is { Length: > 0 })
            {
                return bytes;
            }
        }

        return null;
    }

    private async Task<byte[]?> TryReadLogoAsync(string? relativePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        try
        {
            return await documents.ReadAsync(relativePath, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static byte[] RenderInvoice(Invoice invoice, byte[]? logoBytes)
    {

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(32);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Header().Column(col =>
                {
                    if (logoBytes is { Length: > 0 })
                    {
                        col.Item().MaxHeight(52).MaxWidth(140).Image(logoBytes).FitArea();
                    }

                    col.Item().Text(invoice.SnapshotTenantName).FontSize(11);
                    col.Item().Text(invoice.SnapshotCompanyName).FontSize(16).Bold();
                    col.Item().Text(invoice.SnapshotCareHomeName).FontSize(12);
                    if (!string.IsNullOrWhiteSpace(invoice.SnapshotHeaderText1))
                    {
                        col.Item().Text(invoice.SnapshotHeaderText1);
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.SnapshotHeaderText2))
                    {
                        col.Item().Text(invoice.SnapshotHeaderText2);
                    }
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Item().Text($"Invoice {invoice.InvoiceNumber}").FontSize(16).Bold();
                    col.Item().Text($"Invoice date: {invoice.InvoiceDate:yyyy-MM-dd}");
                    col.Item().Text($"Due date: {invoice.DueDate:yyyy-MM-dd}");
                    col.Item().Text($"Service period: {invoice.PeriodStart:yyyy-MM-dd} to {invoice.PeriodEnd:yyyy-MM-dd}");
                    col.Item().Text($"Company: {invoice.SnapshotCompanyName}");
                    col.Item().Text($"Care home: {invoice.SnapshotCareHomeName}");
                    col.Item().Text($"Funding authority: {invoice.SnapshotFundingAuthorityName} ({invoice.SnapshotFundingAuthorityCode})");
                    col.Item().Text($"Category: {invoice.SnapshotInvoiceCategoryName}");
                    col.Item().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.2f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(2.4f);
                            columns.RelativeColumn(0.7f);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(1.0f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Client");
                            header.Cell().Element(HeaderCell).Text("Reference");
                            header.Cell().Element(HeaderCell).Text("Sage ID");
                            header.Cell().Element(HeaderCell).Text("Service / description");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Days");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Rate");
                            header.Cell().Element(HeaderCell).Text("Nominal");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                        });

                        foreach (var line in invoice.Lines)
                        {
                            table.Cell().Element(BodyCell).Text(line.SnapshotClientName);
                            table.Cell().Element(BodyCell).Text(line.SnapshotClientReferenceNumber);
                            table.Cell().Element(BodyCell).Text(line.SnapshotSageId);
                            table.Cell().Element(BodyCell).Column(cell =>
                            {
                                cell.Item().Text($"{line.ServicePeriodStart:yyyy-MM-dd} to {line.ServicePeriodEnd:yyyy-MM-dd}");
                                if (!string.IsNullOrWhiteSpace(line.Description))
                                {
                                    cell.Item().Text(line.Description).FontSize(8).FontColor(Colors.Grey.Darken2);
                                }
                            });
                            table.Cell().Element(BodyCell).AlignRight().Text(line.EligibleDays.ToString());
                            table.Cell().Element(BodyCell).AlignRight().Text($"{line.RateAmount:0.00} {line.RateFrequency}");
                            table.Cell().Element(BodyCell).Text(line.SnapshotNominalCode);
                            table.Cell().Element(BodyCell).AlignRight().Text(line.LineAmount.ToString("0.00"));
                        }
                    });

                    col.Item().AlignRight().PaddingTop(12).Text($"Total: {invoice.TotalAmount:0.00}").FontSize(12).Bold();

                    col.Item().PaddingTop(16).Text("Bank details").Bold();
                    col.Item().Text($"Account: {invoice.SnapshotBankAccountName}");
                    col.Item().Text($"Sort code: {invoice.SnapshotSortCode}");
                    col.Item().Text($"Account number: {invoice.SnapshotAccountNumber}");
                });

                page.Footer().Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(invoice.SnapshotFooterText))
                    {
                        col.Item().Text(invoice.SnapshotFooterText).FontSize(8);
                    }

                    col.Item().Text($"{invoice.SnapshotContactName} {invoice.SnapshotContactEmail} {invoice.SnapshotContactPhone}")
                        .FontSize(8);
                });
            });
        }).GeneratePdf();
    }

    private static byte[] RenderCreditNote(CreditNote creditNote)
    {
        var invoice = creditNote.Invoice;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.Header().Text($"{invoice.SnapshotCompanyName} — Credit Note").FontSize(16).Bold();
                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Item().Text($"Credit note {creditNote.CreditNoteNumber}").FontSize(18).Bold();
                    col.Item().Text($"Against invoice {invoice.InvoiceNumber}");
                    col.Item().Text($"Date: {creditNote.CreditNoteDate:yyyy-MM-dd}");
                    col.Item().Text($"Reason: {creditNote.Reason}");
                    col.Item().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Description");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                        });
                        foreach (var line in creditNote.Lines)
                        {
                            table.Cell().Element(BodyCell).Text(line.Description);
                            table.Cell().Element(BodyCell).AlignRight().Text(line.Amount.ToString("0.00"));
                        }
                    });
                    col.Item().AlignRight().PaddingTop(12).Text($"Total: {creditNote.TotalAmount:0.00}").Bold();
                });
            });
        }).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container.DefaultTextStyle(x => x.SemiBold().FontSize(8)).Padding(3).BorderBottom(1);
    }

    private static IContainer BodyCell(IContainer container)
    {
        return container.PaddingVertical(3).PaddingHorizontal(2).BorderBottom(0.5f);
    }
}
