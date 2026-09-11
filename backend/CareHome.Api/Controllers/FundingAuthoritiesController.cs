using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.FundingAuthorities;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [Route("api/funding-authorities")]
    [RequireTenant]
    public class FundingAuthoritiesController(
        CareHomeDbContext dbContext,
        ITenantContext tenantContext,
        AuditService audit) : ControllerBase
    {
        private static readonly string[] AllowedTypes =
        [
            "NHS",
            "Council",
            "Private",
            "Other"
        ];

        private static readonly string[] AllowedBillingFrequencies =
        [
            "Daily",
            "Weekly",
            "Monthly",
            "AdHoc",
            "CustomDays"
        ];

        [HttpGet]
        public async Task<ActionResult<List<FundingAuthorityDto>>> GetFundingAuthorities(
            bool activeOnly = false)
        {
            var query = dbContext.FundingAuthorities.AsNoTracking()
                .ForTenant(tenantContext.TenantId);

            if (activeOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var authorities = await query
                .OrderBy(x => x.Name)
                .Select(x => new FundingAuthorityDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Type = x.Type,
                    ContactName = x.ContactName,
                    Phone = x.Phone,
                    Email = x.Email,
                    Address = x.Address,
                    BillingFrequency = x.BillingFrequency,
                    BillingIntervalDays = x.BillingIntervalDays,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            return Ok(authorities);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<FundingAuthorityDto>> GetFundingAuthority(int id)
        {
            var authority = await dbContext.FundingAuthorities
                .AsNoTracking()
                .Where(x => x.Id == id && x.TenantId == tenantContext.TenantId)
                .Select(x => new FundingAuthorityDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Type = x.Type,
                    ContactName = x.ContactName,
                    Phone = x.Phone,
                    Email = x.Email,
                    Address = x.Address,
                    BillingFrequency = x.BillingFrequency,
                    BillingIntervalDays = x.BillingIntervalDays,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();

            if (authority is null)
            {
                return NotFound();
            }

            return Ok(authority);
        }

        [HttpPost]
        public async Task<ActionResult<FundingAuthorityDto>> CreateFundingAuthority(
            CreateFundingAuthorityRequest request)
        {
            var code = request.Code.Trim();

            var duplicateCode = await dbContext.FundingAuthorities
                .AnyAsync(x => x.TenantId == tenantContext.TenantId && x.Code == code);

            if (duplicateCode)
            {
                return BadRequest(new
                {
                    message = "Funding authority code already exists."
                });
            }

            var typeError = ValidateType(request.Type);
            if (typeError is not null)
            {
                return typeError;
            }

            var billingError = ValidateBilling(
                request.BillingFrequency,
                request.BillingIntervalDays);

            if (billingError is not null)
            {
                return billingError;
            }

            var billingFrequency = request.BillingFrequency.Trim();
            var billingIntervalDays =
                billingFrequency == "CustomDays"
                    ? request.BillingIntervalDays
                    : null;

            var authority = new FundingAuthority
            {
                TenantId = tenantContext.TenantId,
                Code = code,
                Name = request.Name.Trim(),
                Type = request.Type.Trim(),
                ContactName = request.ContactName?.Trim(),
                Phone = request.Phone?.Trim(),
                Email = request.Email?.Trim(),
                Address = request.Address?.Trim(),
                BillingFrequency = billingFrequency,
                BillingIntervalDays = billingIntervalDays,
                IsActive = true
            };

            dbContext.FundingAuthorities.Add(authority);

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("FundingAuthority", authority.Id.ToString(), "Create", null, new { authority.Code, authority.Name }, "Created funding authority.");

            return CreatedAtAction(
                nameof(GetFundingAuthority),
                new { id = authority.Id },
                MapToDto(authority));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<FundingAuthorityDto>> UpdateFundingAuthority(
            int id,
            UpdateFundingAuthorityRequest request)
        {
            var authority = await dbContext.FundingAuthorities
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

            if (authority is null)
            {
                return NotFound();
            }

            var code = request.Code.Trim();

            var duplicateCode = await dbContext.FundingAuthorities
                .AnyAsync(x =>
                    x.TenantId == tenantContext.TenantId &&
                    x.Id != id &&
                    x.Code == code);

            if (duplicateCode)
            {
                return BadRequest(new
                {
                    message = "Funding authority code already exists."
                });
            }

            var typeError = ValidateType(request.Type);
            if (typeError is not null)
            {
                return typeError;
            }

            var billingError = ValidateBilling(
                request.BillingFrequency,
                request.BillingIntervalDays);

            if (billingError is not null)
            {
                return billingError;
            }

            var billingFrequency = request.BillingFrequency.Trim();

            authority.Code = code;
            authority.Name = request.Name.Trim();
            authority.Type = request.Type.Trim();
            authority.ContactName = request.ContactName?.Trim();
            authority.Phone = request.Phone?.Trim();
            authority.Email = request.Email?.Trim();
            authority.Address = request.Address?.Trim();
            authority.BillingFrequency = billingFrequency;
            authority.BillingIntervalDays =
                billingFrequency == "CustomDays"
                    ? request.BillingIntervalDays
                    : null;
            authority.IsActive = request.IsActive;

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("FundingAuthority", authority.Id.ToString(), "Update", null, request, "Updated funding authority.");

            return Ok(MapToDto(authority));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeactivateFundingAuthority(int id)
        {
            var authority = await dbContext.FundingAuthorities
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

            if (authority is null)
            {
                return NotFound();
            }

            authority.IsActive = false;

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("FundingAuthority", id.ToString(), "Deactivate", null, null, "Deactivated funding authority.");

            return NoContent();
        }

        private ActionResult? ValidateType(string type)
        {
            if (!AllowedTypes.Contains(type.Trim()))
            {
                return BadRequest(new
                {
                    message =
                        "Funding authority type must be NHS, Council, Private, or Other."
                });
            }

            return null;
        }

        private ActionResult? ValidateBilling(
            string billingFrequency,
            int? billingIntervalDays)
        {
            var frequency = billingFrequency.Trim();

            if (!AllowedBillingFrequencies.Contains(frequency))
            {
                return BadRequest(new
                {
                    message =
                        "Billing frequency must be Daily, Weekly, Monthly, AdHoc, or CustomDays."
                });
            }

            if (frequency == "CustomDays")
            {
                if (billingIntervalDays is null || billingIntervalDays <= 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "Custom billing frequency requires a billing interval greater than 0 days."
                    });
                }
            }

            return null;
        }

        private static FundingAuthorityDto MapToDto(FundingAuthority authority)
        {
            return new FundingAuthorityDto
            {
                Id = authority.Id,
                Code = authority.Code,
                Name = authority.Name,
                Type = authority.Type,
                ContactName = authority.ContactName,
                Phone = authority.Phone,
                Email = authority.Email,
                Address = authority.Address,
                BillingFrequency = authority.BillingFrequency,
                BillingIntervalDays = authority.BillingIntervalDays,
                IsActive = authority.IsActive
            };
        }
    }
}
