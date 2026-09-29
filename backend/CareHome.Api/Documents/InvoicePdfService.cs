using System.Globalization;
using CareHome.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CareHome.Api.Documents;

public class InvoicePdfService(IDocumentStore documents, ILogger<InvoicePdfService> logger)
{
    private static readonly CultureInfo UkCulture = CultureInfo.GetCultureInfo("en-GB");

    public async Task<byte[]> GetOrCreateInvoicePdfAsync(
        Invoice invoice,
        Guid tenantPublicId,
        CancellationToken cancellationToken = default)
    {
        var logo = await TryReadFirstLogoAsync(
            [
                invoice.InvoiceTemplate?.CompanyLogoPath,
                invoice.InvoiceTemplate?.AuthorityLogoPath,
                invoice.CareHome?.LogoPath,
                invoice.Company?.LogoPath,
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
        var primaryLine = invoice.Lines.FirstOrDefault();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(42);
                page.MarginVertical(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Element(header => ComposeInvoiceHeader(header, invoice, logoBytes));

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Spacing(18);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => ComposeResidentPanel(c, primaryLine));
                        row.ConstantItem(24);
                        row.RelativeItem().Element(c => ComposeFundingPanel(c, invoice));
                    });

                    col.Item().Element(c => ComposeLineItemsSection(c, invoice));

                    col.Item().AlignRight().Width(260).Element(c => ComposeTotalsPanel(c, invoice));

                    if (HasBankDetails(invoice))
                    {
                        col.Item().Element(c => ComposeBankDetailsPanel(c, invoice));
                    }

                    col.Item().Text(
                            "Line amounts are calculated by billing from eligible days and contracted rates. "
                            + "The invoice service period may differ from an individual line period when charges are pro-rated.")
                        .FontSize(8.5f)
                        .FontColor(Colors.Grey.Darken1)
                        .LineHeight(1.35f);
                });

                page.Footer().PaddingTop(8).Column(col =>
                {
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(6);
                    if (!string.IsNullOrWhiteSpace(invoice.SnapshotFooterText))
                    {
                        col.Item().Element(c => RichTextPdf.Compose(c, invoice.SnapshotFooterText, 8.5f));
                    }

                    var contact = FormatContactLine(invoice);
                    if (!string.IsNullOrWhiteSpace(contact))
                    {
                        col.Item().PaddingTop(4).Text(contact).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                    }

                    col.Item().AlignRight().Text(text =>
                    {
                        text.Span("Page ").FontSize(8);
                        text.CurrentPageNumber().FontSize(8);
                        text.Span(" of ").FontSize(8);
                        text.TotalPages().FontSize(8);
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void ComposeInvoiceHeader(IContainer container, Invoice invoice, byte[]? logoBytes)
    {
        container.Column(col =>
        {
            col.Spacing(14);

            col.Item().Row(row =>
            {
                row.RelativeItem(3).Column(left =>
                {
                    if (logoBytes is { Length: > 0 })
                    {
                        left.Item().MaxHeight(56).MaxWidth(160).Image(logoBytes).FitArea();
                        left.Item().PaddingTop(8);
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.SnapshotTenantName))
                    {
                        left.Item().Text(invoice.SnapshotTenantName).FontSize(10).FontColor(Colors.Grey.Darken1);
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.SnapshotCompanyName))
                    {
                        left.Item().Text(invoice.SnapshotCompanyName).FontSize(15).Bold();
                    }

                    left.Item().PaddingTop(2).Text(invoice.SnapshotCareHomeName).FontSize(11).SemiBold();

                    if (!string.IsNullOrWhiteSpace(invoice.SnapshotHeaderText1))
                    {
                        left.Item().PaddingTop(6).Element(c => RichTextPdf.Compose(c, invoice.SnapshotHeaderText1, 9));
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.SnapshotHeaderText2))
                    {
                        left.Item().Element(c => RichTextPdf.Compose(c, invoice.SnapshotHeaderText2, 9));
                    }
                });

                row.ConstantItem(20);

                row.RelativeItem(2).AlignRight().Column(right =>
                {
                    right.Item().Text("INVOICE").FontSize(11).LetterSpacing(0.08f).FontColor(Colors.Grey.Darken1);
                    right.Item().PaddingTop(4).Text(invoice.InvoiceNumber).FontSize(22).Bold();
                    right.Item().PaddingTop(10).Text(FormatMoney(invoice.TotalAmount))
                        .FontSize(18)
                        .Bold()
                        .FontColor(Colors.Grey.Darken4);

                    right.Item().PaddingTop(14).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(92);
                            columns.RelativeColumn();
                        });

                        void MetaRow(string label, string value)
                        {
                            table.Cell().PaddingVertical(2).Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
                            table.Cell().PaddingVertical(2).AlignRight().Text(value).FontSize(9).SemiBold();
                        }

                        MetaRow("Invoice date", FormatDisplayDate(invoice.InvoiceDate));
                        MetaRow("Due date", FormatDisplayDate(invoice.DueDate));
                        MetaRow("Service period", FormatDateRange(invoice.PeriodStart, invoice.PeriodEnd));
                    });
                });
            });

            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void ComposeResidentPanel(IContainer container, InvoiceLine? line)
    {
        container.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(col =>
        {
            col.Spacing(8);
            col.Item().Text("Resident").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);

            if (line is null)
            {
                col.Item().Text("—").FontColor(Colors.Grey.Darken1);
                return;
            }

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(72);
                    columns.RelativeColumn();
                });

                LabelValueRow(table, "Client", line.SnapshotClientName);
            });
        });
    }

    private static void ComposeFundingPanel(IContainer container, Invoice invoice)
    {
        container.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(col =>
        {
            col.Spacing(8);
            col.Item().Text("Funding & billing").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(108);
                    columns.RelativeColumn();
                });

                LabelValueRow(table, "Funding authority", invoice.SnapshotFundingAuthorityName);
                LabelValueRow(table, "Funding code", NullIfEmpty(invoice.SnapshotFundingAuthorityCode));
                LabelValueRow(table, "Category", invoice.SnapshotInvoiceCategoryName);
                if (!string.IsNullOrWhiteSpace(invoice.SnapshotInvoiceCategoryCode))
                {
                    LabelValueRow(table, "Category code", invoice.SnapshotInvoiceCategoryCode);
                }

                LabelValueRow(table, "Care home", invoice.SnapshotCareHomeName);
                LabelValueRow(table, "Company", NullIfEmpty(invoice.SnapshotCompanyName));
            });
        });
    }

    private static void ComposeLineItemsSection(IContainer container, Invoice invoice)
    {
        container.Column(col =>
        {
            col.Spacing(10);
            col.Item().Text("Invoice lines").FontSize(11).SemiBold();

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.2f);
                    columns.RelativeColumn(3.4f);
                    columns.RelativeColumn(0.6f);
                    columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(1.1f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(TableHeaderCell).Text("Client");
                    header.Cell().Element(TableHeaderCell).Text("Service / description");
                    header.Cell().Element(TableHeaderCell).AlignRight().Text("Days");
                    header.Cell().Element(TableHeaderCell).AlignRight().Text("Rate");
                    header.Cell().Element(TableHeaderCell).AlignRight().Text("Amount");
                });

                foreach (var line in invoice.Lines)
                {
                    table.Cell().Element(TableBodyCell).AlignMiddle().Text(line.SnapshotClientName);
                    table.Cell().Element(TableBodyCell).Column(cell =>
                    {
                        cell.Item().Text(FormatDateRange(line.ServicePeriodStart, line.ServicePeriodEnd))
                            .FontSize(9)
                            .SemiBold();
                        if (!string.IsNullOrWhiteSpace(line.Description))
                        {
                            cell.Item().PaddingTop(3).Text(line.Description).LineHeight(1.35f);
                        }

                        var basis = line.AmountBasis?.Trim();
                        if (!string.IsNullOrWhiteSpace(basis))
                        {
                            cell.Item().PaddingTop(3).Text(basis).FontSize(8.5f).FontColor(Colors.Grey.Darken1).LineHeight(1.3f);
                        }
                    });
                    table.Cell().Element(TableBodyCell).AlignMiddle().AlignRight().Text(line.EligibleDays.ToString());
                    table.Cell().Element(TableBodyCell).AlignMiddle().AlignRight()
                        .Text($"{FormatMoney(line.RateAmount)} {line.RateFrequency}".Trim());
                    table.Cell().Element(TableBodyCell).AlignMiddle().AlignRight().Text(FormatMoney(line.LineAmount));
                }
            });
        });
    }

    private static void ComposeTotalsPanel(IContainer container, Invoice invoice)
    {
        container.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Total").FontSize(12).SemiBold();
                row.ConstantItem(100).AlignRight().Text(FormatMoney(invoice.TotalAmount)).FontSize(14).Bold();
            });
        });
    }

    private static void ComposeBankDetailsPanel(IContainer container, Invoice invoice)
    {
        container.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(col =>
        {
            col.Spacing(6);
            col.Item().Text("Bank details").FontSize(10).SemiBold();
            if (!string.IsNullOrWhiteSpace(invoice.SnapshotBankDetails))
            {
                col.Item().Text(invoice.SnapshotBankDetails).FontSize(9.5f);
            }
            else
            {
                col.Item().Text($"Account name: {invoice.SnapshotBankAccountName}").FontSize(9.5f);
                col.Item().Text($"Sort code: {invoice.SnapshotSortCode}").FontSize(9.5f);
                col.Item().Text($"Account number: {invoice.SnapshotAccountNumber}").FontSize(9.5f);
            }
        });
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
                            header.Cell().Element(TableHeaderCell).Text("Description");
                            header.Cell().Element(TableHeaderCell).AlignRight().Text("Amount");
                        });
                        foreach (var line in creditNote.Lines)
                        {
                            table.Cell().Element(TableBodyCell).Text(line.Description);
                            table.Cell().Element(TableBodyCell).AlignRight().Text(line.Amount.ToString("0.00"));
                        }
                    });
                    col.Item().AlignRight().PaddingTop(12).Text($"Total: {creditNote.TotalAmount:0.00}").Bold();
                });
            });
        }).GeneratePdf();
    }

    private static IContainer TableHeaderCell(IContainer container)
    {
        return container
            .DefaultTextStyle(x => x.SemiBold().FontSize(9).FontColor(Colors.Grey.Darken3))
            .Background(Colors.Grey.Lighten4)
            .PaddingVertical(7)
            .PaddingHorizontal(5)
            .BorderBottom(0.75f)
            .BorderColor(Colors.Grey.Lighten1);
    }

    private static IContainer TableBodyCell(IContainer container)
    {
        return container
            .PaddingVertical(7)
            .PaddingHorizontal(5)
            .BorderBottom(0.25f)
            .BorderColor(Colors.Grey.Lighten2)
            .MinHeight(28);
    }

    private static void LabelValueRow(TableDescriptor table, string label, string? value)
    {
        table.Cell().PaddingVertical(2).Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
        table.Cell().PaddingVertical(2).Text(value ?? "—").FontSize(9.5f);
    }

    private static bool HasBankDetails(Invoice invoice) =>
        !string.IsNullOrWhiteSpace(invoice.SnapshotBankDetails)
        || !string.IsNullOrWhiteSpace(invoice.SnapshotBankAccountName)
        || !string.IsNullOrWhiteSpace(invoice.SnapshotSortCode)
        || !string.IsNullOrWhiteSpace(invoice.SnapshotAccountNumber);

    private static string FormatDisplayDate(DateOnly date) =>
        date.ToString("d MMM yyyy", UkCulture);

    private static string FormatDateRange(DateOnly start, DateOnly end) =>
        $"{FormatDisplayDate(start)} – {FormatDisplayDate(end)}";

    private static string FormatMoney(decimal amount) =>
        amount.ToString("C2", UkCulture);

    private static string NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string? FormatContactLine(Invoice invoice)
    {
        var parts = new[]
        {
            invoice.SnapshotContactName,
            invoice.SnapshotContactJobTitle,
            invoice.SnapshotContactEmail,
            invoice.SnapshotContactPhone
        }.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

        return parts.Length == 0 ? null : string.Join(" · ", parts);
    }
}
