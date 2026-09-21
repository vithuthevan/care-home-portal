using CareHome.Api.Data;
using CareHome.Api.Dtos.Reports;
using CareHome.Api.Receivables.Contracts;
using CareHome.Api.Receivables.Dtos;
using CareHome.Api.Security;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CareHome.Api.Services
{
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
                CompanyName = x.ClientFundingContract.Client.CareHome.Company.Name,
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
                .Where(x => x.Invoice.TenantId == tenantId && homes.Contains(x.Invoice.CareHomeId) && x.Invoice.Status != "Void");

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

            return await query.Select(x => new InvoiceReportRowDto
            {
                InvoiceNumber = x.Invoice.InvoiceNumber,
                InvoiceDate = x.Invoice.InvoiceDate,
                ClientName = x.SnapshotClientName,
                CareHomeName = x.SnapshotCareHomeName,
                Category = x.SnapshotInvoiceCategoryName,
                Amount = x.LineAmount,
                PaymentStatus = x.Invoice.PaymentStatus,
                Status = x.Invoice.Status
            }).ToListAsync(cancellationToken);
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
            return await dbContext.InvoiceLines.AsNoTracking()
                .Where(x => x.Invoice.TenantId == tenantId
                    && homes.Contains(x.Invoice.CareHomeId)
                    && x.Invoice.Status != "Void"
                    && x.Invoice.InvoiceDate >= from
                    && x.Invoice.InvoiceDate <= to)
                .GroupBy(x => x.SnapshotInvoiceCategoryName)
                .Select(g => new IncomeByCategoryRowDto
                {
                    Category = g.Key,
                    Amount = g.Sum(x => x.LineAmount)
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<OccupancyRowDto>> OccupancyAsync(int tenantId, int? companyId, CancellationToken cancellationToken)
        {
            var homes = await AllowedHomes(tenantId, companyId, null, cancellationToken);
            return await dbContext.CareHomes.AsNoTracking()
                .Where(x => homes.Contains(x.Id))
                .Select(x => new OccupancyRowDto
                {
                    CareHomeName = x.Name,
                    CompanyName = x.Company.Name,
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
            return items
                .OrderBy(x => x.DueDate)
                .Select(x => new OutstandingInvoiceRowDto
                {
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

        public byte[] ToCsv<T>(IEnumerable<T> rows)
        {
            var props = typeof(T).GetProperties();
            var lines = new List<string> { string.Join(",", props.Select(p => p.Name)) };
            foreach (var row in rows)
            {
                lines.Add(string.Join(",", props.Select(p => Escape(p.GetValue(row)))));
            }

            return System.Text.Encoding.UTF8.GetBytes(string.Join("\n", lines));
        }

        public byte[] ToExcel<T>(string sheetName, IEnumerable<T> rows)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.AddWorksheet(sheetName);
            var props = typeof(T).GetProperties();
            for (var i = 0; i < props.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = props[i].Name;
            }

            var r = 2;
            foreach (var row in rows)
            {
                for (var c = 0; c < props.Length; c++)
                {
                    var raw = props[c].GetValue(row);
                    var text = raw?.ToString() ?? "";
                    if (raw is not decimal and not DateOnly and not DateTimeOffset and not DateTime and not bool and not int and not long)
                    {
                        text = CsvFormulaSanitizer.Neutralize(text);
                    }

                    sheet.Cell(r, c + 1).Value = text;
                }

                r++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ToPdf(string title, IEnumerable<string> lines)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.Header().Text(title).FontSize(16).Bold();
                    page.Content().Column(col =>
                    {
                        foreach (var line in lines)
                        {
                            col.Item().Text(line).FontSize(10);
                        }
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
            var text = value?.ToString() ?? "";
            if (value is not decimal and not DateOnly and not DateTimeOffset and not DateTime and not bool and not int and not long)
            {
                text = CsvFormulaSanitizer.Neutralize(text);
            }

            return CsvFormulaSanitizer.CsvField(text, neutralizeFormula: false);
        }
    }
}

