using CareHome.Api.Billing;
using CareHome.Api.Dtos.Billing;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using CareHome.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/billing")]
[RequireTenant]
public class BillingController(
    BillingService billing,
    DocumentEmailService documentEmail,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("suggested-period")]
    [Authorize(Policy = CareHomePolicies.CanViewFinancialReports)]
    public async Task<ActionResult<BillingSuggestionDto>> SuggestedPeriod([FromQuery] int? careHomeId)
    {
        return Ok(await billing.SuggestPeriodAsync(tenantContext.TenantId, careHomeId, HttpContext.RequestAborted));
    }

    [HttpPost("preview")]
    [Authorize(Policy = CareHomePolicies.CanViewFinancialReports)]
    public async Task<ActionResult<BillingPreviewResponse>> Preview(BillingPreviewRequest request)
    {
        var validationError = BillingPreviewRequestValidator.Validate(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        return Ok(await billing.PreviewAsync(tenantContext.TenantId, request));
    }

    [HttpPost("generate")]
    [Authorize(Policy = CareHomePolicies.CanManageBilling)]
    public async Task<ActionResult<BillingGenerateResponse>> Generate(BillingPreviewRequest request)
    {
        var validationError = BillingPreviewRequestValidator.Validate(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var (result, error) = await billing.GenerateAsync(tenantContext.TenantId, request);
        if (error is not null && result is null)
        {
            return BadRequest(new { message = error });
        }

        if (error is not null)
        {
            return BadRequest(new { message = error, exceptions = result?.Exceptions });
        }

        if (result is not null && request.SendEmailAfterGenerate && result.InvoiceIds.Count > 0)
        {
            var emailSummary = await documentEmail.SendInvoicesAsync(
                tenantContext.TenantId,
                result.InvoiceIds,
                HttpContext.RequestAborted);
            result.EmailSend = new BillingEmailSendSummaryDto
            {
                Succeeded = emailSummary.Succeeded,
                Failed = emailSummary.Failed,
                Skipped = emailSummary.Skipped
            };
        }

        return Ok(result);
    }
}

