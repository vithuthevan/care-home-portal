using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Companies;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RequireTenant]
    public class CompaniesController(
        CareHomeDbContext dbContext,
        ITenantContext tenantContext,
        AuditService audit) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<CompanyDto>>> GetCompanies()
        {
            var tenantId = tenantContext.TenantId;
            var companies = await dbContext.Companies
                .AsNoTracking()
                .ForTenant(tenantId)
                .OrderBy(company => company.Name)
                .Select(company => new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    IsActive = company.IsActive
                })
                .ToListAsync();

            return Ok(companies);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CompanyDto>> GetCompany(int id)
        {
            var tenantId = tenantContext.TenantId;
            var company = await dbContext.Companies
                .AsNoTracking()
                .Where(company => company.Id == id && company.TenantId == tenantId)
                .Select(company => new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    IsActive = company.IsActive
                })
                .FirstOrDefaultAsync();

            if (company is null)
            {
                return NotFound();
            }

            return Ok(company);
        }

        [HttpPost]
        public async Task<ActionResult<CompanyDto>> CreateCompany(
            CreateCompanyRequest request)
        {
            var tenantId = tenantContext.TenantId;
            var companyName = request.Name.Trim();

            var companyExists = await dbContext.Companies
                .AnyAsync(company =>
                    company.TenantId == tenantId &&
                    company.Name == companyName);

            if (companyExists)
            {
                return BadRequest(new
                {
                    message = "A company with this name already exists."
                });
            }

            var company = new Company
            {
                TenantId = tenantId,
                Name = companyName,
                IsActive = true
            };

            dbContext.Companies.Add(company);

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("Company", company.Id.ToString(), "Create", null, new { company.Name }, "Created company.");

            return CreatedAtAction(
                nameof(GetCompany),
                new { id = company.Id },
                ToDto(company));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CompanyDto>> UpdateCompany(
            int id,
            UpdateCompanyRequest request)
        {
            var tenantId = tenantContext.TenantId;
            var company = await dbContext.Companies
                .FirstOrDefaultAsync(company => company.Id == id && company.TenantId == tenantId);

            if (company is null)
            {
                return NotFound();
            }

            var companyName = request.Name.Trim();

            var duplicateExists = await dbContext.Companies
                .AnyAsync(existingCompany =>
                    existingCompany.TenantId == tenantId &&
                    existingCompany.Id != id &&
                    existingCompany.Name == companyName);

            if (duplicateExists)
            {
                return BadRequest(new
                {
                    message = "A company with this name already exists."
                });
            }

            if (company.IsActive && !request.IsActive)
            {
                var deactivationError =
                    await RejectIfDeactivatingWithActiveCareHomes(id);

                if (deactivationError is not null)
                {
                    return deactivationError;
                }
            }

            company.Name = companyName;
            company.IsActive = request.IsActive;

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("Company", company.Id.ToString(), "Update", null, request, "Updated company.");

            return Ok(ToDto(company));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeactivateCompany(int id)
        {
            var tenantId = tenantContext.TenantId;
            var company = await dbContext.Companies
                .FirstOrDefaultAsync(company => company.Id == id && company.TenantId == tenantId);

            if (company is null)
            {
                return NotFound();
            }

            if (company.IsActive)
            {
                var deactivationError =
                    await RejectIfDeactivatingWithActiveCareHomes(id);

                if (deactivationError is not null)
                {
                    return deactivationError;
                }
            }

            company.IsActive = false;

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("Company", id.ToString(), "Deactivate", null, null, "Deactivated company.");

            return NoContent();
        }

        private async Task<ActionResult?> RejectIfDeactivatingWithActiveCareHomes(
            int companyId)
        {
            var hasActiveCareHomes =
                await dbContext.CareHomes.AnyAsync(x =>
                    x.CompanyId == companyId &&
                    x.IsActive);

            if (hasActiveCareHomes)
            {
                return BadRequest(new
                {
                    message =
                        "Deactivate all care homes under this company before deactivating the company."
                });
            }

            return null;
        }

        private static CompanyDto ToDto(Company company)
        {
            return new CompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                IsActive = company.IsActive
            };
        }
    }
}
