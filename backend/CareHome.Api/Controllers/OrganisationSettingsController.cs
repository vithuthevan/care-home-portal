using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Tenants;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [Route("api/settings/organisation")]
    [RequireTenant]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.Administrator}")]
    public class OrganisationSettingsController(
        CareHomeDbContext dbContext,
        ITenantContext tenantContext,
        AuditService audit) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<OrganisationSettingsDto>> Get()
        {
            var tenantId = tenantContext.TenantId;
            var tenant = await dbContext.Tenants
                .Include(x => x.Settings)
                .FirstOrDefaultAsync(x => x.Id == tenantId);

            if (tenant is null)
            {
                return NotFound();
            }

            return Ok(ToDto(tenant));
        }

        [HttpPut]
        public async Task<ActionResult<OrganisationSettingsDto>> Update(UpdateOrganisationSettingsRequest request)
        {
            var tenantId = tenantContext.TenantId;
            var tenant = await dbContext.Tenants
                .Include(x => x.Settings)
                .FirstOrDefaultAsync(x => x.Id == tenantId);

            if (tenant is null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Organisation name is required." });
            }

            if (request.PaymentTermsDays < 0 || request.PaymentTermsDays > 365)
            {
                return BadRequest(new { message = "Payment terms must be between 0 and 365 days." });
            }

            if (request.NumberLength < 1 || request.NumberLength > 10)
            {
                return BadRequest(new { message = "Number length must be between 1 and 10." });
            }

            tenant.Name = request.Name.Trim();
            tenant.TradingName = NullIfEmpty(request.TradingName);
            tenant.RegistrationNumber = NullIfEmpty(request.RegistrationNumber);
            tenant.Address = NullIfEmpty(request.Address);
            tenant.Phone = NullIfEmpty(request.Phone);
            tenant.Email = NullIfEmpty(request.Email);
            tenant.Website = NullIfEmpty(request.Website);
            tenant.UpdatedAt = DateTimeOffset.UtcNow;

            tenant.Settings.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode)
                ? "GBP"
                : request.CurrencyCode.Trim().ToUpperInvariant();
            tenant.Settings.CurrencySymbol = string.IsNullOrWhiteSpace(request.CurrencySymbol)
                ? "£"
                : request.CurrencySymbol.Trim();
            tenant.Settings.TimeZoneId = string.IsNullOrWhiteSpace(request.TimeZoneId)
                ? "Europe/London"
                : request.TimeZoneId.Trim();
            tenant.Settings.InvoicePrefix = request.InvoicePrefix?.Trim() ?? "INV-";
            tenant.Settings.CreditNotePrefix = request.CreditNotePrefix?.Trim() ?? "CN-";
            tenant.Settings.NumberLength = request.NumberLength;
            tenant.Settings.PaymentTermsDays = request.PaymentTermsDays;
            tenant.Settings.EmailFromName = NullIfEmpty(request.EmailFromName);
            tenant.Settings.EmailFromAddress = NullIfEmpty(request.EmailFromAddress);
            tenant.Settings.PrimaryColour = NullIfEmpty(request.PrimaryColour);

            var invoiceSequence = await dbContext.DocumentSequences
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.DocumentType == DocumentTypes.Invoice);
            if (invoiceSequence is not null)
            {
                invoiceSequence.Prefix = tenant.Settings.InvoicePrefix;
                invoiceSequence.NumberLength = tenant.Settings.NumberLength;
            }

            var creditSequence = await dbContext.DocumentSequences
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.DocumentType == DocumentTypes.CreditNote);
            if (creditSequence is not null)
            {
                creditSequence.Prefix = tenant.Settings.CreditNotePrefix;
                creditSequence.NumberLength = tenant.Settings.NumberLength;
            }

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("OrganisationSettings", tenantId.ToString(), "Update", null, request, "Updated organisation settings.");
            return Ok(ToDto(tenant));
        }

        private static OrganisationSettingsDto ToDto(Models.Tenant tenant)
        {
            return new OrganisationSettingsDto
            {
                TenantId = tenant.Id,
                Name = tenant.Name,
                TradingName = tenant.TradingName,
                RegistrationNumber = tenant.RegistrationNumber,
                Address = tenant.Address,
                Phone = tenant.Phone,
                Email = tenant.Email,
                Website = tenant.Website,
                CurrencyCode = tenant.Settings.CurrencyCode,
                CurrencySymbol = tenant.Settings.CurrencySymbol,
                TimeZoneId = tenant.Settings.TimeZoneId,
                InvoicePrefix = tenant.Settings.InvoicePrefix,
                CreditNotePrefix = tenant.Settings.CreditNotePrefix,
                NumberLength = tenant.Settings.NumberLength,
                PaymentTermsDays = tenant.Settings.PaymentTermsDays,
                EmailFromName = tenant.Settings.EmailFromName,
                EmailFromAddress = tenant.Settings.EmailFromAddress,
                PrimaryColour = tenant.Settings.PrimaryColour
            };
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}

