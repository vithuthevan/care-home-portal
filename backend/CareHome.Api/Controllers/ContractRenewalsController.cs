using CareHome.Api.Dtos.Renewals;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using CareHome.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/contract-renewals")]
[RequireTenant]
[Authorize(Policy = CareHomePolicies.CanManageFunding)]
public class ContractRenewalsController(
    ContractRenewalWorkflowService renewals,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<RenewalDashboardDto>> Dashboard(CancellationToken cancellationToken)
    {
        return Ok(await renewals.GetDashboardAsync(tenantContext.TenantId, cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<List<RenewalListDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await renewals.ListAsync(tenantContext.TenantId, cancellationToken));
    }

    [HttpPost("for-contract/{contractId:int}")]
    public async Task<ActionResult<RenewalListDto>> Create(int contractId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await renewals.CreateForContractAsync(tenantContext.TenantId, contractId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{publicId:guid}/agree")]
    public async Task<IActionResult> Agree(
        Guid publicId,
        AgreeRenewalRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = userManager.GetUserId(User);
            await renewals.AgreeAsync(tenantContext.TenantId, publicId, request, actor, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
