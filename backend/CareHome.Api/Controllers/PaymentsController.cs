using CareHome.Api.Common;
using CareHome.Api.Payments.Dtos;
using CareHome.Api.Payments.Services;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/payments")]
[RequireTenant]
public class PaymentsController(
    PaymentService payments,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = CareHomePolicies.CanViewPayments)]
    public async Task<ActionResult<PagedResult<PaymentListDto>>> List(
        string? status,
        bool unappliedOnly = false,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await payments.ListAsync(
            tenantContext.TenantId,
            status,
            unappliedOnly,
            page,
            pageSize,
            cancellationToken);

        return Ok(new PagedResult<PaymentListDto>
        {
            Items = items,
            TotalCount = total,
            Page = Math.Max(page, 1),
            PageSize = Math.Clamp(pageSize, 1, 200)
        });
    }

    [HttpGet("{publicId:guid}")]
    [Authorize(Policy = CareHomePolicies.CanViewPayments)]
    public async Task<ActionResult<PaymentDetailDto>> Get(Guid publicId, CancellationToken cancellationToken)
    {
        var detail = await payments.GetByPublicIdAsync(tenantContext.TenantId, publicId, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("{publicId:guid}/allocation-candidates")]
    [Authorize(Policy = CareHomePolicies.CanManagePayments)]
    public async Task<ActionResult<List<PaymentAllocationCandidateDto>>> AllocationCandidates(
        Guid publicId,
        string? search,
        CancellationToken cancellationToken)
    {
        var candidates = await payments.ListAllocationCandidatesAsync(
            tenantContext.TenantId,
            publicId,
            search,
            cancellationToken);
        return Ok(candidates);
    }

    [HttpGet("{publicId:guid}/allocation-suggestions")]
    [Authorize(Policy = CareHomePolicies.CanManagePayments)]
    public async Task<ActionResult<List<PaymentAllocationSuggestionDto>>> Suggestions(
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var suggestions = await payments.SuggestAllocationsAsync(
            tenantContext.TenantId,
            publicId,
            cancellationToken);
        return Ok(suggestions);
    }

    [HttpPost]
    [Authorize(Policy = CareHomePolicies.CanManagePayments)]
    public async Task<ActionResult<PaymentDetailDto>> Create(
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = userManager.GetUserId(User);
            var detail = await payments.CreateManualAsync(
                tenantContext.TenantId,
                request,
                actor,
                cancellationToken);
            return CreatedAtAction(nameof(Get), new { publicId = detail.PublicId }, detail);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{publicId:guid}/allocations")]
    [Authorize(Policy = CareHomePolicies.CanManagePayments)]
    public async Task<ActionResult<PaymentDetailDto>> Allocate(
        Guid publicId,
        AllocatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = userManager.GetUserId(User);
            var detail = await payments.AllocateAsync(
                tenantContext.TenantId,
                publicId,
                request,
                actor,
                cancellationToken);
            return Ok(detail);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{publicId:guid}/reverse")]
    [Authorize(Policy = CareHomePolicies.CanManagePayments)]
    public async Task<ActionResult<PaymentDetailDto>> ReversePayment(
        Guid publicId,
        ReversePaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = userManager.GetUserId(User);
            var detail = await payments.ReversePaymentAsync(
                tenantContext.TenantId,
                publicId,
                request,
                actor,
                cancellationToken);
            return Ok(detail);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{publicId:guid}/allocations/{allocationPublicId:guid}/reverse")]
    [Authorize(Policy = CareHomePolicies.CanManagePayments)]
    public async Task<ActionResult<PaymentDetailDto>> ReverseAllocation(
        Guid publicId,
        Guid allocationPublicId,
        ReverseAllocationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = userManager.GetUserId(User);
            var detail = await payments.ReverseAllocationAsync(
                tenantContext.TenantId,
                publicId,
                allocationPublicId,
                request,
                actor,
                cancellationToken);
            return Ok(detail);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
