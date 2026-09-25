using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Documents;
using CareHome.Api.Dtos.Sage;
using CareHome.Api.Export;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [Route("api/sage-exports")]
    [RequireTenant]
    public class SageExportsController(
        SageExportService sage,
        CareHomeDbContext dbContext,
        IDocumentStore documents,
        ITenantContext tenantContext) : ControllerBase
    {
        [HttpPost("preview")]
        public async Task<ActionResult<SageExportPreviewResponse>> Preview(SageExportRequest request)
        {
            var (preview, _) = await sage.PreviewAsync(tenantContext.TenantId, request);
            return Ok(preview);
        }

        [HttpPost]
        public async Task<ActionResult<SageExportBatchDto>> Export(SageExportRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var publicId = await dbContext.Tenants
                .Where(x => x.Id == tenantContext.TenantId)
                .Select(x => x.PublicId)
                .FirstAsync();
            var (batch, error) = await sage.ExportAsync(tenantContext.TenantId, publicId, request, userId);
            if (error is not null || batch is null)
            {
                return BadRequest(new { message = error ?? "Export failed." });
            }

            return Ok(new SageExportBatchDto
            {
                Id = batch.Id,
                ExportedAt = batch.ExportedAt,
                DateFrom = batch.DateFrom,
                DateTo = batch.DateTo,
                CompanyId = batch.CompanyId,
                RecordCount = batch.RecordCount,
                FileName = batch.FileName,
                Status = batch.Status
            });
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<SageExportBatchDto>>> List(int page = 1, int pageSize = 50)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);
            var query = dbContext.SageExportBatches.AsNoTracking()
                .Where(x => x.TenantId == tenantContext.TenantId);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.ExportedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SageExportBatchDto
                {
                    Id = x.Id,
                    ExportedAt = x.ExportedAt,
                    DateFrom = x.DateFrom,
                    DateTo = x.DateTo,
                    CompanyId = x.CompanyId,
                    RecordCount = x.RecordCount,
                    FileName = x.FileName,
                    Status = x.Status
                })
                .ToListAsync();

            return Ok(new PagedResult<SageExportBatchDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpPost("{id:int}/retry-file")]
        public async Task<ActionResult<SageExportBatchDto>> RetryFile(int id)
        {
            var publicId = await dbContext.Tenants
                .Where(x => x.Id == tenantContext.TenantId)
                .Select(x => x.PublicId)
                .FirstAsync();
            var (batch, error) = await sage.RetryFileWriteAsync(tenantContext.TenantId, publicId, id);
            if (error is not null || batch is null)
            {
                return BadRequest(new { message = error ?? "Retry failed." });
            }

            return Ok(new SageExportBatchDto
            {
                Id = batch.Id,
                ExportedAt = batch.ExportedAt,
                DateFrom = batch.DateFrom,
                DateTo = batch.DateTo,
                CompanyId = batch.CompanyId,
                RecordCount = batch.RecordCount,
                FileName = batch.FileName,
                Status = batch.Status
            });
        }

        [HttpGet("{id:int}/file")]
        public async Task<IActionResult> FileDownload(int id)
        {
            var batch = await dbContext.SageExportBatches.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);
            if (batch is null)
            {
                return NotFound();
            }

            var bytes = await documents.ReadAsync(batch.FilePath);
            if (bytes is null)
            {
                return NotFound(new { message = "Export recorded, but the CSV file is unavailable. Use Retry CSV to regenerate the file." });
            }

            return File(bytes, "text/csv", batch.FileName);
        }
    }
}

