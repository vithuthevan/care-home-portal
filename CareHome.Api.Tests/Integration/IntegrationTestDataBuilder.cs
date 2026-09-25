using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CareHome.Api.Tests.Integration;

public sealed record BillingScenario(
    Tenant Tenant,
    Company Company,
    CareHomeLocation CareHome,
    CareHomeLocation OtherCareHome,
    FundingAuthority FundingAuthority,
    InvoiceCategory Category,
    InvoiceTemplate Template,
    NominalCode Nominal,
    Client Client,
    ClientFundingContract Contract,
    DateOnly PeriodStart,
    DateOnly PeriodEnd);

public static class IntegrationTestDataBuilder
{
    public static async Task<BillingScenario> SeedBillingTenantAsync(
        IServiceProvider services,
        string tenantName)
    {
        using var scope = services.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<TenantProvisioningService>();
        var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();

        var provisioned = await provisioning.ProvisionAsync(new TenantProvisionRequest
        {
            Name = tenantName,
            IsActive = true
        });

        var tenant = provisioned.Tenant;
        var settings = await db.TenantSettings.FirstAsync(x => x.TenantId == tenant.Id);
        settings.FinanceModuleEnabled = true;
        var company = new Company
        {
            TenantId = tenant.Id,
            Name = $"{tenantName} Ltd",
            IsActive = true
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var home = new CareHomeLocation
        {
            TenantId = tenant.Id,
            CompanyId = company.Id,
            Code = "HOME-A",
            Name = $"{tenantName} Home A",
            BedCapacity = 30,
            IsActive = true
        };
        var otherHome = new CareHomeLocation
        {
            TenantId = tenant.Id,
            CompanyId = company.Id,
            Code = "HOME-B",
            Name = $"{tenantName} Home B",
            BedCapacity = 20,
            IsActive = true
        };
        db.CareHomes.AddRange(home, otherHome);

        var authority = new FundingAuthority
        {
            TenantId = tenant.Id,
            Code = "FA-TEST",
            Name = "Test Funder",
            Type = "Council",
            BillingFrequency = "Weekly",
            Email = "funder@test.local",
            IsActive = true
        };
        db.FundingAuthorities.Add(authority);

        var nominal = new NominalCode
        {
            TenantId = tenant.Id,
            Code = "4000",
            Name = "Care income",
            IsActive = true
        };
        db.NominalCodes.Add(nominal);
        await db.SaveChangesAsync();

        var category = await db.InvoiceCategories
            .FirstAsync(x => x.TenantId == tenant.Id && x.Code == "GENERAL_CARE");

        var template = new InvoiceTemplate
        {
            TenantId = tenant.Id,
            Name = "Integration default",
            InvoiceCategoryId = category.Id,
            HeaderText1 = "Invoice",
            FooterText = "Thank you",
            BankAccountName = "Test",
            SortCode = "00-00-00",
            AccountNumber = "12345678",
            ContactEmail = "finance@test.local",
            EmailSubjectTemplate = "Invoice {{InvoiceNumber}}",
            EmailBodyTemplate = "Attached",
            IsActive = true
        };
        db.InvoiceTemplates.Add(template);
        await db.SaveChangesAsync();

        var client = new Client
        {
            TenantId = tenant.Id,
            CareHomeId = home.Id,
            SageId = "SAGE-INT",
            ReferenceNumber = $"REF-{Guid.NewGuid():N}".Substring(0, 12),
            FirstName = "Integration",
            LastName = "Resident",
            CareType = "Residential",
            Status = "Current",
            AdmissionDate = new DateOnly(2026, 1, 1)
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        var contract = new ClientFundingContract
        {
            TenantId = tenant.Id,
            ClientId = client.Id,
            FundingAuthorityId = authority.Id,
            InvoiceCategoryId = category.Id,
            NominalCodeId = nominal.Id,
            InvoiceTemplateId = template.Id,
            ContractStartDate = new DateOnly(2026, 1, 1),
            Status = "Active",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.ClientFundingContracts.Add(contract);
        await db.SaveChangesAsync();

        db.FundingRates.Add(new FundingRate
        {
            ClientFundingContractId = contract.Id,
            EffectiveFrom = new DateOnly(2026, 1, 1),
            Frequency = "Weekly",
            Amount = 700m,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var periodStart = new DateOnly(2026, 5, 1);
        var periodEnd = new DateOnly(2026, 5, 7);

        return new BillingScenario(
            tenant,
            company,
            home,
            otherHome,
            authority,
            category,
            template,
            nominal,
            client,
            contract,
            periodStart,
            periodEnd);
    }
}
