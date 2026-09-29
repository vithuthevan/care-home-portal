using CareHome.Api.Billing;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Billing;
using CareHome.Api.Dtos.CreditNotes;
using CareHome.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Security;

/// <summary>
/// Idempotent development demo dataset for end-to-end presentation (residents, billing, invoice, credit note).
/// </summary>
public class FinalDemoPresentationSeeder(
    CareHomeDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    BillingService billing,
    CreditNoteService creditNotes,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<FinalDemoPresentationSeeder> logger)
{
    private const string PrimaryTenantName = "Green Meadows Care Ltd";
    private const string LegacyTenantName = "Demo Care Group";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        if (!configuration.GetValue("Seed:EnsureDemoPresentation", true))
        {
            return;
        }

        var tenant = await dbContext.Tenants
            .FirstOrDefaultAsync(
                t => t.Name == PrimaryTenantName || t.Name == LegacyTenantName,
                cancellationToken);

        if (tenant is null)
        {
            return;
        }

        await EnsureDemoTenantAdminAsync(tenant, cancellationToken);

        var company = await dbContext.Companies
            .FirstOrDefaultAsync(c => c.TenantId == tenant.Id, cancellationToken);
        if (company is null)
        {
            return;
        }

        var careHome = await dbContext.CareHomes
            .FirstOrDefaultAsync(h => h.TenantId == tenant.Id, cancellationToken);
        if (careHome is null)
        {
            return;
        }

        var authority = await dbContext.FundingAuthorities
            .Where(x => x.TenantId == tenant.Id && x.IsActive)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (authority is null)
        {
            return;
        }

        var category = await dbContext.InvoiceCategories
            .FirstAsync(x => x.TenantId == tenant.Id && x.Code == "GENERAL_CARE", cancellationToken);

        var nominal = await dbContext.NominalCodes
            .Where(x => x.TenantId == tenant.Id && x.IsActive)
            .OrderBy(x => x.Code)
            .FirstAsync(cancellationToken);

        var template = await dbContext.InvoiceTemplates
            .Where(x => x.TenantId == tenant.Id && x.IsActive && x.InvoiceCategoryId == category.Id)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var admission = new DateOnly(2025, 1, 1);
        var periodStart = new DateOnly(2026, 4, 1);
        var periodEnd = new DateOnly(2026, 4, 30);

        var john = await EnsureResidentAsync(
            tenant.Id,
            careHome.Id,
            "REF-JCARTER",
            "SAGE-JC001",
            "John",
            "Carter",
            admission,
            cancellationToken);
        var mary = await EnsureResidentAsync(
            tenant.Id,
            careHome.Id,
            "REF-MWILSON",
            "SAGE-MW001",
            "Mary",
            "Wilson",
            admission,
            cancellationToken);
        var david = await EnsureResidentAsync(
            tenant.Id,
            careHome.Id,
            "REF-DBROWN",
            "SAGE-DB001",
            "David",
            "Brown",
            admission,
            cancellationToken);

        await EnsureContractAndRateAsync(
            tenant.Id,
            john.Id,
            authority.Id,
            category.Id,
            nominal.Id,
            template?.Id,
            admission,
            575m,
            now,
            cancellationToken);
        await EnsureContractAndRateAsync(
            tenant.Id,
            mary.Id,
            authority.Id,
            category.Id,
            nominal.Id,
            template?.Id,
            admission,
            600m,
            now,
            cancellationToken);
        await EnsureContractAndRateAsync(
            tenant.Id,
            david.Id,
            authority.Id,
            category.Id,
            nominal.Id,
            template?.Id,
            admission,
            550m,
            now,
            cancellationToken);

        await EnsureMiscChargeAsync(tenant.Id, mary, "Hairdressing appointment", 35m, periodStart.AddDays(10), nominal, now, cancellationToken);

        var hasJohnInvoice = await dbContext.InvoiceLines
            .AnyAsync(
                x => x.ClientId == john.Id && x.Invoice.PeriodStart == periodStart && x.Invoice.Status != InvoiceStatuses.Void,
                cancellationToken);
        if (!hasJohnInvoice)
        {
            var (generateResult, generateError) = await billing.GenerateAsync(
                tenant.Id,
                new BillingPreviewRequest
                {
                    CompanyId = company.Id,
                    CareHomeId = careHome.Id,
                    InvoiceCategoryId = category.Id,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    ClientIds = [john.Id]
                },
                cancellationToken);

            if (generateError is not null)
            {
                logger.LogWarning("Demo invoice for John Carter skipped: {Error}", generateError);
            }
            else
            {
                logger.LogInformation(
                    "Demo invoice generated for John Carter. InvoiceIds={Ids}",
                    string.Join(",", generateResult?.InvoiceIds ?? []));
            }
        }

        var davidInvoice = await dbContext.Invoices
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.Id && x.Lines.Any(l => l.ClientId == david.Id) && x.Status != InvoiceStatuses.Void)
            .OrderByDescending(x => x.InvoiceDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (davidInvoice is null)
        {
            var (davidGen, davidError) = await billing.GenerateAsync(
                tenant.Id,
                new BillingPreviewRequest
                {
                    CompanyId = company.Id,
                    CareHomeId = careHome.Id,
                    InvoiceCategoryId = category.Id,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    ClientIds = [david.Id]
                },
                cancellationToken);

            if (davidError is null && davidGen?.InvoiceIds.Count > 0)
            {
                davidInvoice = await dbContext.Invoices
                    .AsNoTracking()
                    .FirstAsync(x => x.Id == davidGen.InvoiceIds[0], cancellationToken);
            }
        }

        if (davidInvoice is not null)
        {
            var hasCredit = await dbContext.CreditNotes
                .AnyAsync(x => x.TenantId == tenant.Id && x.InvoiceId == davidInvoice.Id, cancellationToken);
            if (!hasCredit)
            {
                var line = await dbContext.InvoiceLines
                    .AsNoTracking()
                    .Where(x => x.InvoiceId == davidInvoice.Id)
                    .OrderBy(x => x.Id)
                    .FirstAsync(cancellationToken);

                var creditAmount = Math.Round(line.LineAmount * 0.5m, 2, MidpointRounding.AwayFromZero);
                var preview = await creditNotes.PreviewAsync(
                    tenant.Id,
                    new CreditNotePreviewRequest
                    {
                        InvoiceId = davidInvoice.Id,
                        PeriodStart = davidInvoice.PeriodStart,
                        PeriodEnd = davidInvoice.PeriodEnd,
                        CreditNoteDate = periodEnd.AddDays(2),
                        Reason = "Demo partial credit — service adjustment",
                        LineAmounts = new Dictionary<int, decimal> { [line.Id] = creditAmount }
                    },
                    cancellationToken);

                if (preview.CanGenerate)
                {
                    var (note, creditError) = await creditNotes.GenerateAsync(
                        tenant.Id,
                        new CreditNotePreviewRequest
                        {
                            InvoiceId = davidInvoice.Id,
                            PeriodStart = davidInvoice.PeriodStart,
                            PeriodEnd = davidInvoice.PeriodEnd,
                            CreditNoteDate = periodEnd.AddDays(2),
                            Reason = "Demo partial credit — service adjustment",
                            LineAmounts = new Dictionary<int, decimal> { [line.Id] = creditAmount }
                        },
                        cancellationToken);

                    if (creditError is not null)
                    {
                        logger.LogWarning("Demo credit note skipped: {Error}", creditError);
                    }
                    else if (note is not null)
                    {
                        logger.LogInformation("Demo credit note {Number} created for David Brown.", note.CreditNoteNumber);
                    }
                }
            }
        }
    }

    private async Task EnsureDemoTenantAdminAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        var email = configuration["Seed:DemoTenantAdminEmail"]?.Trim();
        var password = configuration["Seed:DemoTenantAdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var displayName = configuration["Seed:DemoTenantAdminDisplayName"]?.Trim() ?? "Vithursan";
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                TenantId = tenant.Id,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                IsActive = true,
                MustChangePassword = false
            };

            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                logger.LogWarning(
                    "Demo tenant admin not created: {Errors}",
                    string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(user, AppRoles.TenantAdmin);
            logger.LogInformation("Demo tenant admin {Email} created for {Tenant}.", email, tenant.Name);
            return;
        }

        if (user.TenantId != tenant.Id)
        {
            return;
        }

        if (!await userManager.IsInRoleAsync(user, AppRoles.TenantAdmin))
        {
            await userManager.AddToRoleAsync(user, AppRoles.TenantAdmin);
        }

        user.DisplayName = displayName;
        user.MustChangePassword = false;
        user.IsActive = true;
        await userManager.UpdateAsync(user);

        if (await userManager.CheckPasswordAsync(user, password))
        {
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(user, token, password);
        if (!reset.Succeeded)
        {
            logger.LogWarning(
                "Demo tenant admin password not reset: {Errors}",
                string.Join("; ", reset.Errors.Select(e => e.Description)));
        }
    }

    private async Task<Client> EnsureResidentAsync(
        int tenantId,
        int careHomeId,
        string reference,
        string sageId,
        string firstName,
        string lastName,
        DateOnly admission,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Clients
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.ReferenceNumber == reference,
                cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var client = new Client
        {
            TenantId = tenantId,
            CareHomeId = careHomeId,
            ReferenceNumber = reference,
            SageId = sageId,
            FirstName = firstName,
            LastName = lastName,
            CareType = "Residential",
            Status = "Current",
            AdmissionDate = admission
        };
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync(cancellationToken);
        return client;
    }

    private async Task EnsureContractAndRateAsync(
        int tenantId,
        int clientId,
        int authorityId,
        int categoryId,
        int nominalId,
        int? templateId,
        DateOnly start,
        decimal weeklyRate,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var contract = await dbContext.ClientFundingContracts
            .Include(x => x.Rates)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ClientId == clientId, cancellationToken);

        if (contract is null)
        {
            contract = new ClientFundingContract
            {
                TenantId = tenantId,
                ClientId = clientId,
                FundingAuthorityId = authorityId,
                InvoiceCategoryId = categoryId,
                NominalCodeId = nominalId,
                InvoiceTemplateId = templateId,
                ContractStartDate = start,
                ContractEndDate = null,
                Status = "Active",
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.ClientFundingContracts.Add(contract);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!contract.Rates.Any())
        {
            dbContext.FundingRates.Add(new FundingRate
            {
                ClientFundingContractId = contract.Id,
                EffectiveFrom = start,
                Frequency = "Weekly",
                Amount = weeklyRate,
                CreatedAt = now
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureMiscChargeAsync(
        int tenantId,
        Client client,
        string description,
        decimal amount,
        DateOnly usedDate,
        NominalCode nominal,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.MiscCharges
            .AnyAsync(
                x => x.TenantId == tenantId && x.ClientId == client.Id && x.Description == description && !x.IsInvoiced,
                cancellationToken);
        if (exists)
        {
            return;
        }

        var batch = new MiscChargeImportBatch
        {
            TenantId = tenantId,
            FileName = "demo-misc-charges.csv",
            ImportedAt = now,
            TotalRows = 1,
            AcceptedRows = 1,
            RejectedRows = 0,
            Status = "Committed"
        };
        dbContext.MiscChargeImportBatches.Add(batch);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.MiscCharges.Add(new MiscCharge
        {
            TenantId = tenantId,
            ImportBatchId = batch.Id,
            ClientId = client.Id,
            ClientReference = client.ReferenceNumber,
            UsedDate = usedDate,
            Description = description,
            Amount = amount,
            NominalCodeId = nominal.Id,
            NominalCodeValue = nominal.Code,
            SourceRowNumber = 1,
            IsInvoiced = false,
            CreatedAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
