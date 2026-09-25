using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Security;

public class IdentitySeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<IdentitySeeder> logger)
{
    public async Task SeedAsync()
    {
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (!environment.IsDevelopment()
            && !string.IsNullOrWhiteSpace(email)
            && !string.IsNullOrWhiteSpace(password)
            && KnownDevelopmentCredentials.IsForbiddenProductionBootstrap(email, password))
        {
            throw new InvalidOperationException(
                "The Development platform admin credentials cannot be used outside Development. Set Seed__AdminEmail and Seed__AdminPassword to unique bootstrap values, or leave them empty and create the first PlatformAdmin manually.");
        }

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (!await roleManager.RoleExistsAsync(AppRoles.SuperAdmin))
        {
            await roleManager.CreateAsync(new IdentityRole(AppRoles.SuperAdmin));
        }

        var superAdmins = await userManager.GetUsersInRoleAsync(AppRoles.SuperAdmin);
        foreach (var superAdmin in superAdmins)
        {
            if (!await userManager.IsInRoleAsync(superAdmin, AppRoles.PlatformAdmin))
            {
                await userManager.AddToRoleAsync(superAdmin, AppRoles.PlatformAdmin);
            }

            if (superAdmin.TenantId is not null)
            {
                superAdmin.TenantId = null;
                await userManager.UpdateAsync(superAdmin);
            }
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Platform admin seed skipped because Seed:AdminEmail or Seed:AdminPassword is not set.");
            return;
        }

        if (!environment.IsDevelopment()
            && KnownDevelopmentCredentials.IsForbiddenProductionBootstrap(email, password))
        {
            throw new InvalidOperationException(
                "The Development platform admin credentials cannot be used outside Development. Set Seed__AdminEmail and Seed__AdminPassword to unique bootstrap values, or leave them empty and create the first PlatformAdmin manually.");
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, AppRoles.PlatformAdmin))
            {
                await userManager.AddToRoleAsync(existing, AppRoles.PlatformAdmin);
            }

            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Platform Administrator",
            IsActive = true,
            TenantId = null
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogWarning("Platform admin seed failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, AppRoles.PlatformAdmin);
        logger.LogInformation("PlatformAdmin user seeded for {Email}", email);
    }
}

public class DevelopmentMasterDataSeeder(
    CareHomeDbContext dbContext,
    TenantProvisioningService provisioning,
    IHostEnvironment environment,
    ILogger<DevelopmentMasterDataSeeder> logger)
{
    public async Task SeedAsync()
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        if (await dbContext.Tenants.AnyAsync())
        {
            return;
        }

        var provisioned = await provisioning.ProvisionAsync(new TenantProvisionRequest
        {
            Name = "Demo Care Group",
            TradingName = "Demo Care Group",
            IsActive = true
        });
        var tenant = provisioned.Tenant;

        var company = new Company
        {
            TenantId = tenant.Id,
            Name = "Demo Care Ltd",
            IsActive = true
        };
        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync();

        dbContext.CareHomes.Add(new CareHomeLocation
        {
            TenantId = tenant.Id,
            CompanyId = company.Id,
            Code = "SUNRISE",
            Name = "Sunrise House",
            BedCapacity = 20,
            IsActive = true
        });

        dbContext.FundingAuthorities.AddRange(
            new FundingAuthority
            {
                TenantId = tenant.Id,
                Code = "DEV-NHS",
                Name = "Development NHS Example",
                Type = "NHS",
                BillingFrequency = "Monthly",
                Email = "nhs-dev@localhost",
                IsActive = true
            },
            new FundingAuthority
            {
                TenantId = tenant.Id,
                Code = "DEV-COUNCIL",
                Name = "Development Council Example",
                Type = "Council",
                BillingFrequency = "Weekly",
                Email = "council-dev@localhost",
                IsActive = true
            },
            new FundingAuthority
            {
                TenantId = tenant.Id,
                Code = "DEV-PRIVATE",
                Name = "Development Private Example",
                Type = "Private",
                BillingFrequency = "Monthly",
                Email = "private-dev@localhost",
                IsActive = true
            });

        await dbContext.SaveChangesAsync();

        var generalCare = await dbContext.InvoiceCategories
            .FirstAsync(x => x.TenantId == tenant.Id && x.Code == "GENERAL_CARE");
        var misc = await dbContext.InvoiceCategories
            .FirstAsync(x => x.TenantId == tenant.Id && x.Code == "MISC");

        dbContext.InvoiceTemplates.Add(new InvoiceTemplate
        {
            TenantId = tenant.Id,
            Name = "Default General Care",
            InvoiceCategoryId = generalCare.Id,
            HeaderText1 = "Care Home Invoice",
            FooterText = "Thank you for your payment.",
            BankAccountName = "Example Account",
            SortCode = "00-00-00",
            AccountNumber = "00000000",
            ContactName = "Finance Team",
            ContactEmail = "finance@localhost",
            EmailSubjectTemplate = "Invoice {{InvoiceNumber}}",
            EmailBodyTemplate = "Please find invoice {{InvoiceNumber}} attached.",
            IsActive = true
        });

        dbContext.InvoiceTemplates.Add(new InvoiceTemplate
        {
            TenantId = tenant.Id,
            Name = "Default Miscellaneous",
            InvoiceCategoryId = misc.Id,
            HeaderText1 = "Miscellaneous Charges",
            ContactEmail = "finance@localhost",
            EmailSubjectTemplate = "Invoice {{InvoiceNumber}}",
            EmailBodyTemplate = "Please find invoice {{InvoiceNumber}} attached.",
            IsActive = true
        });

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Development demo organisation '{Name}' seeded.", tenant.Name);
    }
}

