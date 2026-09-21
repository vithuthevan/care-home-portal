using CareHome.Api.Common;
using CareHome.Api.Receivables.Contracts;
using CareHome.Api.Receivables.Domain;
using CareHome.Api.Receivables.Dtos;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/receivables")]
[RequireTenant]
[Authorize(Policy = CareHomePolicies.CanViewReceivables)]
public class ReceivablesController(
    IReceivablesService receivables,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ReceivablesSummaryDto>> Summary(
        int? companyId,
        int? careHomeId,
        int? fundingAuthorityId,
        DateOnly? asOfDate,
        CancellationToken cancellationToken)
    {
        var query = BuildQuery(companyId, careHomeId, fundingAuthorityId, asOfDate);
        var summary = await receivables.GetTenantSummaryAsync(tenantContext.TenantId, query, cancellationToken);
        return Ok(summary);
    }

    [HttpGet("ageing")]
    public async Task<ActionResult<ReceivablesAgeingDto>> Ageing(
        int? companyId,
        int? careHomeId,
        int? fundingAuthorityId,
        DateOnly? asOfDate,
        CancellationToken cancellationToken)
    {
        var query = BuildQuery(companyId, careHomeId, fundingAuthorityId, asOfDate);
        var ageing = await receivables.GetAgeingAsync(tenantContext.TenantId, query, cancellationToken);
        return Ok(ageing);
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<PagedResult<ReceivableInvoiceDto>>> Invoices(
        int? companyId,
        int? careHomeId,
        int? fundingAuthorityId,
        string? documentStatus,
        string? paymentStatus,
        bool? overdueOnly,
        ReceivableAgeingBucket? ageingBucket,
        DateOnly? invoiceDateFrom,
        DateOnly? invoiceDateTo,
        DateOnly? dueDateFrom,
        DateOnly? dueDateTo,
        string? invoiceNumber,
        bool openReceivablesOnly = true,
        DateOnly? asOfDate = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new ReceivableInvoiceQuery
        {
            CompanyId = companyId,
            CareHomeId = careHomeId,
            FundingAuthorityId = fundingAuthorityId,
            DocumentStatus = documentStatus,
            PaymentStatus = paymentStatus,
            OverdueOnly = overdueOnly,
            AgeingBucket = ageingBucket,
            InvoiceDateFrom = invoiceDateFrom,
            InvoiceDateTo = invoiceDateTo,
            DueDateFrom = dueDateFrom,
            DueDateTo = dueDateTo,
            InvoiceNumber = invoiceNumber,
            OpenReceivablesOnly = openReceivablesOnly,
            AsOfDate = asOfDate,
            Page = page,
            PageSize = pageSize
        };

        var (items, total) = await receivables.ListInvoicesAsync(
            tenantContext.TenantId,
            query,
            cancellationToken);

        return Ok(new PagedResult<ReceivableInvoiceDto>
        {
            Items = items,
            TotalCount = total,
            Page = Math.Max(page, 1),
            PageSize = Math.Clamp(pageSize, 1, 200)
        });
    }

    [HttpGet("funders")]
    public async Task<ActionResult<List<FunderReceivableSummaryDto>>> Funders(
        int? companyId,
        int? careHomeId,
        DateOnly? asOfDate,
        CancellationToken cancellationToken)
    {
        var query = BuildQuery(companyId, careHomeId, null, asOfDate);
        var rows = await receivables.ListFunderSummariesAsync(tenantContext.TenantId, query, cancellationToken);
        return Ok(rows);
    }

    [HttpGet("care-homes")]
    public async Task<ActionResult<List<CareHomeReceivableSummaryDto>>> CareHomes(
        int? companyId,
        DateOnly? asOfDate,
        CancellationToken cancellationToken)
    {
        var query = BuildQuery(companyId, null, null, asOfDate);
        var rows = await receivables.ListCareHomeSummariesAsync(tenantContext.TenantId, query, cancellationToken);
        return Ok(rows);
    }

    [HttpGet("care-homes/{careHomeId:int}")]
    public async Task<ActionResult<CareHomeReceivableSummaryDto>> CareHome(
        int careHomeId,
        DateOnly? asOfDate,
        CancellationToken cancellationToken)
    {
        var query = new ReceivableInvoiceQuery { AsOfDate = asOfDate };
        var summary = await receivables.GetCareHomeSummaryAsync(
            tenantContext.TenantId,
            careHomeId,
            query,
            cancellationToken);
        return summary is null ? NotFound() : Ok(summary);
    }

    private static ReceivableInvoiceQuery BuildQuery(
        int? companyId,
        int? careHomeId,
        int? fundingAuthorityId,
        DateOnly? asOfDate) =>
        new()
        {
            CompanyId = companyId,
            CareHomeId = careHomeId,
            FundingAuthorityId = fundingAuthorityId,
            AsOfDate = asOfDate
        };
}
