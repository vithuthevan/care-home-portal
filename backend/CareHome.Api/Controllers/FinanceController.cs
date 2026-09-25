using CareHome.Api.Dtos.Finance;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using CareHome.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/finance")]
[RequireTenant]
[Authorize(Policy = CareHomePolicies.CanViewFinancialReports)]
public class FinanceController(
    FinanceAttentionService financeAttention,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("attention")]
    public async Task<ActionResult<FinanceAttentionDto>> Attention(CancellationToken cancellationToken)
    {
        return Ok(await financeAttention.GetAsync(tenantContext.TenantId, cancellationToken));
    }
}
