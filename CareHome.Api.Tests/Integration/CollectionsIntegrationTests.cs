using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CareHome.Api.Common;
using CareHome.Api.Dtos.Billing;
using CareHome.Api.Dtos.Collections;
using Xunit;

namespace CareHome.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class CollectionsIntegrationTests(ApiIntegrationFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [SqlIntegrationFact]
    public async Task Collections_dashboard_returns_policy_and_overdue_totals()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Collections Dash");
        await GenerateInvoiceAsync(scenario);

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"col-dash-{Guid.NewGuid():N}@test.local");

        var response = await CreateAuthedClient(token).GetAsync("/api/collections/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CollectionsDashboardDto>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotNull(body!.Policy);
        Assert.True(body.Overdue >= 0);
    }

    [SqlIntegrationFact]
    public async Task Collections_policy_rejects_invalid_threshold_order()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Collections Policy");

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"col-pol-{Guid.NewGuid():N}@test.local");

        var response = await CreateAuthedClient(token).PutAsJsonAsync(
            "/api/collections/policy",
            new UpdateCollectionPolicyRequest
            {
                Overdue7Days = 20,
                Overdue14Days = 10,
                Overdue30Days = 30,
                EscalationDays = 60,
            });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [SqlIntegrationFact]
    public async Task Send_reminders_reports_disabled_when_policy_off()
    {
        var scenario = await IntegrationTestDataBuilder.SeedBillingTenantAsync(
            fixture.Factory.Services,
            "Collections Send");

        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"col-send-{Guid.NewGuid():N}@test.local");

        var client = CreateAuthedClient(token);
        var response = await client.PostAsync("/api/collections/send-reminders", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CollectionReminderRunResultDto>(JsonOptions);
        Assert.NotNull(body);
        Assert.True(body!.RemindersDisabled);
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
            $"col-gen-{Guid.NewGuid():N}@test.local");

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
