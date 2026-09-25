using CareHome.Api.Remittance.Dtos;
using CareHome.Api.Remittance.Services;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/remittances")]
[RequireTenant]
[Authorize(Policy = CareHomePolicies.CanManagePayments)]
public class RemittancesController(
    RemittanceService remittance,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RemittanceBatchListDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await remittance.ListAsync(tenantContext.TenantId, cancellationToken));
    }

    [HttpGet("{publicId:guid}")]
    public async Task<ActionResult<RemittanceBatchDetailDto>> Get(Guid publicId, CancellationToken cancellationToken)
    {
        var detail = await remittance.GetAsync(tenantContext.TenantId, publicId, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("import/csv")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<RemittanceBatchDetailDto>> ImportCsv(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { message = "File is empty." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var actor = userManager.GetUserId(User);
            var detail = await remittance.ImportCsvAsync(
                tenantContext.TenantId,
                file.FileName,
                stream,
                actor,
                cancellationToken);
            return Ok(detail);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("import/xlsx")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<RemittanceBatchDetailDto>> ImportXlsx(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { message = "File is empty." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var actor = userManager.GetUserId(User);
            var detail = await remittance.ImportXlsxAsync(
                tenantContext.TenantId,
                file.FileName,
                stream,
                actor,
                cancellationToken);
            return Ok(detail);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{publicId:guid}")]
    public async Task<ActionResult<RemittanceBatchDetailDto>> Update(
        Guid publicId,
        UpdateRemittanceBatchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = userManager.GetUserId(User);
            return Ok(await remittance.UpdateAsync(tenantContext.TenantId, publicId, request, actor, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{publicId:guid}/confirm")]
    public async Task<ActionResult<RemittanceBatchDetailDto>> Confirm(
        Guid publicId,
        ConfirmRemittanceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = userManager.GetUserId(User);
            return Ok(await remittance.ConfirmAsync(tenantContext.TenantId, publicId, request, actor, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
