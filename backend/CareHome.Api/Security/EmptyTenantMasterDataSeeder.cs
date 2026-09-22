using CareHome.Api.Data;
using CareHome.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Security;

/// <summary>
/// Adds starter master data to active tenants that have invoice categories but no companies yet.
/// </summary>
public class EmptyTenantMasterDataSeeder(
    CareHomeDbContext dbContext,
    IConfiguration configuration,
    ILogger<EmptyTenantMasterDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Seed:MasterDataForEmptyTenants", false))
        {
            return;
        }

        var tenantIds = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            var hasCompanies = await dbContext.Companies
                .AnyAsync(c => c.TenantId == tenantId, cancellationToken);
            if (hasCompanies)
            {
                continue;
            }

            var hasCategories = await dbContext.InvoiceCategories
                .AnyAsync(c => c.TenantId == tenantId, cancellationToken);
            if (!hasCategories)
            {
                continue;
            }

            await SeedTenantAsync(tenantId, cancellationToken);
        }
    }

    private async Task SeedTenantAsync(int tenantId, CancellationToken cancellationToken)
    {
        var company = new Company
        {
            TenantId = tenantId,
            Name = "Demo Care Ltd",
            IsActive = true
        };
        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.CareHomes.Add(new CareHomeLocation
        {
            TenantId = tenantId,
            CompanyId = company.Id,
            Code = "RIVER01",
            Name = "River View House",
            BedCapacity = 24,
            IsActive = true
        });

        dbContext.FundingAuthorities.Add(new FundingAuthority
        {
            TenantId = tenantId,
            Code = "ANYTOWN",
            Name = "Anytown Council",
            Type = "Council",
            BillingFrequency = "Weekly",
            Email = "billing@anytown.example",
            IsActive = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var generalCare = await dbContext.InvoiceCategories
            .FirstAsync(x => x.TenantId == tenantId && x.Code == "GENERAL_CARE", cancellationToken);
        var misc = await dbContext.InvoiceCategories
            .FirstAsync(x => x.TenantId == tenantId && x.Code == "MISC", cancellationToken);

        var hasTemplates = await dbContext.InvoiceTemplates
            .AnyAsync(x => x.TenantId == tenantId, cancellationToken);
        if (!hasTemplates)
        {
            dbContext.InvoiceTemplates.Add(new InvoiceTemplate
            {
                TenantId = tenantId,
                Name = "Default General Care",
                InvoiceCategoryId = generalCare.Id,
                HeaderText1 = "Care Home Invoice",
                FooterText = "Thank you for your payment.",
                BankAccountName = "Example Account",
                SortCode = "00-00-00",
                AccountNumber = "00000000",
                ContactName = "Finance Team",
                ContactEmail = "finance@example.com",
                EmailSubjectTemplate = "Invoice {{InvoiceNumber}}",
                EmailBodyTemplate = "Please find invoice {{InvoiceNumber}} attached.",
                IsActive = true
            });

            dbContext.InvoiceTemplates.Add(new InvoiceTemplate
            {
                TenantId = tenantId,
                Name = "Default Miscellaneous",
                InvoiceCategoryId = misc.Id,
                HeaderText1 = "Miscellaneous Charges",
                ContactEmail = "finance@example.com",
                EmailSubjectTemplate = "Invoice {{InvoiceNumber}}",
                EmailBodyTemplate = "Please find invoice {{InvoiceNumber}} attached.",
                IsActive = true
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var tenantName = await dbContext.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.Name)
            .FirstAsync(cancellationToken);

        logger.LogInformation(
            "Seeded starter master data for tenant {TenantId} ({TenantName}).",
            tenantId,
            tenantName);
    }
}
