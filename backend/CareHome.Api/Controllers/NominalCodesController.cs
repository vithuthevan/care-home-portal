using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.NominalCodes;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [Route("api/nominal-codes")]
    [RequireTenant]
    public class NominalCodesController(
        CareHomeDbContext dbContext,
        ITenantContext tenantContext,
        AuditService audit) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<NominalCodeDto>>> GetNominalCodes(
            bool activeOnly = false)
        {
            var query = dbContext.NominalCodes.AsNoTracking()
                .ForTenant(tenantContext.TenantId);

            if (activeOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var codes = await query
                .OrderBy(x => x.Name)
                .Select(x => new NominalCodeDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            return Ok(codes);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<NominalCodeDto>> GetNominalCode(int id)
        {
            var code = await dbContext.NominalCodes
                .AsNoTracking()
                .Where(x => x.Id == id && x.TenantId == tenantContext.TenantId)
                .Select(x => new NominalCodeDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();

            if (code is null)
            {
                return NotFound();
            }

            return Ok(code);
        }

        [HttpPost]
        public async Task<ActionResult<NominalCodeDto>> CreateNominalCode(
            CreateNominalCodeRequest request)
        {
            var code = request.Code.Trim();

            var duplicateCode = await dbContext.NominalCodes
                .AnyAsync(x => x.TenantId == tenantContext.TenantId && x.Code == code);

            if (duplicateCode)
            {
                return BadRequest(new
                {
                    message = "Nominal code already exists."
                });
            }

            var nominalCode = new NominalCode
            {
                TenantId = tenantContext.TenantId,
                Code = code,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                IsActive = true
            };

            dbContext.NominalCodes.Add(nominalCode);

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("NominalCode", nominalCode.Id.ToString(), "Create", null, new { nominalCode.Code, nominalCode.Name }, "Created nominal code.");

            return CreatedAtAction(
                nameof(GetNominalCode),
                new { id = nominalCode.Id },
                ToDto(nominalCode));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<NominalCodeDto>> UpdateNominalCode(
            int id,
            UpdateNominalCodeRequest request)
        {
            var nominalCode = await dbContext.NominalCodes
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

            if (nominalCode is null)
            {
                return NotFound();
            }

            var code = request.Code.Trim();

            var duplicateCode = await dbContext.NominalCodes
                .AnyAsync(x =>
                    x.TenantId == tenantContext.TenantId &&
                    x.Id != id &&
                    x.Code == code);

            if (duplicateCode)
            {
                return BadRequest(new
                {
                    message = "Nominal code already exists."
                });
            }

            nominalCode.Code = code;
            nominalCode.Name = request.Name.Trim();
            nominalCode.Description = request.Description?.Trim();
            nominalCode.IsActive = request.IsActive;

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("NominalCode", nominalCode.Id.ToString(), "Update", null, request, "Updated nominal code.");

            return Ok(ToDto(nominalCode));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeactivateNominalCode(int id)
        {
            var nominalCode = await dbContext.NominalCodes
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

            if (nominalCode is null)
            {
                return NotFound();
            }

            nominalCode.IsActive = false;

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("NominalCode", id.ToString(), "Deactivate", null, null, "Deactivated nominal code.");

            return NoContent();
        }

        private static NominalCodeDto ToDto(NominalCode nominalCode)
        {
            return new NominalCodeDto
            {
                Id = nominalCode.Id,
                Code = nominalCode.Code,
                Name = nominalCode.Name,
                Description = nominalCode.Description,
                IsActive = nominalCode.IsActive
            };
        }
    }
}
