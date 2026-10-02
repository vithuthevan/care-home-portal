using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using CareHome.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/reports")]
[RequireTenant]
[Authorize(Policy = CareHomePolicies.CanViewFinancialReports)]
public class ReportsController(ReportService reports, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("client-census")]
    public async Task<IActionResult> Census(int? companyId, int? careHomeId, string? format)
    {
        var rows = await reports.ClientCensusAsync(tenantContext.TenantId, companyId, careHomeId, HttpContext.RequestAborted);
        return Export(format, "client-census", rows);
    }

    [HttpGet("current-rates")]
    public async Task<IActionResult> CurrentRates(
        int? companyId,
        int? careHomeId,
        string? clientStatus,
        int? fundingAuthorityId,
        int? categoryId,
        string? format)
    {
        var rows = await reports.CurrentRatesAsync(tenantContext.TenantId, companyId, careHomeId, clientStatus, fundingAuthorityId, categoryId, HttpContext.RequestAborted);
        return Export(format, "current-rates", rows);
    }

    [HttpGet("invoices-by-client")]
    public async Task<IActionResult> InvoicesByClient(int? clientId, DateOnly? from, DateOnly? to, string? format)
    {
        var rows = await reports.InvoicesByClientAsync(tenantContext.TenantId, clientId, from, to, HttpContext.RequestAborted);
        return Export(format, "invoices-by-client", rows);
    }

    [HttpGet("invoices-by-care-home")]
    public async Task<IActionResult> InvoicesByCareHome(int? careHomeId, DateOnly? from, DateOnly? to, string? format)
    {
        var rows = await reports.InvoicesByCareHomeAsync(tenantContext.TenantId, careHomeId, from, to, HttpContext.RequestAborted);
        return Export(format, "invoices-by-care-home", rows);
    }

    [HttpGet("income-by-category")]
    public async Task<IActionResult> Income(DateOnly? from, DateOnly? to, string? format)
    {
        if (from is null || to is null)
        {
            return BadRequest(new { message = "From and to dates are required." });
        }

        if (to < from)
        {
            return BadRequest(new { message = "To date cannot be before from date." });
        }

        var rows = await reports.IncomeByCategoryAsync(tenantContext.TenantId, from.Value, to.Value, HttpContext.RequestAborted);
        return Export(format, "income-by-category", rows);
    }

    [HttpGet("occupancy")]
    public async Task<IActionResult> Occupancy(int? companyId, string? format)
    {
        var rows = await reports.OccupancyAsync(tenantContext.TenantId, companyId, HttpContext.RequestAborted);
        return Export(format, "occupancy", rows);
    }

    [HttpGet("rate-history")]
    public async Task<IActionResult> RateHistory(int? contractId, string? format)
    {
        var rows = await reports.RateHistoryAsync(tenantContext.TenantId, contractId, HttpContext.RequestAborted);
        return Export(format, "rate-history", rows);
    }

    [HttpGet("billing-exceptions")]
    public async Task<IActionResult> Exceptions(string? format)
    {
        var rows = await reports.BillingExceptionsAsync(tenantContext.TenantId, HttpContext.RequestAborted);
        return Export(format, "billing-exceptions", rows);
    }

    [HttpGet("outstanding")]
    public async Task<IActionResult> Outstanding(string? format)
    {
        var rows = await reports.OutstandingAsync(tenantContext.TenantId, HttpContext.RequestAborted);
        return Export(format, "outstanding", rows);
    }

    private IActionResult Export<T>(string? format, string name, List<T> rows)
    {
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return File(reports.ToCsv(name, rows), "text/csv", $"{name}.csv");
        }

        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase))
        {
            return File(reports.ToExcel(name, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{name}.xlsx");
        }

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            return File(reports.ToPdf(name, rows), "application/pdf", $"{name}.pdf");
        }

        return Ok(rows);
    }
}

