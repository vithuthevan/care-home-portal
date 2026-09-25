using System.Net.Mail;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Email;
using CareHome.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Security;

public class TenantProvisioningService(
    CareHomeDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    TenantNominalCodeSeeder nominalCodeSeeder,
    IConfiguration configuration,
    ILogger<TenantProvisioningService> logger)
{
    public async Task<TenantProvisionResult> ProvisionAsync(
        TenantProvisionRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var tenant = new Tenant
        {
            PublicId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            TradingName = NullIfEmpty(request.TradingName),
            RegistrationNumber = NullIfEmpty(request.RegistrationNumber),
            Address = NullIfEmpty(request.Address),
            Phone = NullIfEmpty(request.Phone),
            Email = NullIfEmpty(request.Email),
            Website = NullIfEmpty(request.Website),
            IsActive = request.IsActive,
            CreatedAt = now,
            Settings = new TenantSettings
            {
                CurrencyCode = "GBP",
                CurrencySymbol = "£",
                TimeZoneId = "Europe/London",
                InvoicePrefix = "INV-",
                CreditNotePrefix = "CN-",
                NumberLength = 4,
                PaymentTermsDays = 30,
                BillingPeriodMode = BillingPeriodModes.Manual,
                AllowPrivatePayer = false,
                ShowGuardian = false,
                FinanceModuleEnabled = false
            }
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var category in DefaultInvoiceCategories.All)
        {
            dbContext.InvoiceCategories.Add(new InvoiceCategory
            {
                TenantId = tenant.Id,
                Code = category.Code,
                Name = category.Name,
                Description = category.Description,
                GroupingMode = DefaultInvoiceCategories.DefaultGroupingMode(category.Code),
                IsActive = true
            });
        }

        dbContext.DocumentSequences.Add(new DocumentSequence
        {
            TenantId = tenant.Id,
            DocumentType = DocumentTypes.Invoice,
            Prefix = tenant.Settings.InvoicePrefix,
            NumberLength = tenant.Settings.NumberLength,
            NextValue = 1
        });

        dbContext.DocumentSequences.Add(new DocumentSequence
        {
            TenantId = tenant.Id,
            DocumentType = DocumentTypes.CreditNote,
            Prefix = tenant.Settings.CreditNotePrefix,
            NumberLength = tenant.Settings.NumberLength,
            NextValue = 1
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        await nominalCodeSeeder.EnsureStarterCodesAsync(tenant.Id, cancellationToken);

        var generalCareId = await dbContext.InvoiceCategories
            .Where(x => x.TenantId == tenant.Id && x.Code == "GENERAL_CARE")
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);
        dbContext.InvoiceTemplates.Add(new InvoiceTemplate
        {
            TenantId = tenant.Id,
            Name = "Default General Care",
            InvoiceCategoryId = generalCareId,
            HeaderText1 = "Care Home Invoice",
            FooterText = "Thank you for your payment.",
            EmailSubjectTemplate = "Invoice {{InvoiceNumber}}",
            EmailBodyTemplate = "Please find invoice {{InvoiceNumber}} attached.",
            IsActive = true
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var result = new TenantProvisionResult { Tenant = tenant };
        if (!string.IsNullOrWhiteSpace(request.AdminEmail))
        {
            var adminEmail = request.AdminEmail.Trim();
            if (!IsValidEmail(adminEmail))
            {
                throw new InvalidOperationException("A valid administrator email is required.");
            }

            var temporaryPassword = TemporaryPasswordGenerator.Generate();
            var displayName = string.IsNullOrWhiteSpace(request.AdminDisplayName)
                ? request.Name.Trim() + " Admin"
                : request.AdminDisplayName.Trim();

            var user = new ApplicationUser
            {
                TenantId = tenant.Id,
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = displayName,
                IsActive = true,
                MustChangePassword = true
            };

            var created = await userManager.CreateAsync(user, temporaryPassword);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(" ", created.Errors.Select(e => e.Description)));
            }

            await userManager.AddToRoleAsync(user, AppRoles.TenantAdmin);

            var email = await emailSender.SendAsync(
                adminEmail,
                "Your Care Home Back Office login details",
                BuildWelcomeEmail(tenant.Name, displayName, adminEmail, temporaryPassword),
                null,
                null,
                cancellationToken);

            if (!email.Success)
            {
                throw new InvalidOperationException(
                    email.ErrorMessage ?? "Login details could not be emailed. Check SMTP configuration.");
            }

            result.CredentialsEmailed = true;
            result.CredentialsEmailSimulated = email.Simulated;
            if (email.Simulated)
            {
                result.TemporaryPassword = temporaryPassword;
                logger.LogWarning(
                    "Welcome email simulated for {Email}. Temporary password omitted from logs.",
                    adminEmail);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private string BuildWelcomeEmail(
        string organisationName,
        string displayName,
        string email,
        string temporaryPassword)
    {
        var signInUrl = configuration["App:PublicUrl"]?.Trim().TrimEnd('/');
        var signInLine = string.IsNullOrWhiteSpace(signInUrl)
            ? "Sign in to Care Home Back Office."
            : $"Sign in at: {signInUrl}/login";

        return
            $"Hello {displayName}," +
            $"{Environment.NewLine}{Environment.NewLine}" +
            $"An organisation account has been created for {organisationName}." +
            $"{Environment.NewLine}{Environment.NewLine}" +
            $"{signInLine}" +
            $"{Environment.NewLine}{Environment.NewLine}" +
            $"Email: {email}" +
            $"{Environment.NewLine}" +
            $"Temporary password: {temporaryPassword}" +
            $"{Environment.NewLine}{Environment.NewLine}" +
            "You must change this temporary password after you sign in. " +
            "You will not have access to the system until you set a new password." +
            $"{Environment.NewLine}{Environment.NewLine}" +
            "If you did not expect this email, contact your platform administrator.";
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return email.Contains('@', StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public class TenantProvisionResult
{
    public required Tenant Tenant { get; init; }

    public bool CredentialsEmailed { get; set; }

    public bool CredentialsEmailSimulated { get; set; }

    public string? TemporaryPassword { get; set; }
}

public class TenantProvisionRequest
{
    public string Name { get; set; } = string.Empty;

    public string? TradingName { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public bool IsActive { get; set; } = true;

    public string? AdminEmail { get; set; }

    public string? AdminDisplayName { get; set; }
}
