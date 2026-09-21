using CareHome.Api.Common;
using CareHome.Api.Reconciliation.Domain;
using CareHome.Api.Reconciliation.Dtos;
using BankCsvColumnMapping = CareHome.Api.Reconciliation.Domain.BankCsvColumnMapping;
using CareHome.Api.Reconciliation.Services;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/banking")]
[RequireTenant]
public class BankingController(
    BankAccountService bankAccounts,
    BankImportService bankImport,
    BankCsvMappingTemplateService mappingTemplates,
    ReconciliationService reconciliation,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("accounts")]
    [Authorize(Policy = CareHomePolicies.CanViewBanking)]
    public async Task<ActionResult<List<BankAccountDto>>> ListAccounts(CancellationToken cancellationToken)
    {
        return Ok(await bankAccounts.ListAsync(tenantContext.TenantId, cancellationToken));
    }

    [HttpPost("accounts")]
    [Authorize(Policy = CareHomePolicies.CanManageBanking)]
    public async Task<ActionResult<BankAccountDto>> CreateAccount(
        CreateBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await bankAccounts.CreateAsync(tenantContext.TenantId, request, cancellationToken);
            return CreatedAtAction(nameof(ListAccounts), created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("imports/preview")]
    [Authorize(Policy = CareHomePolicies.CanManageBanking)]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<ActionResult<BankImportPreviewDto>> PreviewImport(
        [FromForm] Guid bankAccountPublicId,
        [FromForm] IFormFile file,
        [FromForm] string? columnMappingJson,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { message = "File is empty." });
        }

        BankCsvColumnMapping? mapping = null;
        if (!string.IsNullOrWhiteSpace(columnMappingJson))
        {
            mapping = System.Text.Json.JsonSerializer.Deserialize<BankCsvColumnMapping>(
                columnMappingJson,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        }

        await using var stream = file.OpenReadStream();
        var preview = await bankImport.PreviewAsync(
            tenantContext.TenantId,
            bankAccountPublicId,
            file.FileName,
            stream,
            mapping,
            cancellationToken);
        return Ok(preview);
    }

    [HttpPost("imports/commit")]
    [Authorize(Policy = CareHomePolicies.CanManageBanking)]
    public async Task<ActionResult<BankImportBatchDto>> CommitImport(
        [FromBody] BankImportCommitWithContentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = userManager.GetUserId(User);
            var (batch, error) = await bankImport.CommitAsync(
                tenantContext.TenantId,
                request.BankAccountPublicId,
                request.FileName,
                request.ContentChecksum,
                request.CsvContent,
                request.ColumnMapping,
                request.AcceptedRowNumbers,
                actor,
                cancellationToken);
            return error is not null ? BadRequest(new { message = error }) : Ok(batch);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("imports")]
    [Authorize(Policy = CareHomePolicies.CanViewBanking)]
    public async Task<ActionResult<List<BankImportBatchDto>>> ListImports(CancellationToken cancellationToken)
    {
        return Ok(await bankImport.ListBatchesAsync(tenantContext.TenantId, cancellationToken));
    }

    [HttpGet("reconciliation")]
    [Authorize(Policy = CareHomePolicies.CanReconcilePayments)]
    public async Task<ActionResult<ReconciliationWorkspaceSummaryDto>> ReconciliationWorkspace(
        string? status,
        CancellationToken cancellationToken)
    {
        return Ok(await reconciliation.GetWorkspaceAsync(tenantContext.TenantId, status, cancellationToken));
    }

    [HttpPost("reconciliation/transactions/{publicId:guid}/confirm")]
    [Authorize(Policy = CareHomePolicies.CanReconcilePayments)]
    public async Task<IActionResult> Confirm(
        Guid publicId,
        ConfirmReconciliationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = userManager.GetUserId(User);
            await reconciliation.ConfirmAsync(tenantContext.TenantId, publicId, request, actor, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("reconciliation/transactions/{publicId:guid}/unapplied-payment")]
    [Authorize(Policy = CareHomePolicies.CanReconcilePayments)]
    public async Task<IActionResult> CreateUnapplied(
        Guid publicId,
        CreateUnappliedPaymentFromBankRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = userManager.GetUserId(User);
            await reconciliation.CreateUnappliedPaymentAsync(
                tenantContext.TenantId,
                publicId,
                request,
                actor,
                cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("reconciliation/transactions/{publicId:guid}/ignore")]
    [Authorize(Policy = CareHomePolicies.CanReconcilePayments)]
    public async Task<IActionResult> Ignore(Guid publicId, CancellationToken cancellationToken)
    {
        try
        {
            var actor = userManager.GetUserId(User);
            await reconciliation.IgnoreAsync(tenantContext.TenantId, publicId, actor, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("reconciliation/transactions/{publicId:guid}/reverse")]
    [Authorize(Policy = CareHomePolicies.CanReconcilePayments)]
    public async Task<IActionResult> Reverse(
        Guid publicId,
        ReverseReconciliationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = userManager.GetUserId(User);
            await reconciliation.ReverseAsync(tenantContext.TenantId, publicId, request, actor, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("reconciliation/invoices/search")]
    [Authorize(Policy = CareHomePolicies.CanReconcilePayments)]
    public async Task<ActionResult<List<ManualInvoiceSearchResultDto>>> SearchInvoices(
        string? q,
        decimal? amount,
        int? fundingAuthorityId,
        int? careHomeId,
        int? clientId,
        CancellationToken cancellationToken)
    {
        return Ok(await reconciliation.SearchInvoicesAsync(
            tenantContext.TenantId,
            q,
            amount,
            fundingAuthorityId,
            careHomeId,
            clientId,
            cancellationToken));
    }

    [HttpGet("reconciliation/transactions/{publicId:guid}")]
    [Authorize(Policy = CareHomePolicies.CanReconcilePayments)]
    public async Task<ActionResult<BankTransactionDetailDto>> GetTransaction(
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var detail = await reconciliation.GetTransactionDetailAsync(tenantContext.TenantId, publicId, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("import-mapping-templates")]
    [Authorize(Policy = CareHomePolicies.CanManageBanking)]
    public async Task<ActionResult<List<BankCsvMappingTemplateDto>>> ListMappingTemplates(
        Guid? bankAccountPublicId,
        CancellationToken cancellationToken)
    {
        return Ok(await mappingTemplates.ListAsync(tenantContext.TenantId, bankAccountPublicId, cancellationToken));
    }

    [HttpPost("import-mapping-templates")]
    [Authorize(Policy = CareHomePolicies.CanManageBanking)]
    public async Task<ActionResult<BankCsvMappingTemplateDto>> SaveMappingTemplate(
        SaveBankCsvMappingTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await mappingTemplates.SaveAsync(tenantContext.TenantId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("import-mapping-templates/{publicId:guid}")]
    [Authorize(Policy = CareHomePolicies.CanManageBanking)]
    public async Task<IActionResult> DeleteMappingTemplate(Guid publicId, CancellationToken cancellationToken)
    {
        await mappingTemplates.DeleteAsync(tenantContext.TenantId, publicId, cancellationToken);
        return NoContent();
    }
}

public class BankImportCommitWithContentRequest : BankImportCommitRequest
{
    public string CsvContent { get; set; } = string.Empty;
}
