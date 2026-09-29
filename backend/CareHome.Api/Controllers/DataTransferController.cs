using CareHome.Api.Dtos.DataTransfer;
using CareHome.Api.ImportExport;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using CareHome.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/data-transfer")]
[Authorize]
public class DataTransferController(
    DataTransferService dataTransfer,
    IAuthorizationService authorization,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("entities")]
    [RequireTenant]
    public ActionResult<IReadOnlyList<DataTransferEntityInfoDto>> ListEntities() =>
        Ok(dataTransfer.ListEntities());

    [HttpGet("{entity}/template")]
    [RequireTenant]
    [Authorize(Policy = CareHomePolicies.CanManageOrganisation)]
    public async Task<IActionResult> DownloadTemplate(string entity, string format = "xlsx", CancellationToken cancellationToken = default)
    {
        var denied = await EnsureAuthorizedForEntityAsync(entity);
        if (denied is not null)
        {
            return denied;
        }

        try
        {
            var bytes = await dataTransfer.GetTemplateAsync(entity, format, cancellationToken);
            var fileName = $"{entity}-template.{TabularSpreadsheet.FileExtension(format)}";
            return File(bytes, TabularSpreadsheet.ContentType(format), fileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{entity}/export")]
    public async Task<IActionResult> Export(string entity, string format = "xlsx", CancellationToken cancellationToken = default)
    {
        var denied = await EnsureAuthorizedForEntityAsync(entity);
        if (denied is not null)
        {
            return denied;
        }

        try
        {
            var bytes = await dataTransfer.ExportAsync(entity, format, cancellationToken);
            var fileName = $"{entity}-export.{TabularSpreadsheet.FileExtension(format)}";
            return File(bytes, TabularSpreadsheet.ContentType(format), fileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{entity}/import/preview")]
    [RequireTenant]
    [Authorize(Policy = CareHomePolicies.CanManageOrganisation)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<DataTransferPreviewDto>> PreviewImport(
        string entity,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureAuthorizedForEntityAsync(entity);
        if (denied is not null)
        {
            return denied;
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "A .csv or .xlsx file is required." });
        }

        if (!TabularSpreadsheet.IsSupported(file.FileName))
        {
            return BadRequest(new { message = "Only .csv and .xlsx files are accepted." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await dataTransfer.PreviewImportAsync(entity, file.FileName, stream, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{entity}/import/confirm")]
    [RequireTenant]
    [Authorize(Policy = CareHomePolicies.CanManageOrganisation)]
    public async Task<ActionResult<DataTransferCommitResultDto>> ConfirmImport(
        string entity,
        [FromBody] DataTransferPreviewDto preview,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureAuthorizedForEntityAsync(entity);
        if (denied is not null)
        {
            return denied;
        }

        try
        {
            return Ok(await dataTransfer.CommitImportAsync(entity, preview, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<ActionResult?> EnsureAuthorizedForEntityAsync(string entity)
    {
        var meta = dataTransfer.ListEntities()
            .FirstOrDefault(x => x.Key.Equals(entity, StringComparison.OrdinalIgnoreCase));
        if (meta is null)
        {
            return null;
        }

        if (meta.RequiresPlatform)
        {
            var platform = await authorization.AuthorizeAsync(User, CareHomePolicies.PlatformOnly);
            return platform.Succeeded ? null : Forbid();
        }

        if (!tenantContext.HasTenant)
        {
            return new ObjectResult(new { message = "This account cannot access organisation data." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        return null;
    }
}
