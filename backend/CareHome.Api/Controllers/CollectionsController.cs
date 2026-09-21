using CareHome.Api.Dtos.Collections;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using CareHome.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/collections")]
[RequireTenant]
[Authorize(Policy = CareHomePolicies.CanViewReceivables)]
public class CollectionsController(
    CollectionsWorkflowService collections,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<CollectionsDashboardDto>> Dashboard(CancellationToken cancellationToken)
    {
        return Ok(await collections.GetDashboardAsync(tenantContext.TenantId, cancellationToken));
    }

    [HttpPut("policy")]
    [Authorize(Policy = CareHomePolicies.CanManageOrganisation)]
    public async Task<ActionResult<CollectionPolicyDto>> UpdatePolicy(
        UpdateCollectionPolicyRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await collections.UpdatePolicyAsync(tenantContext.TenantId, request, cancellationToken));
    }
}
