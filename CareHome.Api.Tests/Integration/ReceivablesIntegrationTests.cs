using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Billing;
using CareHome.Api.Dtos.CreditNotes;
using CareHome.Api.Models;
using CareHome.Api.Receivables.Domain;
using CareHome.Api.Receivables.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHome.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class ReceivablesIntegrationTests(ApiIntegrationFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [SqlIntegrationFact]
    public async Task Tenant_A_cannot_read_Tenant_B_receivables_summary()
    {
        var scenarioA = await IntegrationTestDataBuilder.SeedBillingTenantAsync(fixture.Factory.Services, "AR A");
        var scenarioB = await IntegrationTestDataBuilder.SeedBillingTenantAsync(fixture.Factory.Services, "AR B");
        await GenerateInvoiceAsync(scenarioB);

        var (_, tokenA) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenarioA.Tenant,
            AppRoles.Administrator,
            $"ar-a-{Guid.NewGuid():N}@test.local");

        var response = await CreateAuthedClient(tokenA).GetAsync("/api/receivables/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<ReceivablesSummaryDto>(JsonOptions);
        Assert.NotNull(summary);
        Assert.Equal(0m, summary!.TotalOutstanding);
    }

    [SqlIntegrationFact]
    public async Task LocationManager_receivables_exclude_unassigned_home()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "AR Location");

        await GenerateInvoiceOnHomeAsync(scenario, scenario.OtherCareHome);

        var (_, managerToken) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.LocationManager,
            $"ar-lm-{Guid.NewGuid():N}@test.local",
            careHomeIds: [scenario.CareHome.Id]);

        var response = await CreateAuthedClient(managerToken).GetAsync("/api/receivables/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<ReceivablesSummaryDto>(JsonOptions);
        Assert.NotNull(summary);
        Assert.Equal(0m, summary!.TotalOutstanding);
    }

    [SqlIntegrationFact]
    public async Task Receivables_summary_reflects_generated_invoice()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "AR Summary");
        await GenerateInvoiceAsync(scenario);

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"ar-sum-{Guid.NewGuid():N}@test.local");

        var response = await CreateAuthedClient(token).GetAsync("/api/receivables/summary");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<ReceivablesSummaryDto>(JsonOptions);
        Assert.NotNull(summary);
        Assert.True(summary!.TotalOutstanding > 0);
        Assert.True(summary.OpenInvoiceCount >= 1);
    }

    [SqlIntegrationFact]
    public async Task Credit_note_reduces_outstanding_without_marking_paid()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "AR Credit");
        await GenerateInvoiceAsync(scenario);

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"ar-cn-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var preview = await client.PostAsJsonAsync("/api/credit-notes/preview", new CreditNotePreviewRequest
        {
            PeriodStart = scenario.PeriodStart,
            PeriodEnd = scenario.PeriodEnd,
            CreditNoteDate = scenario.PeriodEnd,
            Reason = "Partial adjustment"
        });
        preview.EnsureSuccessStatusCode();
        var previewBody = await preview.Content.ReadFromJsonAsync<CreditNotePreviewResponse>(JsonOptions);
        Assert.NotNull(previewBody);
        Assert.True(previewBody!.CanGenerate);

        var firstLine = previewBody.Lines.First();
        var generate = await client.PostAsJsonAsync("/api/credit-notes/generate", new CreditNotePreviewRequest
        {
            PeriodStart = scenario.PeriodStart,
            PeriodEnd = scenario.PeriodEnd,
            CreditNoteDate = scenario.PeriodEnd,
            Reason = "Partial adjustment",
            LineAmounts = new Dictionary<int, decimal> { [firstLine.InvoiceLineId] = firstLine.CreditAmount }
        });
        generate.EnsureSuccessStatusCode();

        var invoices = await client.GetAsync("/api/receivables/invoices?pageSize=10");
        invoices.EnsureSuccessStatusCode();
        var page = await invoices.Content.ReadFromJsonAsync<PagedReceivables>(JsonOptions);
        Assert.NotNull(page);
        var row = page!.Items.Single();
        Assert.True(row.CreditedAmount > 0);
        Assert.True(row.OutstandingAmount < row.OriginalAmount);
        Assert.Equal(0m, row.PaidAmount);
        Assert.Equal(ReceivableCollectionStatuses.Unpaid, row.PaymentStatus);
    }

    [SqlIntegrationFact]
    public async Task Voided_invoice_excluded_from_open_receivables()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "AR Void");
        var invoiceId = await GenerateInvoiceAsync(scenario);

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"ar-void-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var voidResponse = await client.PostAsync($"/api/invoices/{invoiceId}/void", null);
        voidResponse.EnsureSuccessStatusCode();

        var summary = await client.GetFromJsonAsync<ReceivablesSummaryDto>("/api/receivables/summary", JsonOptions);
        Assert.NotNull(summary);
        Assert.Equal(0m, summary!.TotalOutstanding);
    }

    [SqlIntegrationFact]
    public async Task ReadOnly_user_can_view_receivables()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "AR ReadOnly");
        await GenerateInvoiceAsync(scenario);

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.ReadOnly,
            $"ar-ro-{Guid.NewGuid():N}@test.local");

        var response = await CreateAuthedClient(token).GetAsync("/api/receivables/invoices");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class PagedReceivables
    {
        public List<ReceivableInvoiceDto> Items { get; set; } = [];
    }

    private HttpClient CreateAuthedClient(string token)
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task GenerateInvoiceOnHomeAsync(BillingScenario scenario, CareHomeLocation home)
    {
        var request = new BillingPreviewRequest
        {
            CompanyId = scenario.Company.Id,
            CareHomeId = home.Id,
            InvoiceCategoryId = scenario.Category.Id,
            PeriodStart = scenario.PeriodStart,
            PeriodEnd = scenario.PeriodEnd
        };

        using var scope = fixture.Factory.Services.CreateScope();
        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"gen-home-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var generate = await client.PostAsJsonAsync("/api/billing/generate", request);
        generate.EnsureSuccessStatusCode();
    }

    private async Task<int> GenerateInvoiceAsync(BillingScenario scenario)
    {
        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"gen-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var generate = await client.PostAsJsonAsync("/api/billing/generate", new BillingPreviewRequest
        {
            CompanyId = scenario.Company.Id,
            CareHomeId = scenario.CareHome.Id,
            InvoiceCategoryId = scenario.Category.Id,
            PeriodStart = scenario.PeriodStart,
            PeriodEnd = scenario.PeriodEnd
        });
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
