using CareHome.Api.Billing;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CareHome.Api.Tests;

public class InvoiceTemplateResolverTests
{
    [Fact]
    public async Task Run_override_beats_contract_pin_and_resolver()
    {
        await using var db = CreateDb();
        var data = await SeedTemplatesAsync(db);

        var resolver = new InvoiceTemplateResolver(db);
        var result = await resolver.ResolveForBillingAsync(
            data.TenantId,
            data.CategoryId,
            data.AuthorityId,
            data.CareHomeId,
            data.CompanyId,
            data.PinnedTemplateId,
            data.OverrideTemplateId);

        Assert.NotNull(result);
        Assert.Equal(data.OverrideTemplateId, result!.Id);
        Assert.Equal("Override template", result.Name);
    }

    [Fact]
    public async Task Contract_pin_beats_resolver_when_no_run_override()
    {
        await using var db = CreateDb();
        var data = await SeedTemplatesAsync(db);

        var resolver = new InvoiceTemplateResolver(db);
        var result = await resolver.ResolveForBillingAsync(
            data.TenantId,
            data.CategoryId,
            data.AuthorityId,
            data.CareHomeId,
            data.CompanyId,
            data.PinnedTemplateId,
            runOverrideTemplateId: null);

        Assert.NotNull(result);
        Assert.Equal(data.PinnedTemplateId, result!.Id);
        Assert.Equal("Pinned template", result.Name);
    }

    [Fact]
    public async Task LoadPinned_returns_null_for_wrong_category()
    {
        await using var db = CreateDb();
        var data = await SeedTemplatesAsync(db);

        var resolver = new InvoiceTemplateResolver(db);
        var result = await resolver.LoadPinnedAsync(
            data.TenantId,
            data.OverrideTemplateId,
            data.CategoryId + 999);

        Assert.Null(result);
    }

    private static CareHomeDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CareHomeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CareHomeDbContext(options);
    }

    private static async Task<TemplateSeed> SeedTemplatesAsync(CareHomeDbContext db)
    {
        var tenant = new Tenant { PublicId = Guid.NewGuid(), Name = "Test tenant", IsActive = true };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var category = new InvoiceCategory
        {
            TenantId = tenant.Id,
            Name = "General",
            Code = "GENERAL",
            GroupingMode = InvoiceGroupingModes.PerFunder,
            IsActive = true,
        };
        db.InvoiceCategories.Add(category);

        var authority = new FundingAuthority
        {
            TenantId = tenant.Id,
            Name = "Council",
            Code = "COUNCIL",
            Type = "Local",
            IsActive = true,
            BillingFrequency = "Monthly",
        };
        db.FundingAuthorities.Add(authority);

        var company = new Company
        {
            TenantId = tenant.Id,
            Name = "Co",
            IsActive = true,
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var home = new CareHomeLocation
        {
            TenantId = tenant.Id,
            CompanyId = company.Id,
            Name = "Home",
            Code = "HOME",
            IsActive = true,
        };
        db.CareHomes.Add(home);
        await db.SaveChangesAsync();

        var resolverWinner = new InvoiceTemplate
        {
            TenantId = tenant.Id,
            Name = "Resolver scoped",
            InvoiceCategoryId = category.Id,
            FundingAuthorityId = authority.Id,
            CareHomeId = home.Id,
            IsActive = true,
        };
        var pinned = new InvoiceTemplate
        {
            TenantId = tenant.Id,
            Name = "Pinned template",
            InvoiceCategoryId = category.Id,
            IsActive = true,
        };
        var overrideTemplate = new InvoiceTemplate
        {
            TenantId = tenant.Id,
            Name = "Override template",
            InvoiceCategoryId = category.Id,
            IsActive = true,
        };
        db.InvoiceTemplates.AddRange(resolverWinner, pinned, overrideTemplate);
        await db.SaveChangesAsync();

        return new TemplateSeed(
            tenant.Id,
            category.Id,
            authority.Id,
            home.Id,
            company.Id,
            pinned.Id,
            overrideTemplate.Id);
    }

    private sealed record TemplateSeed(
        int TenantId,
        int CategoryId,
        int AuthorityId,
        int CareHomeId,
        int CompanyId,
        int PinnedTemplateId,
        int OverrideTemplateId);
}
