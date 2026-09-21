using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Billing;
using CareHome.Api.Models;
using CareHome.Api.Payments.Dtos;
using CareHome.Api.Receivables.Domain;
using CareHome.Api.Receivables.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHome.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class PaymentsIntegrationTests(ApiIntegrationFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [SqlIntegrationFact]
    public async Task Create_and_allocate_payment_updates_receivables()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Pay AR");
        await GenerateInvoiceAsync(scenario);

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"pay-ar-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var invoice = await GetSingleInvoiceAsync(client);
        var partial = Math.Min(500m, Math.Max(1m, invoice.TotalAmount / 2m));

        var payment = await client.PostAsJsonAsync("/api/payments", new CreatePaymentRequest
        {
            FundingAuthorityId = scenario.FundingAuthority.Id,
            CareHomeId = scenario.CareHome.Id,
            ReceivedDate = scenario.PeriodEnd,
            Amount = partial,
            Reference = "TEST-PMT"
        });
        payment.EnsureSuccessStatusCode();
        var created = await payment.Content.ReadFromJsonAsync<PaymentDetailDto>(JsonOptions);
        Assert.NotNull(created);

        var allocate = await client.PostAsJsonAsync(
            $"/api/payments/{created!.PublicId}/allocations",
            new AllocatePaymentRequest
            {
                Allocations =
                [
                    new PaymentAllocationLineRequest
                    {
                        InvoicePublicId = invoice.PublicId,
                        Amount = partial
                    }
                ]
            });
        allocate.EnsureSuccessStatusCode();

        var receivables = await client.GetFromJsonAsync<PagedReceivables>("/api/receivables/invoices", JsonOptions);
        var row = receivables!.Items.Single();
        Assert.Equal(partial, row.PaidAmount);
        Assert.True(row.OutstandingAmount < row.OriginalAmount);
        Assert.Equal(ReceivableCollectionStatuses.PartiallyPaid, row.PaymentStatus);
    }

    [SqlIntegrationFact]
    public async Task Cross_tenant_payment_allocation_blocked()
    {
        var scenarioA = await IntegrationTestDataBuilder.SeedBillingTenantAsync(fixture.Factory.Services, "Pay A");
        var scenarioB = await IntegrationTestDataBuilder.SeedBillingTenantAsync(fixture.Factory.Services, "Pay B");
        await GenerateInvoiceAsync(scenarioB);

        var (_, tokenA) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenarioA.Tenant,
            AppRoles.Administrator,
            $"pay-x-{Guid.NewGuid():N}@test.local");

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
        var foreignInvoice = await db.Invoices.AsNoTracking()
            .Where(i => i.TenantId == scenarioB.Tenant.Id)
            .Select(i => i.PublicId)
            .FirstAsync();

        var client = CreateAuthedClient(tokenA);
        var payment = await client.PostAsJsonAsync("/api/payments", new CreatePaymentRequest
        {
            CareHomeId = scenarioA.CareHome.Id,
            ReceivedDate = scenarioA.PeriodEnd,
            Amount = 100m
        });
        payment.EnsureSuccessStatusCode();
        var created = await payment.Content.ReadFromJsonAsync<PaymentDetailDto>(JsonOptions);

        var allocate = await client.PostAsJsonAsync(
            $"/api/payments/{created!.PublicId}/allocations",
            new AllocatePaymentRequest
            {
                Allocations =
                [
                    new PaymentAllocationLineRequest { InvoicePublicId = foreignInvoice, Amount = 100m }
                ]
            });
        Assert.Equal(HttpStatusCode.BadRequest, allocate.StatusCode);
    }

    [SqlIntegrationFact]
    public async Task ReadOnly_user_cannot_create_payment()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Pay RO");
        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.ReadOnly,
            $"pay-ro-{Guid.NewGuid():N}@test.local");

        var response = await CreateAuthedClient(token).PostAsJsonAsync("/api/payments", new CreatePaymentRequest
        {
            CareHomeId = scenario.CareHome.Id,
            ReceivedDate = scenario.PeriodEnd,
            Amount = 50m
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [SqlIntegrationFact]
    public async Task Payment_reversal_restores_outstanding()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Pay Rev");
        await GenerateInvoiceAsync(scenario);

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"pay-rev-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var invoice = await GetSingleInvoiceAsync(client);
        var outstandingBefore = invoice.TotalAmount;

        var payment = await client.PostAsJsonAsync("/api/payments", new CreatePaymentRequest
        {
            CareHomeId = scenario.CareHome.Id,
            ReceivedDate = scenario.PeriodEnd,
            Amount = outstandingBefore,
            InitialAllocations =
            [
                new PaymentAllocationLineRequest
                {
                    InvoicePublicId = invoice.PublicId,
                    Amount = outstandingBefore
                }
            ]
        });
        payment.EnsureSuccessStatusCode();
        var created = await payment.Content.ReadFromJsonAsync<PaymentDetailDto>(JsonOptions);

        var reverse = await client.PostAsJsonAsync(
            $"/api/payments/{created!.PublicId}/reverse",
            new ReversePaymentRequest { Reason = "Test reversal" });
        reverse.EnsureSuccessStatusCode();

        var receivables = await client.GetFromJsonAsync<PagedReceivables>("/api/receivables/invoices", JsonOptions);
        var row = receivables!.Items.Single();
        Assert.Equal(outstandingBefore, row.OutstandingAmount);
        Assert.Equal(0m, row.PaidAmount);
    }

    [SqlIntegrationFact]
    public async Task Concurrent_over_allocation_on_invoice_blocked()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Pay Race");
        await GenerateInvoiceAsync(scenario);

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"pay-race-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var invoice = await GetSingleInvoiceAsync(client);

        async Task<PaymentDetailDto> CreatePaymentAsync()
        {
            var response = await client.PostAsJsonAsync("/api/payments", new CreatePaymentRequest
            {
                CareHomeId = scenario.CareHome.Id,
                ReceivedDate = scenario.PeriodEnd,
                Amount = 800m
            });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<PaymentDetailDto>(JsonOptions))!;
        }

        var paymentA = await CreatePaymentAsync();
        var paymentB = await CreatePaymentAsync();

        var taskA = client.PostAsJsonAsync(
            $"/api/payments/{paymentA.PublicId}/allocations",
            new AllocatePaymentRequest
            {
                Allocations =
                [
                    new PaymentAllocationLineRequest { InvoicePublicId = invoice.PublicId, Amount = 800m }
                ]
            });

        var taskB = client.PostAsJsonAsync(
            $"/api/payments/{paymentB.PublicId}/allocations",
            new AllocatePaymentRequest
            {
                Allocations =
                [
                    new PaymentAllocationLineRequest { InvoicePublicId = invoice.PublicId, Amount = 800m }
                ]
            });

        await Task.WhenAll(taskA, taskB);
        var results = new[] { taskA.Result.StatusCode, taskB.Result.StatusCode };
        Assert.Contains(HttpStatusCode.OK, results);
        Assert.Contains(HttpStatusCode.BadRequest, results);

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
        var invoiceId = await db.Invoices
            .Where(i => i.PublicId == invoice.PublicId)
            .Select(i => i.Id)
            .FirstAsync();
        var totalAllocated = await db.PaymentAllocations
            .Where(a => a.InvoiceId == invoiceId && !a.IsReversed)
            .SumAsync(a => a.AllocatedAmount);
        Assert.True(totalAllocated <= invoice.TotalAmount);
    }

    private sealed class PagedReceivables
    {
        public List<ReceivableInvoiceDto> Items { get; set; } = [];
    }

    private sealed class InvoiceRow
    {
        public Guid PublicId { get; set; }
        public decimal TotalAmount { get; set; }
    }

    private HttpClient CreateAuthedClient(string token)
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<InvoiceRow> GetSingleInvoiceAsync(HttpClient client)
    {
        var page = await client.GetFromJsonAsync<PagedResult<InvoiceListItem>>( "/api/invoices?pageSize=5", JsonOptions);
        var item = page!.Items.First();
        return new InvoiceRow { PublicId = item.PublicId, TotalAmount = item.TotalAmount };
    }

    private sealed class InvoiceListItem
    {
        public Guid PublicId { get; set; }
        public decimal TotalAmount { get; set; }
    }

    private async Task GenerateInvoiceAsync(BillingScenario scenario)
    {
        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"pay-gen-{Guid.NewGuid():N}@test.local");

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
}
