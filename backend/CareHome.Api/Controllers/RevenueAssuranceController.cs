using CareHome.Api.RevenueAssurance.Dtos;
using CareHome.Api.RevenueAssurance.Services;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/revenue-assurance")]
[RequireTenant]
[Authorize(Policy = CareHomePolicies.CanViewFinancialReports)]
public class RevenueAssuranceController(
    RevenueAssuranceService revenueAssurance,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<RevenueAssuranceDashboardDto>> Dashboard(CancellationToken cancellationToken)
    {
        return Ok(await revenueAssurance.GetDashboardAsync(tenantContext.TenantId, cancellationToken));
    }

    [HttpGet("findings")]
    public async Task<ActionResult<List<RevenueAssuranceFindingDto>>> Findings(
        string? status,
        CancellationToken cancellationToken)
    {
        return Ok(await revenueAssurance.ListFindingsAsync(tenantContext.TenantId, status, cancellationToken));
    }

    [HttpPost("scan")]
    [Authorize(Policy = CareHomePolicies.CanManageBilling)]
    public async Task<ActionResult<object>> Scan(CancellationToken cancellationToken)
    {
        var actor = userManager.GetUserId(User);
        var created = await revenueAssurance.RunScanAsync(tenantContext.TenantId, actor, cancellationToken);
        return Ok(new { created });
    }

    [HttpPost("findings/{publicId:guid}/resolve")]
    [Authorize(Policy = CareHomePolicies.CanManageBilling)]
    public async Task<IActionResult> Resolve(
        Guid publicId,
        ResolveFindingRequest request,
        CancellationToken cancellationToken)
    {
        var actor = userManager.GetUserId(User);
        await revenueAssurance.ResolveFindingAsync(tenantContext.TenantId, publicId, request, actor, cancellationToken);
        return NoContent();
    }
}
