using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Tenants;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [Route("api/platform/tenants")]
    [Authorize(Roles = $"{AppRoles.PlatformAdmin},{AppRoles.SuperAdmin}")]
    public class PlatformTenantsController(
        CareHomeDbContext dbContext,
        TenantProvisioningService provisioning,
        AuditService audit,
        IHostEnvironment environment) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<TenantDto>>> List()
        {
            var tenants = await dbContext.Tenants.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new TenantDto
                {
                    Id = x.Id,
                    PublicId = x.PublicId,
                    Name = x.Name,
                    TradingName = x.TradingName,
                    RegistrationNumber = x.RegistrationNumber,
                    Address = x.Address,
                    Phone = x.Phone,
                    Email = x.Email,
                    Website = x.Website,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return Ok(tenants);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TenantDto>> Get(int id)
        {
            var tenant = await dbContext.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return tenant is null ? NotFound() : Ok(ToDto(tenant));
        }

        [HttpPost]
        public async Task<ActionResult<CreateTenantResponse>> Create(CreateTenantRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Organisation name is required." });
            }

            if (string.IsNullOrWhiteSpace(request.AdminEmail))
            {
                return BadRequest(new { message = "Administrator email is required so login details can be sent." });
            }

            try
            {
                var provisioned = await provisioning.ProvisionAsync(new TenantProvisionRequest
                {
                    Name = request.Name,
                    TradingName = request.TradingName,
                    RegistrationNumber = request.RegistrationNumber,
                    Address = request.Address,
                    Phone = request.Phone,
                    Email = request.Email,
                    Website = request.Website,
                    IsActive = request.IsActive,
                    AdminEmail = request.AdminEmail,
                    AdminDisplayName = request.AdminDisplayName
                });

                var tenant = provisioned.Tenant;
                await audit.LogAsync(
                    "Tenant",
                    tenant.Id.ToString(),
                    "Create",
                    null,
                    new { tenant.Name },
                    "Created organisation.",
                    tenantId: tenant.Id);

                return CreatedAtAction(nameof(Get), new { id = tenant.Id }, ToCreateResponse(provisioned));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<TenantDto>> Update(int id, UpdateTenantRequest request)
        {
            var tenant = await dbContext.Tenants.FirstOrDefaultAsync(x => x.Id == id);
            if (tenant is null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Organisation name is required." });
            }

            tenant.Name = request.Name.Trim();
            tenant.TradingName = NullIfEmpty(request.TradingName);
            tenant.RegistrationNumber = NullIfEmpty(request.RegistrationNumber);
            tenant.Address = NullIfEmpty(request.Address);
            tenant.Phone = NullIfEmpty(request.Phone);
            tenant.Email = NullIfEmpty(request.Email);
            tenant.Website = NullIfEmpty(request.Website);
            tenant.IsActive = request.IsActive;
            tenant.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync();
            await audit.LogAsync(
                "Tenant",
                tenant.Id.ToString(),
                "Update",
                null,
                request,
                "Updated organisation.",
                tenantId: tenant.Id);

            return Ok(ToDto(tenant));
        }

        private static TenantDto ToDto(Tenant tenant)
        {
            return new TenantDto
            {
                Id = tenant.Id,
                PublicId = tenant.PublicId,
                Name = tenant.Name,
                TradingName = tenant.TradingName,
                RegistrationNumber = tenant.RegistrationNumber,
                Address = tenant.Address,
                Phone = tenant.Phone,
                Email = tenant.Email,
                Website = tenant.Website,
                IsActive = tenant.IsActive,
                CreatedAt = tenant.CreatedAt
            };
        }

        private CreateTenantResponse ToCreateResponse(TenantProvisionResult provisioned)
        {
            var tenant = provisioned.Tenant;
            return new CreateTenantResponse
            {
                Id = tenant.Id,
                PublicId = tenant.PublicId,
                Name = tenant.Name,
                TradingName = tenant.TradingName,
                RegistrationNumber = tenant.RegistrationNumber,
                Address = tenant.Address,
                Phone = tenant.Phone,
                Email = tenant.Email,
                Website = tenant.Website,
                IsActive = tenant.IsActive,
                CreatedAt = tenant.CreatedAt,
                CredentialsEmailed = provisioned.CredentialsEmailed,
                CredentialsEmailSimulated = provisioned.CredentialsEmailSimulated,
                // Never return temporary passwords outside Development.
                TemporaryPassword = environment.IsDevelopment()
                    ? provisioned.TemporaryPassword
                    : null
            };
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}

