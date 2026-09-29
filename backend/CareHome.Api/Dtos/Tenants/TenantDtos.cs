using System.ComponentModel.DataAnnotations;
using CareHome.Api.Common;

namespace CareHome.Api.Dtos.Tenants;

public class TenantDto
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? TradingName { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public class CreateTenantResponse : TenantDto
{
    public bool CredentialsEmailed { get; set; }

    public bool CredentialsEmailSimulated { get; set; }

    public string? TemporaryPassword { get; set; }
}

public class CreateTenantRequest
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

    public string? AdminPassword { get; set; }

    public string? AdminDisplayName { get; set; }
}

public class UpdateTenantRequest
{
    public string Name { get; set; } = string.Empty;

    public string? TradingName { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public bool IsActive { get; set; } = true;
}

public class OrganisationSettingsDto
{
    public int TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? TradingName { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public string CurrencyCode { get; set; } = "GBP";

    public string CurrencySymbol { get; set; } = "£";

    public string TimeZoneId { get; set; } = "Europe/London";

    public string InvoicePrefix { get; set; } = "INV-";

    public string CreditNotePrefix { get; set; } = "CN-";

    public int NumberLength { get; set; } = 4;

    public int PaymentTermsDays { get; set; } = 30;

    public string? EmailFromName { get; set; }

    public string? EmailFromAddress { get; set; }

    public string? PrimaryColour { get; set; }

    public string BillingPeriodMode { get; set; } = BillingPeriodModes.Manual;

    public bool AllowPrivatePayer { get; set; }

    public bool ShowGuardian { get; set; }

    public bool FinanceModuleEnabled { get; set; }

    public bool FinanceModuleAvailable { get; set; }
}

public class UpdateOrganisationSettingsRequest
{
    public string Name { get; set; } = string.Empty;

    public string? TradingName { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    [OptionalEmailAddress]
    public string? Email { get; set; }

    public string? Website { get; set; }

    public string CurrencyCode { get; set; } = "GBP";

    public string CurrencySymbol { get; set; } = "£";

    public string TimeZoneId { get; set; } = "Europe/London";

    public string InvoicePrefix { get; set; } = "INV-";

    public string CreditNotePrefix { get; set; } = "CN-";

    public int NumberLength { get; set; } = 4;

    public int PaymentTermsDays { get; set; } = 30;

    public string? EmailFromName { get; set; }

    [OptionalEmailAddress]
    public string? EmailFromAddress { get; set; }

    public string? PrimaryColour { get; set; }

    public string BillingPeriodMode { get; set; } = BillingPeriodModes.Manual;

    public bool AllowPrivatePayer { get; set; }

    public bool ShowGuardian { get; set; }

    public bool FinanceModuleEnabled { get; set; }
}

