using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.NominalCodes;
using CareHome.Api.Models;
using CareHome.Api.Security;
using CareHome.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/nominal-codes")]
[RequireTenant]
public class NominalCodesController(
    CareHomeDbContext dbContext,
    ITenantContext tenantContext,
    AuditService audit,
    MasterDataUsageService usage) : ControllerBase
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

        var entities = await query.OrderBy(x => x.Name).ToListAsync();
        var usageMap = await usage.GetNominalCodeUsagesAsync(
            tenantContext.TenantId,
            entities.Select(x => (x.Id, x.Code)).ToList());

        var codes = entities.Select(x => ToDto(x, usageMap.GetValueOrDefault(x.Id))).ToList();

        return Ok(codes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NominalCodeDto>> GetNominalCode(int id)
    {
        var entity = await dbContext.NominalCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

        if (entity is null)
        {
            return NotFound();
        }

        var usageDto = await usage.GetNominalCodeUsageAsync(
            tenantContext.TenantId,
            entity.Id,
            entity.Code);

        return Ok(ToDto(entity, usageDto));
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
            ToDto(nominalCode, new()));
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

        var usageDto = await usage.GetNominalCodeUsageAsync(
            tenantContext.TenantId,
            nominalCode.Id,
            nominalCode.Code);

        return Ok(ToDto(nominalCode, usageDto));
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

    private static NominalCodeDto ToDto(NominalCode nominalCode, Dtos.Common.MasterDataUsageDto? usageDto)
    {
        return new NominalCodeDto
        {
            Id = nominalCode.Id,
            Code = nominalCode.Code,
            Name = nominalCode.Name,
            Description = nominalCode.Description,
            IsActive = nominalCode.IsActive,
            ConfigurationSource = "Organisation",
            Usage = usageDto
        };
    }
}
