using CareHome.Api.Dtos.Disputes;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using CareHome.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/disputes")]
[RequireTenant]
[Authorize(Policy = CareHomePolicies.CanManageReceivables)]
public class DisputesController(
    DisputeWorkflowService disputes,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<DisputeListDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await disputes.ListAsync(tenantContext.TenantId, cancellationToken));
    }

    [HttpGet("{publicId:guid}")]
    public async Task<ActionResult<DisputeDetailDto>> Get(Guid publicId, CancellationToken cancellationToken)
    {
        var detail = await disputes.GetAsync(tenantContext.TenantId, publicId, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult<DisputeDetailDto>> Open(OpenDisputeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var actor = userManager.GetUserId(User);
            return Ok(await disputes.OpenAsync(tenantContext.TenantId, request, actor, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{publicId:guid}/messages")]
    public async Task<IActionResult> AddMessage(
        Guid publicId,
        [FromBody] string body,
        CancellationToken cancellationToken)
    {
        var actor = userManager.GetUserId(User);
        await disputes.AddMessageAsync(tenantContext.TenantId, publicId, body, actor, cancellationToken);
        return NoContent();
    }

    [HttpPost("{publicId:guid}/resolve")]
    public async Task<IActionResult> Resolve(
        Guid publicId,
        ResolveDisputeRequest request,
        CancellationToken cancellationToken)
    {
        var actor = userManager.GetUserId(User);
        await disputes.ResolveAsync(tenantContext.TenantId, publicId, request, actor, cancellationToken);
        return NoContent();
    }
}
