using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CareHome.Api.Common;
using CareHome.Api.Dtos.Billing;
using CareHome.Api.Receivables.Domain;
using CareHome.Api.Reconciliation.Dtos;
using Xunit;
using static CareHome.Api.Tests.Integration.IntegrationTestDataBuilder;

namespace CareHome.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class ReconciliationIntegrationTests(ApiIntegrationFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [SqlIntegrationFact]
    public async Task Import_and_exact_reconciliation_updates_ar()
    {
        var scenario = await SeedBillingTenantAsync(fixture.Factory.Services, "Bank Rec");
        await GenerateInvoiceAsync(scenario);

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"bank-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var invoice = await GetSingleInvoiceAsync(client);

        var account = await client.PostAsJsonAsync("/api/banking/accounts", new CreateBankAccountRequest
        {
            Name = "Main Account"
        });
        account.EnsureSuccessStatusCode();
        var acct = await account.Content.ReadFromJsonAsync<BankAccountDto>(JsonOptions);
        Assert.NotNull(acct);

        var csv = $"Date,Amount,Reference\n{scenario.PeriodEnd:yyyy-MM-dd},{invoice.TotalAmount:0.00},INV\n";
        var checksum = ComputeSha256(csv);

        var commit = await client.PostAsJsonAsync("/api/banking/imports/commit", new
        {
            bankAccountPublicId = acct!.PublicId,
            fileName = "test.csv",
            contentChecksum = checksum,
            columnMapping = new { date = "Date", amount = "Amount", reference = "Reference" },
            acceptedRowNumbers = new[] { 2 },
            csvContent = csv
        });
        commit.EnsureSuccessStatusCode();

        var workspace = await client.GetFromJsonAsync<ReconciliationWorkspaceSummaryDto>(
            "/api/banking/reconciliation",
            JsonOptions);

        var txn = workspace!.Transactions.FirstOrDefault(t => t.Amount == invoice.TotalAmount);
        Assert.NotNull(txn);

        if (txn!.TopSuggestion is not null)
        {
            var confirm = await client.PostAsJsonAsync(
                $"/api/banking/reconciliation/transactions/{txn.PublicId}/confirm",
                new ConfirmReconciliationRequest { SuggestionPublicId = txn.TopSuggestion.PublicId });
            confirm.EnsureSuccessStatusCode();
        }
        else
        {
            var manual = await client.PostAsJsonAsync(
                $"/api/banking/reconciliation/transactions/{txn.PublicId}/confirm",
                new ConfirmReconciliationRequest
                {
                    ManualAllocations =
                    [
                        new CareHome.Api.Payments.Dtos.PaymentAllocationLineRequest
                        {
                            InvoicePublicId = invoice.PublicId,
                            Amount = invoice.TotalAmount
                        }
                    ]
                });
            manual.EnsureSuccessStatusCode();
        }

        var receivables = await client.GetFromJsonAsync<PagedReceivables>("/api/receivables/invoices", JsonOptions);
        var row = receivables!.Items.Single();
        Assert.Equal(0m, row.OutstandingAmount);
        Assert.Equal(ReceivableCollectionStatuses.Paid, row.PaymentStatus);
    }

    [SqlIntegrationFact]
    public async Task Duplicate_file_import_blocked()
    {
        var scenario = await SeedBillingTenantAsync(fixture.Factory.Services, "Dup File");
        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"dup-file-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var acctResponse = await client.PostAsJsonAsync(
            "/api/banking/accounts",
            new CreateBankAccountRequest { Name = "A" });
        var acct = (await acctResponse.Content.ReadFromJsonAsync<BankAccountDto>(JsonOptions))!;

        var csv = "Date,Amount\n2026-09-01,100.00\n";
        var checksum = ComputeSha256(csv);
        var body = new
        {
            bankAccountPublicId = acct.PublicId,
            fileName = "dup.csv",
            contentChecksum = checksum,
            columnMapping = new { date = "Date", amount = "Amount" },
            acceptedRowNumbers = new[] { 2 },
            csvContent = csv
        };

        var first = await client.PostAsJsonAsync("/api/banking/imports/commit", body);
        first.EnsureSuccessStatusCode();
        var second = await client.PostAsJsonAsync("/api/banking/imports/commit", body);
        Assert.False(second.IsSuccessStatusCode);
    }

    private static string ComputeSha256(string content)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes);
    }

    private HttpClient CreateAuthedClient(string token)
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task GenerateInvoiceAsync(BillingScenario scenario)
    {
        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"bank-gen-{Guid.NewGuid():N}@test.local");

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
    }

    private static async Task<InvoiceRow> GetSingleInvoiceAsync(HttpClient client)
    {
        var page = await client.GetFromJsonAsync<PagedResult<InvoiceListItem>>("/api/invoices?pageSize=5", JsonOptions);
        var item = page!.Items.First();
        return new InvoiceRow { PublicId = item.PublicId, TotalAmount = item.TotalAmount };
    }

    private sealed class InvoiceRow
    {
        public Guid PublicId { get; set; }

        public decimal TotalAmount { get; set; }
    }

    private sealed class InvoiceListItem
    {
        public Guid PublicId { get; set; }

        public decimal TotalAmount { get; set; }
    }

    private sealed class PagedReceivables
    {
        public List<ReceivableRow> Items { get; set; } = [];
    }

    private sealed class ReceivableRow
    {
        public decimal OutstandingAmount { get; set; }

        public string PaymentStatus { get; set; } = string.Empty;
    }

    [SqlIntegrationFact]
    public async Task Partial_manual_reconciliation_leaves_unapplied_cash()
    {
        var scenario = await SeedBillingTenantAsync(fixture.Factory.Services, "Partial Rec");
        await GenerateInvoiceAsync(scenario);
        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"partial-{Guid.NewGuid():N}@test.local");
        var client = CreateAuthedClient(token);
        var invoice = await GetSingleInvoiceAsync(client);

        var acct = (await (await client.PostAsJsonAsync("/api/banking/accounts", new CreateBankAccountRequest { Name = "P" }))
            .Content.ReadFromJsonAsync<BankAccountDto>(JsonOptions))!;

        var partial = invoice.TotalAmount / 2;
        var csv = $"Date,Amount,Reference\n{scenario.PeriodEnd:yyyy-MM-dd},{invoice.TotalAmount:0.00},PART\n";
        var checksum = ComputeSha256(csv);
        await client.PostAsJsonAsync("/api/banking/imports/commit", new
        {
            bankAccountPublicId = acct.PublicId,
            fileName = "p.csv",
            contentChecksum = checksum,
            columnMapping = new { date = "Date", amount = "Amount", reference = "Reference" },
            acceptedRowNumbers = new[] { 2 },
            csvContent = csv
        });

        var workspace = await client.GetFromJsonAsync<ReconciliationWorkspaceSummaryDto>("/api/banking/reconciliation", JsonOptions);
        var txn = workspace!.Transactions.First();

        var confirm = await client.PostAsJsonAsync(
            $"/api/banking/reconciliation/transactions/{txn.PublicId}/confirm",
            new ConfirmReconciliationRequest
            {
                ManualAllocations =
                [
                    new CareHome.Api.Payments.Dtos.PaymentAllocationLineRequest
                    {
                        InvoicePublicId = invoice.PublicId,
                        Amount = partial
                    }
                ]
            });
        confirm.EnsureSuccessStatusCode();

        var receivables = await client.GetFromJsonAsync<PagedReceivables>("/api/receivables/invoices", JsonOptions);
        Assert.True(receivables!.Items.Single().OutstandingAmount > 0);
    }

    [SqlIntegrationFact]
    public async Task Cross_tenant_reconciliation_denied()
    {
        var a = await SeedBillingTenantAsync(fixture.Factory.Services, "Tenant A Rec");
        var b = await SeedBillingTenantAsync(fixture.Factory.Services, "Tenant B Rec");
        var (_, tokenA) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services, a.Tenant, AppRoles.Administrator, $"a-{Guid.NewGuid():N}@test.local");
        var (_, tokenB) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services, b.Tenant, AppRoles.Administrator, $"b-{Guid.NewGuid():N}@test.local");
        var clientA = CreateAuthedClient(tokenA);
        var clientB = CreateAuthedClient(tokenB);

        var acctB = (await (await clientB.PostAsJsonAsync("/api/banking/accounts", new CreateBankAccountRequest { Name = "B" }))
            .Content.ReadFromJsonAsync<BankAccountDto>(JsonOptions))!;
        var csv = "Date,Amount\n2026-09-01,50.00\n";
        var checksum = ComputeSha256(csv);
        await clientB.PostAsJsonAsync("/api/banking/imports/commit", new
        {
            bankAccountPublicId = acctB.PublicId,
            fileName = "b.csv",
            contentChecksum = checksum,
            columnMapping = new { date = "Date", amount = "Amount" },
            acceptedRowNumbers = new[] { 2 },
            csvContent = csv
        });
        var wsB = await clientB.GetFromJsonAsync<ReconciliationWorkspaceSummaryDto>("/api/banking/reconciliation", JsonOptions);
        var txnPublicId = wsB!.Transactions.First().PublicId;

        var denied = await clientA.PostAsJsonAsync(
            $"/api/banking/reconciliation/transactions/{txnPublicId}/confirm",
            new ConfirmReconciliationRequest { ManualAllocations = [] });
        Assert.False(denied.IsSuccessStatusCode);
    }
}
