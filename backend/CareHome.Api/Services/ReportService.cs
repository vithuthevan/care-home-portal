using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Reports;
using CareHome.Api.Models;
using CareHome.Api.Receivables.Contracts;
using CareHome.Api.Receivables.Dtos;
using CareHome.Api.Security;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CareHome.Api.Services;

public class ReportService(
    CareHomeDbContext dbContext,
    UserAccessService userAccess,
    IReceivablesService receivables)
{
    public async Task<List<CensusRowDto>> ClientCensusAsync(
        int tenantId, int? companyId, int? careHomeId, CancellationToken cancellationToken)
    {
        var homes = await AllowedHomes(tenantId, companyId, careHomeId, cancellationToken);
        return await dbContext.Clients.AsNoTracking()
            .Where(x => homes.Contains(x.CareHomeId) && !x.IsArchived)
            .Select(x => new CensusRowDto
            {
                ClientPublicId = x.PublicId,
                CareHomePublicId = x.CareHome.PublicId,
                ClientName = x.FirstName + " " + x.LastName,
                ReferenceNumber = x.ReferenceNumber,
                CareHomeName = x.CareHome.Name,
                Status = x.Status,
                CareType = x.CareType,
                AdmissionDate = x.AdmissionDate
            })
            .OrderBy(x => x.CareHomeName)
            .ThenBy(x => x.ClientName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CurrentRateRowDto>> CurrentRatesAsync(
        int tenantId,
        int? companyId,
        int? careHomeId,
        string? clientStatus,
        int? fundingAuthorityId,
        int? categoryId,
        CancellationToken cancellationToken)
    {
        var homes = await AllowedHomes(tenantId, companyId, careHomeId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var query = dbContext.FundingRates.AsNoTracking()
            .Where(x => homes.Contains(x.ClientFundingContract.Client.CareHomeId))
            .Where(x => x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
            .Where(x => x.ClientFundingContract.Status == "Active");

        if (!string.IsNullOrWhiteSpace(clientStatus))
        {
            query = query.Where(x => x.ClientFundingContract.Client.Status == clientStatus);
        }

        if (fundingAuthorityId.HasValue)
        {
            query = query.Where(x => x.ClientFundingContract.FundingAuthorityId == fundingAuthorityId);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.ClientFundingContract.InvoiceCategoryId == categoryId);
        }

        return await query.Select(x => new CurrentRateRowDto
        {
            ClientPublicId = x.ClientFundingContract.Client.PublicId,
            CareHomePublicId = x.ClientFundingContract.Client.CareHome.PublicId,
            CompanyName = x.ClientFundingContract.Client.CareHome.Company != null
                ? x.ClientFundingContract.Client.CareHome.Company.Name
                : string.Empty,
            CareHomeName = x.ClientFundingContract.Client.CareHome.Name,
            ClientName = x.ClientFundingContract.Client.FirstName + " " + x.ClientFundingContract.Client.LastName,
            ClientStatus = x.ClientFundingContract.Client.Status,
            FundingAuthority = x.ClientFundingContract.FundingAuthority.Name,
            Category = x.ClientFundingContract.InvoiceCategory.Name,
            Frequency = x.Frequency,
            Amount = x.Amount,
            EffectiveFrom = x.EffectiveFrom,
            EffectiveTo = x.EffectiveTo
        }).ToListAsync(cancellationToken);
    }

    public async Task<List<InvoiceReportRowDto>> InvoicesByClientAsync(
        int tenantId, int? clientId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var homes = await AllowedHomes(tenantId, null, null, cancellationToken);
        var query = dbContext.InvoiceLines.AsNoTracking()
            .Where(x => x.Invoice.TenantId == tenantId && homes.Contains(x.Invoice.CareHomeId) && x.Invoice.Status != InvoiceStatuses.Void);

        if (clientId.HasValue)
        {
            query = query.Where(x => x.ClientId == clientId);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.Invoice.InvoiceDate >= from);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.Invoice.InvoiceDate <= to);
        }

        var rows = await query.Select(x => new
        {
            x.Invoice.PublicId,
            x.Invoice.InvoiceNumber,
            x.Invoice.InvoiceDate,
            ClientPublicId = x.Client.PublicId,
            CareHomePublicId = x.Invoice.CareHome.PublicId,
            x.SnapshotClientName,
            x.SnapshotCareHomeName,
            x.SnapshotInvoiceCategoryName,
            x.LineAmount,
            Credits = x.CreditNoteLines
                .Where(c => c.CreditNote.Status != CreditNoteStatuses.Void)
                .Sum(c => c.Amount),
            x.Invoice.PaymentStatus,
            x.Invoice.Status
        }).ToListAsync(cancellationToken);

        return rows.Select(x => new InvoiceReportRowDto
        {
            InvoicePublicId = x.PublicId,
            ClientPublicId = x.ClientPublicId,
            CareHomePublicId = x.CareHomePublicId,
            InvoiceNumber = x.InvoiceNumber,
            InvoiceDate = x.InvoiceDate,
            ClientName = x.SnapshotClientName,
            CareHomeName = x.SnapshotCareHomeName,
            Category = x.SnapshotInvoiceCategoryName,
            Amount = InvoiceLineNetAmount.FromParts(x.LineAmount, x.Credits),
            PaymentStatus = x.PaymentStatus,
            Status = x.Status
        }).ToList();
    }

    public async Task<List<InvoiceReportRowDto>> InvoicesByCareHomeAsync(
        int tenantId, int? careHomeId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var rows = await InvoicesByClientAsync(tenantId, null, from, to, cancellationToken);
        if (!careHomeId.HasValue)
        {
            return rows;
        }

        var name = await dbContext.CareHomes
            .Where(x => x.Id == careHomeId && x.TenantId == tenantId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return rows.Where(r => r.CareHomeName == name).ToList();
    }

    public async Task<List<IncomeByCategoryRowDto>> IncomeByCategoryAsync(
        int tenantId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var homes = await AllowedHomes(tenantId, null, null, cancellationToken);
        var lines = await dbContext.InvoiceLines.AsNoTracking()
            .Where(x => x.Invoice.TenantId == tenantId
                && homes.Contains(x.Invoice.CareHomeId)
                && x.Invoice.Status != InvoiceStatuses.Void
                && x.Invoice.InvoiceDate >= from
                && x.Invoice.InvoiceDate <= to)
            .Select(x => new
            {
                x.SnapshotInvoiceCategoryName,
                Net = x.LineAmount + x.CreditNoteLines
                    .Where(c => c.CreditNote.Status != CreditNoteStatuses.Void)
                    .Sum(c => c.Amount)
            })
            .ToListAsync(cancellationToken);

        return lines
            .GroupBy(x => x.SnapshotInvoiceCategoryName)
            .Select(g => new IncomeByCategoryRowDto
            {
                Category = g.Key,
                Amount = Money.Round(g.Sum(x => x.Net))
            })
            .ToList();
    }

    public async Task<List<OccupancyRowDto>> OccupancyAsync(int tenantId, int? companyId, CancellationToken cancellationToken)
    {
        var homes = await AllowedHomes(tenantId, companyId, null, cancellationToken);
        return await dbContext.CareHomes.AsNoTracking()
            .Where(x => homes.Contains(x.Id))
            .Select(x => new OccupancyRowDto
            {
                CareHomePublicId = x.PublicId,
                CompanyPublicId = x.Company != null ? x.Company.PublicId : null,
                CareHomeName = x.Name,
                CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                Capacity = x.BedCapacity,
                CurrentClients = x.Clients.Count(c => c.Status == "Current" && !c.IsArchived),
                AvailableBeds = x.BedCapacity - x.Clients.Count(c => c.Status == "Current" && !c.IsArchived)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RateHistoryRowDto>> RateHistoryAsync(int tenantId, int? contractId, CancellationToken cancellationToken)
    {
        var homes = await AllowedHomes(tenantId, null, null, cancellationToken);
        var query = dbContext.FundingRates.AsNoTracking()
            .Where(x => homes.Contains(x.ClientFundingContract.Client.CareHomeId));

        if (contractId.HasValue)
        {
            query = query.Where(x => x.ClientFundingContractId == contractId);
        }

        return await query.OrderBy(x => x.EffectiveFrom).Select(x => new RateHistoryRowDto
        {
            ClientPublicId = x.ClientFundingContract.Client.PublicId,
            ClientName = x.ClientFundingContract.Client.FirstName + " " + x.ClientFundingContract.Client.LastName,
            FundingAuthority = x.ClientFundingContract.FundingAuthority.Name,
            EffectiveFrom = x.EffectiveFrom,
            EffectiveTo = x.EffectiveTo,
            Frequency = x.Frequency,
            Amount = x.Amount,
            Notes = x.Notes
        }).ToListAsync(cancellationToken);
    }

    public async Task<List<BillingExceptionRowDto>> BillingExceptionsAsync(int tenantId, CancellationToken cancellationToken)
    {
        var homes = await AllowedHomes(tenantId, null, null, cancellationToken);
        return await dbContext.BillingExceptionLogs.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Where(x => x.CareHomeId == null || homes.Contains(x.CareHomeId.Value))
            .OrderByDescending(x => x.LoggedAt)
            .Take(500)
            .Select(x => new BillingExceptionRowDto
            {
                ClientPublicId = x.Client == null ? null : x.Client.PublicId,
                LoggedAt = x.LoggedAt,
                Severity = x.Severity,
                Code = x.Code,
                Message = x.Message,
                ClientName = x.Client == null ? null : x.Client.FirstName + " " + x.Client.LastName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<OutstandingInvoiceRowDto>> OutstandingAsync(int tenantId, CancellationToken cancellationToken)
    {
        var query = new ReceivableInvoiceQuery { OpenReceivablesOnly = true, Page = 1, PageSize = 10_000 };
        var (items, _) = await receivables.ListInvoicesAsync(tenantId, query, cancellationToken);
        var homeIds = items.Select(x => x.CareHomeId).Distinct().ToList();
        var homePublicIds = await dbContext.CareHomes.AsNoTracking()
            .Where(x => x.TenantId == tenantId && homeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.PublicId, cancellationToken);

        return items
            .OrderBy(x => x.DueDate)
            .Select(x => new OutstandingInvoiceRowDto
            {
                InvoicePublicId = x.PublicId,
                CareHomePublicId = homePublicIds.GetValueOrDefault(x.CareHomeId),
                InvoiceNumber = x.InvoiceNumber,
                InvoiceDate = x.InvoiceDate,
                DueDate = x.DueDate,
                CareHomeName = x.CareHomeName,
                Amount = x.OutstandingAmount,
                PaymentStatus = x.PaymentStatus,
                IsDue = x.DaysOverdue > 0
            })
            .ToList();
    }

    public byte[] ToCsv<T>(string report, IEnumerable<T> rows)
    {
        var props = ExportProperties(typeof(T));
        var lines = new List<string>
        {
            string.Join(",", props.Select(p => CsvFormulaSanitizer.CsvField(ColumnLabel(report, p.Name), neutralizeFormula: false)))
        };
        foreach (var row in rows)
        {
            lines.Add(string.Join(",", props.Select(p => Escape(p.GetValue(row)))));
        }

        return new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(string.Join("\r\n", lines));
    }

    public byte[] ToExcel<T>(string report, IEnumerable<T> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(SheetName(report));
        var props = ExportProperties(typeof(T));
        for (var i = 0; i < props.Length; i++)
        {
            var header = sheet.Cell(1, i + 1);
            header.Value = ColumnLabel(report, props[i].Name);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EEF4");
            header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            if (IsNumeric(props[i].Name))
            {
                header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }
        }

        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < props.Length; c++)
            {
                WriteCell(sheet.Cell(r, c + 1), props[c].GetValue(row));
            }

            r++;
        }

        if (props.Length > 0)
        {
            var lastRow = Math.Max(r - 1, 1);
            var range = sheet.Range(1, 1, lastRow, props.Length);
            if (lastRow > 1)
            {
                var table = range.CreateTable("ReportTable");
                table.Theme = XLTableTheme.TableStyleMedium2;
                table.ShowAutoFilter = true;
            }
            else
            {
                range.SetAutoFilter();
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Columns(1, props.Length).AdjustToContents(1, Math.Min(lastRow, 40));
            sheet.Rows(1, lastRow).AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ToPdf<T>(string report, IEnumerable<T> rows)
    {
        var data = rows as IList<T> ?? rows.ToList();
        var props = ExportProperties(typeof(T));
        var title = ReportTitle(report);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(28);
                page.Size(props.Length > 6 ? PageSizes.A4.Landscape() : PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(props.Length > 8 ? 8 : 9));
                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(16).Bold();
                    col.Item().PaddingTop(2).Text($"{data.Count} row(s)").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
                page.Content().PaddingTop(12).Element(body =>
                {
                    if (props.Length == 0)
                    {
                        body.Text("Nothing to export.");
                        return;
                    }

                    body.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var prop in props)
                            {
                                if (IsNumeric(prop.Name))
                                {
                                    columns.ConstantColumn(72);
                                }
                                else
                                {
                                    columns.RelativeColumn();
                                }
                            }
                        });

                        table.Header(header =>
                        {
                            foreach (var prop in props)
                            {
                                IContainer cell = header.Cell()
                                    .Background(Colors.Grey.Lighten3)
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Grey.Medium)
                                    .Padding(4);
                                if (IsNumeric(prop.Name))
                                {
                                    cell = cell.AlignRight();
                                }

                                cell.Text(ColumnLabel(report, prop.Name)).SemiBold();
                            }
                        });

                        if (data.Count == 0)
                        {
                            table.Cell().ColumnSpan((uint)props.Length).Padding(8).Text("No rows.");
                            return;
                        }

                        for (var i = 0; i < data.Count; i++)
                        {
                            var shade = i % 2 == 1 ? Colors.Grey.Lighten4 : Colors.White;
                            foreach (var prop in props)
                            {
                                IContainer cell = table.Cell()
                                    .Background(shade)
                                    .BorderBottom(0.5f)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(3);
                                if (IsNumeric(prop.Name))
                                {
                                    cell = cell.AlignRight();
                                }

                                cell.Text(FormatExportValue(prop.GetValue(data[i])));
                            }
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    private async Task<List<int>> AllowedHomes(int tenantId, int? companyId, int? careHomeId, CancellationToken cancellationToken)
    {
        var allowed = await userAccess.GetAllowedCareHomeIdsAsync(cancellationToken);
        var query = dbContext.CareHomes.AsNoTracking().Where(x => x.TenantId == tenantId);
        if (allowed is not null)
        {
            query = query.Where(x => allowed.Contains(x.Id));
        }

        if (companyId.HasValue)
        {
            query = query.Where(x => x.CompanyId == companyId);
        }

        if (careHomeId.HasValue)
        {
            query = query.Where(x => x.Id == careHomeId);
        }

        return await query.Select(x => x.Id).ToListAsync(cancellationToken);
    }

    private static string Escape(object? value)
    {
        var text = FormatExportValue(value);
        if (value is not decimal and not DateOnly and not DateTimeOffset and not DateTime and not bool and not int and not long and not double)
        {
            text = CsvFormulaSanitizer.Neutralize(text);
        }

        return CsvFormulaSanitizer.CsvField(text, neutralizeFormula: false);
    }

    private static System.Reflection.PropertyInfo[] ExportProperties(Type type) =>
        type.GetProperties()
            .Where(p => p.CanRead && !p.Name.EndsWith("PublicId", StringComparison.Ordinal))
            .ToArray();

    private static void WriteCell(IXLCell cell, object? raw)
    {
        switch (raw)
        {
            case null:
                cell.Value = string.Empty;
                return;
            case decimal dec:
                cell.Value = (double)dec;
                cell.Style.NumberFormat.Format = "#,##0.00";
                return;
            case int number:
                cell.Value = number;
                return;
            case long number:
                cell.Value = (double)number;
                cell.Style.NumberFormat.Format = "#,##0";
                return;
            case double number:
                cell.Value = number;
                return;
            case bool flag:
                cell.Value = flag ? "Yes" : "No";
                return;
            case DateOnly date:
                cell.Value = date.ToDateTime(TimeOnly.MinValue);
                cell.Style.DateFormat.Format = "yyyy-mm-dd";
                return;
            case DateTime date:
                cell.Value = date;
                cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                return;
            case DateTimeOffset date:
                cell.Value = date.UtcDateTime;
                cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                return;
            default:
                cell.Value = CsvFormulaSanitizer.Neutralize(raw.ToString() ?? "");
                return;
        }
    }

    private static string FormatExportValue(object? value) => value switch
    {
        null => "",
        DateOnly date => date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        DateTime date => date.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture),
        DateTimeOffset date => date.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture),
        bool flag => flag ? "Yes" : "No",
        decimal amount => amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
        double amount => amount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString() ?? ""
    };

    private static bool IsNumeric(string property) =>
        property is "Amount" or "TotalAmount" or "Capacity" or "CurrentClients" or "AvailableBeds";

    private static string ColumnLabel(string report, string property)
    {
        if (property == "Amount" && report is "invoices-by-client" or "invoices-by-care-home" or "income-by-category")
        {
            return "Net billed";
        }

        if (property == "PaymentStatus")
        {
            return "Payment status";
        }

        return ColumnLabels.TryGetValue(property, out var label) ? label : SplitWords(property);
    }

    private static string ReportTitle(string report) => report switch
    {
        "client-census" => "Resident census",
        "current-rates" => "Current rates",
        "invoices-by-client" => "Invoices by resident",
        "invoices-by-care-home" => "Invoices by care home",
        "income-by-category" => "Income by category",
        "occupancy" => "Occupancy / availability",
        "rate-history" => "Funding rate history",
        "billing-exceptions" => "Billing exceptions",
        "outstanding" => "Payment status / outstanding",
        _ => SplitWords(report.Replace('-', ' '))
    };

    private static string SheetName(string report)
    {
        var cleaned = new string(report.Select(ch => ":\\/?*[]".Contains(ch) ? '-' : ch).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = "Report";
        }

        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }

    private static string SplitWords(string name)
    {
        var chars = new List<char>(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
            {
                chars.Add(' ');
            }

            chars.Add(i == 0 ? char.ToUpperInvariant(name[i]) : name[i]);
        }

        return new string(chars.ToArray());
    }

    private static readonly Dictionary<string, string> ColumnLabels = new(StringComparer.Ordinal)
    {
        ["ClientName"] = "Resident",
        ["ReferenceNumber"] = "Reference",
        ["CareHomeName"] = "Care Home",
        ["CompanyName"] = "Company",
        ["Status"] = "Status",
        ["CareType"] = "Care Type",
        ["AdmissionDate"] = "Admission Date",
        ["ClientStatus"] = "Resident Status",
        ["FundingAuthority"] = "Funding Authority",
        ["Category"] = "Category",
        ["Frequency"] = "Frequency",
        ["Amount"] = "Amount",
        ["EffectiveFrom"] = "Effective From",
        ["EffectiveTo"] = "Effective To",
        ["InvoiceNumber"] = "Invoice",
        ["InvoiceDate"] = "Invoice Date",
        ["TotalAmount"] = "Amount",
        ["Capacity"] = "Capacity",
        ["CurrentClients"] = "Current Residents",
        ["AvailableBeds"] = "Available Beds",
        ["Notes"] = "Notes",
        ["LoggedAt"] = "Logged At",
        ["Severity"] = "Severity",
        ["Code"] = "Code",
        ["Message"] = "Message",
        ["DueDate"] = "Due Date",
        ["IsDue"] = "Overdue",
    };
}

