using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Billing;
using CareHome.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHome.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class BulkBillingIntegrationTests(ApiIntegrationFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [SqlIntegrationFact]
    public async Task Company_wide_generate_creates_multiple_invoices_for_multiple_homes()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Bulk Homes");

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
            var clientB = new Client
            {
                TenantId = scenario.Tenant.Id,
                CareHomeId = scenario.OtherCareHome.Id,
                SageId = "SAGE-B",
                ReferenceNumber = $"REF-B-{Guid.NewGuid():N}".Substring(0, 12),
                FirstName = "Second",
                LastName = "Resident",
                CareType = "Residential",
                Status = "Current",
                AdmissionDate = new DateOnly(2026, 1, 1)
            };
            db.Clients.Add(clientB);
            await db.SaveChangesAsync();

            db.ClientFundingContracts.Add(new ClientFundingContract
            {
                TenantId = scenario.Tenant.Id,
                ClientId = clientB.Id,
                FundingAuthorityId = scenario.FundingAuthority.Id,
                InvoiceCategoryId = scenario.Category.Id,
                NominalCodeId = scenario.Nominal.Id,
                InvoiceTemplateId = scenario.Template.Id,
                ContractStartDate = new DateOnly(2026, 1, 1),
                Status = "Active",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();

            var contractId = await db.ClientFundingContracts
                .Where(x => x.ClientId == clientB.Id)
                .Select(x => x.Id)
                .FirstAsync();
            db.FundingRates.Add(new FundingRate
            {
                ClientFundingContractId = contractId,
                EffectiveFrom = new DateOnly(2026, 1, 1),
                Frequency = "Weekly",
                Amount = 700m,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"bulk-homes-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var request = new BillingPreviewRequest
        {
            CompanyId = scenario.Company.Id,
            CareHomeId = null,
            InvoiceCategoryId = scenario.Category.Id,
            PeriodStart = scenario.PeriodStart,
            PeriodEnd = scenario.PeriodEnd
        };

        var preview = await client.PostAsJsonAsync("/api/billing/preview", request);
        preview.EnsureSuccessStatusCode();
        var previewBody = await preview.Content.ReadFromJsonAsync<BillingPreviewResponse>(JsonOptions);
        Assert.NotNull(previewBody);
        Assert.True(previewBody!.ExpectedInvoiceCount >= 2);
        Assert.True(previewBody.InvoiceGroups.Count >= 2);

        var generate = await client.PostAsJsonAsync("/api/billing/generate", request);
        generate.EnsureSuccessStatusCode();
        var generateBody = await generate.Content.ReadFromJsonAsync<BillingGenerateResponse>(JsonOptions);
        Assert.NotNull(generateBody);
        Assert.True(generateBody!.InvoiceCount >= 2);
        Assert.Equal(generateBody.InvoiceCount, generateBody.InvoiceIds.Count);
        Assert.Equal(previewBody.ExpectedInvoiceCount, generateBody.InvoiceCount);
    }

    [SqlIntegrationFact]
    public async Task Rent_category_generates_one_invoice_per_resident()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Bulk Rent");

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
            var rentCategory = await db.InvoiceCategories
                .FirstAsync(x => x.TenantId == scenario.Tenant.Id && x.Code == "RENT");
            rentCategory.GroupingMode = InvoiceGroupingModes.PerResident;
            db.InvoiceTemplates.Add(new InvoiceTemplate
            {
                TenantId = scenario.Tenant.Id,
                Name = "Rent integration template",
                InvoiceCategoryId = rentCategory.Id,
                HeaderText1 = "Rent Invoice",
                FooterText = "Thank you",
                ContactEmail = "finance@test.local",
                EmailSubjectTemplate = "Invoice {{InvoiceNumber}}",
                EmailBodyTemplate = "Attached",
                IsActive = true
            });
            await db.SaveChangesAsync();

            var clientB = new Client
            {
                TenantId = scenario.Tenant.Id,
                CareHomeId = scenario.CareHome.Id,
                SageId = "SAGE-R2",
                ReferenceNumber = $"REF-R2-{Guid.NewGuid():N}".Substring(0, 12),
                FirstName = "Rent",
                LastName = "Two",
                CareType = "Residential",
                Status = "Current",
                AdmissionDate = new DateOnly(2026, 1, 1)
            };
            db.Clients.Add(clientB);
            await db.SaveChangesAsync();

            foreach (var clientId in new[] { scenario.Client.Id, clientB.Id })
            {
                db.ClientFundingContracts.Add(new ClientFundingContract
                {
                    TenantId = scenario.Tenant.Id,
                    ClientId = clientId,
                    FundingAuthorityId = scenario.FundingAuthority.Id,
                    InvoiceCategoryId = rentCategory.Id,
                    NominalCodeId = scenario.Nominal.Id,
                    InvoiceTemplateId = scenario.Template.Id,
                    ContractStartDate = new DateOnly(2026, 1, 1),
                    Status = "Active",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }

            await db.SaveChangesAsync();

            var contracts = await db.ClientFundingContracts
                .Where(x => x.InvoiceCategoryId == rentCategory.Id)
                .ToListAsync();
            foreach (var contract in contracts)
            {
                db.FundingRates.Add(new FundingRate
                {
                    ClientFundingContractId = contract.Id,
                    EffectiveFrom = new DateOnly(2026, 1, 1),
                    Frequency = "Weekly",
                    Amount = 500m,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"bulk-rent-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var rentCategoryId = await GetCategoryIdAsync(scenario.Tenant.Id, "RENT");
        var request = new BillingPreviewRequest
        {
            CompanyId = scenario.Company.Id,
            CareHomeId = scenario.CareHome.Id,
            InvoiceCategoryId = rentCategoryId,
            PeriodStart = scenario.PeriodStart,
            PeriodEnd = scenario.PeriodEnd
        };

        var preview = await client.PostAsJsonAsync("/api/billing/preview", request);
        preview.EnsureSuccessStatusCode();
        var previewBody = await preview.Content.ReadFromJsonAsync<BillingPreviewResponse>(JsonOptions);
        Assert.NotNull(previewBody);
        Assert.Equal(2, previewBody!.ExpectedInvoiceCount);

        var generate = await client.PostAsJsonAsync("/api/billing/generate", request);
        generate.EnsureSuccessStatusCode();
        var generateBody = await generate.Content.ReadFromJsonAsync<BillingGenerateResponse>(JsonOptions);
        Assert.NotNull(generateBody);
        Assert.Equal(2, generateBody!.InvoiceCount);
    }

    [SqlIntegrationFact]
    public async Task Provisioned_categories_default_rent_and_misc_to_per_resident()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Grouping Defaults");

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
        var categories = await db.InvoiceCategories
            .Where(x => x.TenantId == scenario.Tenant.Id)
            .ToListAsync();

        Assert.Equal(InvoiceGroupingModes.PerResident, categories.First(x => x.Code == "RENT").GroupingMode);
        Assert.Equal(InvoiceGroupingModes.PerResident, categories.First(x => x.Code == "MISC").GroupingMode);
        Assert.Equal(InvoiceGroupingModes.PerFunder, categories.First(x => x.Code == "GENERAL_CARE").GroupingMode);
    }

    private async Task<int> GetCategoryIdAsync(int tenantId, string code)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
        return await db.InvoiceCategories
            .Where(x => x.TenantId == tenantId && x.Code == code)
            .Select(x => x.Id)
            .FirstAsync();
    }

    private HttpClient CreateAuthedClient(string token)
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
