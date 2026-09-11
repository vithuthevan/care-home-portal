using CareHome.Api.Common;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Xunit;

namespace CareHome.Api.Tests;

public class TenantIsolationTests
{
    private sealed class SampleTenantRow : ITenantOwned
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void ForTenant_filters_to_requested_organisation_only()
    {
        var rows = new List<SampleTenantRow>
        {
            new() { Id = 1, TenantId = 10, Name = "A" },
            new() { Id = 2, TenantId = 20, Name = "B" },
            new() { Id = 3, TenantId = 10, Name = "C" }
        }.AsQueryable();

        var filtered = rows.ForTenant(10).Select(x => x.Id).ToList();

        Assert.Equal([1, 3], filtered);
    }

    [Fact]
    public void Operational_tenant_owned_models_expose_TenantId()
    {
        var tenantOwned = typeof(Company).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ITenantOwned).IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .Select(t => t.Name)
            .ToList();

        Assert.Contains(nameof(Company), tenantOwned);
        Assert.Contains(nameof(Invoice), tenantOwned);
        Assert.Contains(nameof(CreditNote), tenantOwned);
        Assert.Contains(nameof(ClientFundingContract), tenantOwned);
        Assert.Contains(nameof(MiscCharge), tenantOwned);
        Assert.DoesNotContain(nameof(FundingRate), tenantOwned);
        Assert.DoesNotContain(nameof(InvoiceLine), tenantOwned);
    }

    [Fact]
    public void Jwt_security_stamp_claim_type_is_stable()
    {
        var claim = JwtSecurityStamp.CreateClaim("stamp-value");
        Assert.Equal(JwtSecurityStamp.ClaimType, claim.Type);
        Assert.Equal("stamp-value", claim.Value);
        Assert.Throws<InvalidOperationException>(() => JwtSecurityStamp.CreateClaim(null));
        Assert.Throws<InvalidOperationException>(() => JwtSecurityStamp.CreateClaim(""));
    }
}
