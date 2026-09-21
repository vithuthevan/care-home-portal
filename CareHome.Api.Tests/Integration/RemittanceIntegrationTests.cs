using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using CareHome.Api.Common;
using CareHome.Api.Remittance.Dtos;
using Xunit;
using static CareHome.Api.Tests.Integration.IntegrationTestDataBuilder;

namespace CareHome.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class RemittanceIntegrationTests(ApiIntegrationFixture fixture)
{
    [SqlIntegrationFact]
    public async Task Import_remittance_batch()
    {
        var scenario = await SeedBillingTenantAsync(fixture.Factory.Services, "Remit");
        var (_, token) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services,
            scenario.Tenant,
            AppRoles.Administrator,
            $"remit-{Guid.NewGuid():N}@test.local");

        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var csv = "Invoice,Resident,Gross,Paid,Deduction,Reason,Notes\nINV-TEST,,100,100,0,,\n";
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(csv, Encoding.UTF8, "text/csv"), "file", "rem.csv");

        var import = await client.PostAsync("/api/remittances/import/csv", content);
        import.EnsureSuccessStatusCode();
        var batch = await import.Content.ReadFromJsonAsync<RemittanceBatchDetailDto>();
        Assert.NotNull(batch);
        Assert.NotEmpty(batch!.Lines);
    }

    [SqlIntegrationFact]
    public async Task Cross_tenant_remittance_denied()
    {
        var a = await SeedBillingTenantAsync(fixture.Factory.Services, "Rem A");
        var b = await SeedBillingTenantAsync(fixture.Factory.Services, "Rem B");
        var (_, tokenA) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services, a.Tenant, AppRoles.Administrator, $"ra-{Guid.NewGuid():N}@test.local");
        var (_, tokenB) = await IntegrationTestAuth.CreateTenantUserAsync(
            fixture.Factory.Services, b.Tenant, AppRoles.Administrator, $"rb-{Guid.NewGuid():N}@test.local");

        var clientB = fixture.Factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var csv = "Invoice,Resident,Gross,Paid,Deduction,Reason,Notes\nX,,10,10,0,,\n";
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(csv, Encoding.UTF8, "text/csv"), "file", "r.csv");
        var import = await clientB.PostAsync("/api/remittances/import/csv", content);
        import.EnsureSuccessStatusCode();
        var batch = (await import.Content.ReadFromJsonAsync<RemittanceBatchDetailDto>())!;

        var clientA = fixture.Factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var denied = await clientA.GetAsync($"/api/remittances/{batch.PublicId}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, denied.StatusCode);
    }
}
