using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Billing;
using CareHome.Api.Dtos.CreditNotes;
using CareHome.Api.Dtos.Tenants;
using CareHome.Api.Dtos.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CareHome.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class ApiIntegrationTests(ApiIntegrationFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [SqlIntegrationFact]
    public async Task Login_with_valid_credentials_returns_token()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Login Tenant");
        const string email = "login-user@test.local";
        const string password = "IntegrationTest!Pass1";
        await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.TenantAdmin,
            email,
            password);

        var client = fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.False(string.IsNullOrWhiteSpace(body?.Token));
    }

    [SqlIntegrationFact]
    public async Task Tenant_A_cannot_read_Tenant_B_invoice()
    {
        var scenarioA = await IntegrationTestDataBuilder.SeedBillingTenantAsync(fixture.Factory.Services, "Tenant A");
        var scenarioB = await IntegrationTestDataBuilder.SeedBillingTenantAsync(fixture.Factory.Services, "Tenant B");

        var (_, tokenA) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenarioA.Tenant,
            AppRoles.Administrator,
            $"user-a-{Guid.NewGuid():N}@test.local");

        var invoiceIdB = await GenerateInvoiceAsync(scenarioB);

        var client = CreateAuthedClient(tokenA);
        var response = await client.GetAsync($"/api/invoices/{invoiceIdB}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [SqlIntegrationFact]
    public async Task LocationManager_cannot_access_unassigned_home_client()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Location Scope");

        var (_, managerToken) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.LocationManager,
            $"lm-{Guid.NewGuid():N}@test.local",
            careHomeIds: [scenario.CareHome.Id]);

        var client = CreateAuthedClient(managerToken);
        var response = await client.GetAsync($"/api/clients/{scenario.Client.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
        var otherClient = new CareHome.Api.Models.Client
        {
            TenantId = scenario.Tenant.Id,
            CareHomeId = scenario.OtherCareHome.Id,
            SageId = "S2",
            ReferenceNumber = $"REF-{Guid.NewGuid():N}".Substring(0, 12),
            FirstName = "Other",
            LastName = "Home",
            CareType = "Residential",
            Status = "Current",
            AdmissionDate = new DateOnly(2026, 1, 1)
        };
        db.Clients.Add(otherClient);
        await db.SaveChangesAsync();

        var blocked = await client.GetAsync($"/api/clients/{otherClient.Id}");
        Assert.Equal(HttpStatusCode.NotFound, blocked.StatusCode);
    }

    [SqlIntegrationFact]
    public async Task ReadOnly_user_cannot_generate_billing()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "ReadOnly Billing");

        var (_, readOnlyToken) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.ReadOnly,
            $"ro-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(readOnlyToken);
        var response = await client.PostAsJsonAsync("/api/billing/generate", BuildBillingRequest(scenario));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [SqlIntegrationFact]
    public async Task Billing_preview_and_generate_succeed_for_administrator()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Billing Flow");

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"bill-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var preview = await client.PostAsJsonAsync("/api/billing/preview", BuildBillingRequest(scenario));
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);

        var previewBody = await preview.Content.ReadFromJsonAsync<BillingPreviewResponse>(JsonOptions);
        Assert.NotNull(previewBody);
        Assert.True(previewBody!.CanGenerate);
        Assert.True(previewBody.TotalAmount > 0);

        var generate = await client.PostAsJsonAsync("/api/billing/generate", BuildBillingRequest(scenario));
        Assert.Equal(HttpStatusCode.OK, generate.StatusCode);
    }

    [SqlIntegrationFact]
    public async Task Duplicate_billing_generation_is_blocked()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Duplicate Billing");

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"dup-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var first = await client.PostAsJsonAsync("/api/billing/generate", BuildBillingRequest(scenario));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/billing/generate", BuildBillingRequest(scenario));
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        var payload = await second.Content.ReadAsStringAsync();
        Assert.Contains("already fully billed", payload, StringComparison.OrdinalIgnoreCase);
    }

    [SqlIntegrationFact]
    public async Task Invoice_pdf_requires_authorization_and_tenant_scope()
    {
        var scenarioA = await IntegrationTestDataBuilder.SeedBillingTenantAsync(fixture.Factory.Services, "PDF A");
        var scenarioB = await IntegrationTestDataBuilder.SeedBillingTenantAsync(fixture.Factory.Services, "PDF B");

        var invoiceIdA = await GenerateInvoiceAsync(scenarioA, $"pdf-admin-{Guid.NewGuid():N}@test.local");

        var anonymous = fixture.Factory.CreateClient();
        var unauth = await anonymous.GetAsync($"/api/invoices/{invoiceIdA}/pdf");
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);

        var (_, tokenB) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenarioB.Tenant,
            AppRoles.Administrator,
            $"pdf-b-{Guid.NewGuid():N}@test.local");

        var crossTenant = await CreateAuthedClient(tokenB).GetAsync($"/api/invoices/{invoiceIdA}/pdf");
        Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);

        var (_, tokenA) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenarioA.Tenant,
            AppRoles.Administrator,
            $"pdf-a-{Guid.NewGuid():N}@test.local");

        var allowed = await CreateAuthedClient(tokenA).GetAsync($"/api/invoices/{invoiceIdA}/pdf");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal("application/pdf", allowed.Content.Headers.ContentType?.MediaType);
    }

    [SqlIntegrationFact]
    public async Task ReadOnly_user_cannot_create_credit_note_preview()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Credit ReadOnly");

        var invoiceId = await GenerateInvoiceAsync(scenario);

        var (_, readOnlyToken) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.ReadOnly,
            $"cn-ro-{Guid.NewGuid():N}@test.local");

        var request = new CreditNotePreviewRequest
        {
            PeriodStart = scenario.PeriodStart,
            PeriodEnd = scenario.PeriodEnd,
            CreditNoteDate = scenario.PeriodEnd,
            Reason = "Test"
        };

        var response = await CreateAuthedClient(readOnlyToken)
            .PostAsJsonAsync("/api/credit-notes/preview", request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [SqlIntegrationFact]
    public async Task Platform_admin_can_provision_tenant_but_not_read_operational_clients()
    {
        var platformToken = await IntegrationTestAuth.CreatePlatformAdminTokenAsync(fixture.Factory.Services);
        var client = CreateAuthedClient(platformToken);

        var provision = await client.PostAsJsonAsync("/api/platform/tenants", new CreateTenantRequest
        {
            Name = $"Provisioned {Guid.NewGuid():N}",
            IsActive = true,
            AdminEmail = $"admin-{Guid.NewGuid():N}@provision.test"
        });
        Assert.Equal(HttpStatusCode.Created, provision.StatusCode);

        var operational = await client.GetAsync("/api/clients");
        Assert.Equal(HttpStatusCode.Forbidden, operational.StatusCode);
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

    private async Task<int> GenerateInvoiceAsync(BillingScenario scenario, string? adminEmail = null)
    {
        adminEmail ??= $"gen-{Guid.NewGuid():N}@test.local";
        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            adminEmail);

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
