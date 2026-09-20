using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.CareHomes;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [Route("api/care-homes")]
    [RequireTenant]
    public class CareHomesController(
        CareHomeDbContext dbContext,
        ITenantContext tenantContext,
        UserAccessService userAccess,
        AuditService audit) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult> GetCareHomes(
            int? companyId = null,
            int? page = null,
            int? pageSize = null)
        {
            var homes = await userAccess.GetScopedCareHomeIdsAsync(tenantContext.TenantId);
            var baseQuery = dbContext.CareHomes.AsNoTracking()
                .Where(x => x.TenantId == tenantContext.TenantId && homes.Contains(x.Id));

            if (companyId.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.CompanyId == companyId.Value);
            }

            var query = ProjectToDto(baseQuery).OrderBy(x => x.Name);

            if (!Pagination.IsRequested(page, pageSize))
            {
                return Ok(await query.ToListAsync());
            }

            var (p, ps) = Pagination.Normalize(page, pageSize);
            var total = await query.CountAsync();
            var items = await query.Skip((p - 1) * ps).Take(ps).ToListAsync();
            return Ok(new PagedResult<CareHomeDto>
            {
                Items = items,
                TotalCount = total,
                Page = p,
                PageSize = ps
            });
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CareHomeDto>> GetCareHome(int id)
        {
            var careHome = await ProjectToDto(
                    dbContext.CareHomes.AsNoTracking()
                        .Where(x => x.TenantId == tenantContext.TenantId))
                .FirstOrDefaultAsync(x => x.Id == id);

            if (careHome is null)
            {
                return NotFound();
            }

            if (!await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, id))
            {
                return NotFound();
            }

            return Ok(careHome);
        }

        [HttpPost]
        public async Task<ActionResult<CareHomeDto>> CreateCareHome(
            CreateCareHomeRequest request)
        {
            var companyError =
                await ValidateSelectedCompany(request.CompanyId);

            if (companyError is not null)
            {
                return companyError;
            }

            var code = request.Code.Trim();

            var duplicateCode = await dbContext.CareHomes
                .AnyAsync(x =>
                    x.TenantId == tenantContext.TenantId &&
                    x.Code == code);

            if (duplicateCode)
            {
                return BadRequest(new
                {
                    message = "Care home code already exists."
                });
            }

            var careHome = new CareHomeLocation
            {
                TenantId = tenantContext.TenantId,
                CompanyId = request.CompanyId,
                Code = code,
                Name = request.Name.Trim(),
                BedCapacity = request.BedCapacity,
                Address = request.Address?.Trim(),
                Phone = request.Phone?.Trim(),
                Email = request.Email?.Trim(),
                ManagerName = request.ManagerName?.Trim(),
                ManagerPhone = request.ManagerPhone?.Trim(),
                ManagerEmail = request.ManagerEmail?.Trim(),
                IsActive = true
            };

            dbContext.CareHomes.Add(careHome);

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("CareHome", careHome.Id.ToString(), "Create", null, new { careHome.Code, careHome.Name }, "Created care home.");

            var dto = await ProjectToDto(
                    dbContext.CareHomes.AsNoTracking())
                .FirstAsync(x => x.Id == careHome.Id);

            return CreatedAtAction(
                nameof(GetCareHome),
                new { id = careHome.Id },
                dto);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CareHomeDto>> UpdateCareHome(
            int id,
            UpdateCareHomeRequest request)
        {
            var careHome = await dbContext.CareHomes
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

            if (careHome is null)
            {
                return NotFound();
            }

            var companyError =
                await ValidateSelectedCompany(
                    request.CompanyId,
                    careHome.CompanyId);

            if (companyError is not null)
            {
                return companyError;
            }

            var code = request.Code.Trim();

            var duplicateCode = await dbContext.CareHomes
                .AnyAsync(x =>
                    x.TenantId == tenantContext.TenantId &&
                    x.Id != id &&
                    x.Code == code);

            if (duplicateCode)
            {
                return BadRequest(new
                {
                    message = "Care home code already exists."
                });
            }

            if (careHome.IsActive && !request.IsActive)
            {
                var deactivationError =
                    await RejectIfDeactivatingWithCurrentClients(id);

                if (deactivationError is not null)
                {
                    return deactivationError;
                }
            }

            careHome.CompanyId = request.CompanyId;
            careHome.Code = code;
            careHome.Name = request.Name.Trim();
            careHome.BedCapacity = request.BedCapacity;
            careHome.Address = request.Address?.Trim();
            careHome.Phone = request.Phone?.Trim();
            careHome.Email = request.Email?.Trim();
            careHome.ManagerName = request.ManagerName?.Trim();
            careHome.ManagerPhone = request.ManagerPhone?.Trim();
            careHome.ManagerEmail = request.ManagerEmail?.Trim();
            careHome.IsActive = request.IsActive;

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("CareHome", careHome.Id.ToString(), "Update", null, request, "Updated care home.");

            var dto = await ProjectToDto(
                    dbContext.CareHomes.AsNoTracking())
                .FirstAsync(x => x.Id == careHome.Id);

            return Ok(dto);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeactivateCareHome(int id)
        {
            var careHome = await dbContext.CareHomes
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

            if (careHome is null)
            {
                return NotFound();
            }

            if (careHome.IsActive)
            {
                var deactivationError =
                    await RejectIfDeactivatingWithCurrentClients(id);

                if (deactivationError is not null)
                {
                    return deactivationError;
                }
            }

            careHome.IsActive = false;

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("CareHome", id.ToString(), "Deactivate", null, null, "Deactivated care home.");

            return NoContent();
        }

        private static IQueryable<CareHomeDto> ProjectToDto(
            IQueryable<CareHomeLocation> query)
        {
            return query.Select(x => new CareHomeDto
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                CompanyName = x.Company.Name,
                Code = x.Code,
                Name = x.Name,
                BedCapacity = x.BedCapacity,
                Address = x.Address,
                Phone = x.Phone,
                Email = x.Email,
                ManagerName = x.ManagerName,
                ManagerPhone = x.ManagerPhone,
                ManagerEmail = x.ManagerEmail,
                LogoPath = x.LogoPath,
                IsActive = x.IsActive
            });
        }

        private async Task<ActionResult?> ValidateSelectedCompany(
            int companyId,
            int? currentCompanyId = null)
        {
            var company = await dbContext.Companies
                .FirstOrDefaultAsync(x => x.Id == companyId && x.TenantId == tenantContext.TenantId);

            if (company is null)
            {
                return BadRequest(new
                {
                    message = "Selected company does not exist."
                });
            }

            var companyUnchanged =
                currentCompanyId == companyId;

            if (!companyUnchanged && !company.IsActive)
            {
                return BadRequest(new
                {
                    message =
                        "Selected company does not exist or is inactive."
                });
            }

            return null;
        }

        private async Task<ActionResult?> RejectIfDeactivatingWithCurrentClients(
            int careHomeId)
        {
            var hasCurrentClients =
                await dbContext.Clients.AnyAsync(x =>
                    x.CareHomeId == careHomeId &&
                    x.Status == "Current" &&
                    !x.IsArchived);

            if (hasCurrentClients)
            {
                return BadRequest(new
                {
                    message =
                        "This care home has current clients and cannot be deactivated."
                });
            }

            return null;
        }
    }
}
