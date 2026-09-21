using CareHome.Api.Billing;
using CareHome.Api.Dtos.Billing;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [Route("api/billing")]
    [RequireTenant]
    public class BillingController(BillingService billing, ITenantContext tenantContext) : ControllerBase
    {
        [HttpPost("preview")]
        [Authorize(Policy = CareHomePolicies.CanViewFinancialReports)]
        public async Task<ActionResult<BillingPreviewResponse>> Preview(BillingPreviewRequest request)
        {
            return Ok(await billing.PreviewAsync(tenantContext.TenantId, request));
        }

        [HttpPost("generate")]
        [Authorize(Policy = CareHomePolicies.CanManageBilling)]
        public async Task<ActionResult<BillingGenerateResponse>> Generate(BillingPreviewRequest request)
        {
            var (result, error) = await billing.GenerateAsync(tenantContext.TenantId, request);
            if (error is not null && result is null)
            {
                return BadRequest(new { message = error });
            }

            if (error is not null)
            {
                return BadRequest(new { message = error, exceptions = result?.Exceptions });
            }

            return Ok(result);
        }
    }
}

