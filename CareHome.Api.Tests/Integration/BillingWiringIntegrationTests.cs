using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Billing;
using CareHome.Api.Models;
using CareHome.Api.Dtos.CreditNotes;
using CareHome.Api.Dtos.Dashboard;
using CareHome.Api.Dtos.Invoices;
using CareHome.Api.Dtos.Reports;
using CareHome.Api.Dtos.Sage;
using CareHome.Api.Export;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHome.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class BillingWiringIntegrationTests(ApiIntegrationFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [SqlIntegrationFact]
    public async Task Pinned_contract_template_is_used_when_resolver_would_pick_another()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Template Pin");

        int pinnedTemplateId;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
            var scopedWinner = new InvoiceTemplate
            {
                TenantId = scenario.Tenant.Id,
                Name = "Scoped resolver winner",
                InvoiceCategoryId = scenario.Category.Id,
                FundingAuthorityId = scenario.FundingAuthority.Id,
                CareHomeId = scenario.CareHome.Id,
                HeaderText1 = "Scoped",
                ContactEmail = "finance@test.local",
                EmailSubjectTemplate = "Invoice {{InvoiceNumber}}",
                EmailBodyTemplate = "Attached",
                IsActive = true
            };
            var pinned = new InvoiceTemplate
            {
                TenantId = scenario.Tenant.Id,
                Name = "Explicitly pinned",
                InvoiceCategoryId = scenario.Category.Id,
                HeaderText1 = "Pinned",
                ContactEmail = "finance@test.local",
                EmailSubjectTemplate = "Invoice {{InvoiceNumber}}",
                EmailBodyTemplate = "Attached",
                IsActive = true
            };
            db.InvoiceTemplates.AddRange(scopedWinner, pinned);
            await db.SaveChangesAsync();
            pinnedTemplateId = pinned.Id;

            var contract = await db.ClientFundingContracts
                .FirstAsync(x => x.ClientId == scenario.Client.Id);
            contract.InvoiceTemplateId = pinnedTemplateId;
            await db.SaveChangesAsync();
        }

        var invoiceId = await GenerateInvoiceAsync(scenario);

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
            var invoice = await db.Invoices.AsNoTracking().FirstAsync(x => x.Id == invoiceId);
            Assert.Equal(pinnedTemplateId, invoice.InvoiceTemplateId);
            Assert.Equal("Explicitly pinned", invoice.SnapshotTemplateName);
        }
    }

    [SqlIntegrationFact]
    public async Task Credit_note_preview_with_invoiceId_only_includes_that_invoice()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "CN Invoice Scope");
        var invoiceId = await GenerateInvoiceAsync(scenario);

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"cn-scope-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var wrongInvoice = invoiceId + 99_999;
        var wrongPreview = await client.PostAsJsonAsync("/api/credit-notes/preview", new CreditNotePreviewRequest
        {
            InvoiceId = wrongInvoice,
            PeriodStart = scenario.PeriodStart,
            PeriodEnd = scenario.PeriodEnd,
            CreditNoteDate = scenario.PeriodEnd,
            Reason = "Scope test"
        });
        wrongPreview.EnsureSuccessStatusCode();
        var wrongBody = await wrongPreview.Content.ReadFromJsonAsync<CreditNotePreviewResponse>(JsonOptions);
        Assert.NotNull(wrongBody);
        Assert.Empty(wrongBody!.Lines);

        var scopedPreview = await client.PostAsJsonAsync("/api/credit-notes/preview", new CreditNotePreviewRequest
        {
            InvoiceId = invoiceId,
            PeriodStart = scenario.PeriodStart,
            PeriodEnd = scenario.PeriodEnd,
            CreditNoteDate = scenario.PeriodEnd,
            Reason = "Scope test"
        });
        scopedPreview.EnsureSuccessStatusCode();
        var scopedBody = await scopedPreview.Content.ReadFromJsonAsync<CreditNotePreviewResponse>(JsonOptions);
        Assert.NotNull(scopedBody);
        Assert.NotEmpty(scopedBody!.Lines);
        Assert.All(scopedBody.Lines, line => Assert.Equal(
            scopedBody.Lines[0].InvoiceNumber,
            line.InvoiceNumber));
    }

    [SqlIntegrationFact]
    public async Task Net_billed_aligns_across_reports_sage_preview_dashboard_and_invoice_detail()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Net Parity");
        var invoiceId = await GenerateInvoiceAsync(scenario);

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"net-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var preview = await client.PostAsJsonAsync("/api/credit-notes/preview", new CreditNotePreviewRequest
        {
            InvoiceId = invoiceId,
            PeriodStart = scenario.PeriodStart,
            PeriodEnd = scenario.PeriodEnd,
            CreditNoteDate = scenario.PeriodEnd,
            Reason = "Net parity"
        });
        preview.EnsureSuccessStatusCode();
        var previewBody = await preview.Content.ReadFromJsonAsync<CreditNotePreviewResponse>(JsonOptions);
        Assert.NotNull(previewBody);
        var firstLine = previewBody!.Lines.First();
        var creditAmount = firstLine.CreditAmount / 2m;

        var generate = await client.PostAsJsonAsync("/api/credit-notes/generate", new CreditNotePreviewRequest
        {
            InvoiceId = invoiceId,
            PeriodStart = scenario.PeriodStart,
            PeriodEnd = scenario.PeriodEnd,
            CreditNoteDate = scenario.PeriodEnd,
            Reason = "Net parity",
            LineAmounts = new Dictionary<int, decimal> { [firstLine.InvoiceLineId] = creditAmount }
        });
        generate.EnsureSuccessStatusCode();

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
            var line = await db.InvoiceLines
                .Include(l => l.CreditNoteLines)
                .ThenInclude(c => c.CreditNote)
                .FirstAsync(l => l.Id == firstLine.InvoiceLineId);
            var expectedNet = InvoiceLineNetAmount.FromLine(line);
            var invoiceLines = await db.InvoiceLines
                .Include(l => l.CreditNoteLines)
                .ThenInclude(c => c.CreditNote)
                .Where(l => l.InvoiceId == invoiceId)
                .ToListAsync();
            var invoiceNet = Money.Round(invoiceLines.Sum(InvoiceLineNetAmount.FromLine));

            var from = scenario.PeriodStart.ToString("yyyy-MM-dd");
            var to = scenario.PeriodEnd.ToString("yyyy-MM-dd");
            var reportRows = await client.GetFromJsonAsync<List<InvoiceReportRowDto>>(
                $"/api/reports/invoices-by-client?from={from}&to={to}",
                JsonOptions);
            Assert.NotNull(reportRows);
            var reportAmount = reportRows!.Single(r => r.InvoiceNumber == firstLine.InvoiceNumber).Amount;
            Assert.Equal(expectedNet, reportAmount);

            var sagePreview = await client.PostAsJsonAsync("/api/sage-exports/preview", new SageExportRequest
            {
                DateFrom = scenario.PeriodStart,
                DateTo = scenario.PeriodEnd
            });
            sagePreview.EnsureSuccessStatusCode();
            var sageBody = await sagePreview.Content.ReadFromJsonAsync<SageExportPreviewResponse>(JsonOptions);
            Assert.NotNull(sageBody);
            var sageRow = sageBody!.Rows.Single(r => r.Eligible);
            Assert.Equal(expectedNet, sageRow.Amount);

            var dashboard = await client.GetFromJsonAsync<DashboardDto>("/api/dashboard", JsonOptions);
            Assert.NotNull(dashboard);
            var recent = dashboard!.RecentInvoices.Single(r => r.Id == invoiceId);
            Assert.Equal(invoiceNet, recent.NetBilledAmount);

            var invoice = await client.GetFromJsonAsync<InvoiceDetailDto>($"/api/invoices/{invoiceId}", JsonOptions);
            Assert.NotNull(invoice);
            Assert.Equal(invoiceNet, invoice!.NetBilledAmount);
        }
    }

    [SqlIntegrationFact]
    public async Task Sage_export_record_count_matches_csv_data_rows_after_credits()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Sage Rows");
        var invoiceId = await GenerateInvoiceAsync(scenario);

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"sage-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var preview = await client.PostAsJsonAsync("/api/credit-notes/preview", new CreditNotePreviewRequest
        {
            InvoiceId = invoiceId,
            PeriodStart = scenario.PeriodStart,
            PeriodEnd = scenario.PeriodEnd,
            CreditNoteDate = scenario.PeriodEnd,
            Reason = "Sage rows"
        });
        preview.EnsureSuccessStatusCode();
        var previewBody = await preview.Content.ReadFromJsonAsync<CreditNotePreviewResponse>(JsonOptions);
        var firstLine = previewBody!.Lines.First();

        await client.PostAsJsonAsync("/api/credit-notes/generate", new CreditNotePreviewRequest
        {
            InvoiceId = invoiceId,
            PeriodStart = scenario.PeriodStart,
            PeriodEnd = scenario.PeriodEnd,
            CreditNoteDate = scenario.PeriodEnd,
            Reason = "Sage rows",
            LineAmounts = new Dictionary<int, decimal> { [firstLine.InvoiceLineId] = firstLine.CreditAmount }
        });

        var export = await client.PostAsJsonAsync("/api/sage-exports", new SageExportRequest
        {
            DateFrom = scenario.PeriodStart,
            DateTo = scenario.PeriodEnd
        });
        export.EnsureSuccessStatusCode();
        var batch = await export.Content.ReadFromJsonAsync<SageExportBatchDto>(JsonOptions);
        Assert.NotNull(batch);

        var file = await client.GetAsync($"/api/sage-exports/{batch!.Id}/file");
        file.EnsureSuccessStatusCode();
        var csv = await file.Content.ReadAsStringAsync();
        var dataRowCount = CountCsvDataRows(csv);
        Assert.Equal(batch.RecordCount, dataRowCount);
        Assert.True(batch.RecordCount >= 1);

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
        var invoices = await db.Invoices
            .Include(x => x.Lines)
            .ThenInclude(l => l.CreditNoteLines)
            .ThenInclude(c => c.CreditNote)
            .Where(x => x.SageExportBatchId == batch.Id)
            .ToListAsync();
        var columnMap = scope.ServiceProvider.GetRequiredService<Sage50ColumnMap>();
        Assert.Equal(columnMap.CountExportableLines(invoices), batch.RecordCount);
    }

    private static int CountCsvDataRows(string csv)
    {
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Math.Max(0, lines.Length - 1);
    }

    private HttpClient CreateAuthedClient(string token)
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static BillingPreviewRequest BuildBillingRequest(BillingScenario scenario) =>
        new()
        {
            CompanyId = scenario.Company.Id,
            CareHomeId = scenario.CareHome.Id,
            InvoiceCategoryId = scenario.Category.Id,
            PeriodStart = scenario.PeriodStart,
            PeriodEnd = scenario.PeriodEnd
        };

    private async Task<int> GenerateInvoiceAsync(BillingScenario scenario)
    {
        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"gen-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var generate = await client.PostAsJsonAsync("/api/billing/generate", BuildBillingRequest(scenario));
        generate.EnsureSuccessStatusCode();

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
        return await db.Invoices
            .Where(x => x.TenantId == scenario.Tenant.Id)
            .OrderByDescending(x => x.Id)
            .Select(x => x.Id)
            .FirstAsync();
    }
}
