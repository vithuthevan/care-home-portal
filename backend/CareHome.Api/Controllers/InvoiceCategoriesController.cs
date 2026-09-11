using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.InvoiceCategories;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [Route("api/invoice-categories")]
    [RequireTenant]
    public class InvoiceCategoriesController(
        CareHomeDbContext dbContext,
        ITenantContext tenantContext,
        AuditService audit) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<InvoiceCategoryDto>>> GetInvoiceCategories(
            bool activeOnly = false)
        {
            var query = dbContext.InvoiceCategories.AsNoTracking()
                .ForTenant(tenantContext.TenantId);

            if (activeOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var categories = await query
                .OrderBy(x => x.Name)
                .Select(x => new InvoiceCategoryDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InvoiceCategoryDto>> GetInvoiceCategory(int id)
        {
            var category = await dbContext.InvoiceCategories
                .AsNoTracking()
                .Where(x => x.Id == id && x.TenantId == tenantContext.TenantId)
                .Select(x => new InvoiceCategoryDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();

            if (category is null)
            {
                return NotFound();
            }

            return Ok(category);
        }

        [HttpPost]
        public async Task<ActionResult<InvoiceCategoryDto>> CreateInvoiceCategory(
            CreateInvoiceCategoryRequest request)
        {
            var code = request.Code.Trim();

            var duplicateCode = await dbContext.InvoiceCategories
                .AnyAsync(x => x.TenantId == tenantContext.TenantId && x.Code == code);

            if (duplicateCode)
            {
                return BadRequest(new
                {
                    message = "Invoice category code already exists."
                });
            }

            var category = new InvoiceCategory
            {
                TenantId = tenantContext.TenantId,
                Code = code,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                IsActive = true
            };

            dbContext.InvoiceCategories.Add(category);

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("InvoiceCategory", category.Id.ToString(), "Create", null, new { category.Code, category.Name }, "Created invoice category.");

            return CreatedAtAction(
                nameof(GetInvoiceCategory),
                new { id = category.Id },
                ToDto(category));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<InvoiceCategoryDto>> UpdateInvoiceCategory(
            int id,
            UpdateInvoiceCategoryRequest request)
        {
            var category = await dbContext.InvoiceCategories
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

            if (category is null)
            {
                return NotFound();
            }

            var code = request.Code.Trim();

            var duplicateCode = await dbContext.InvoiceCategories
                .AnyAsync(x =>
                    x.TenantId == tenantContext.TenantId &&
                    x.Id != id &&
                    x.Code == code);

            if (duplicateCode)
            {
                return BadRequest(new
                {
                    message = "Invoice category code already exists."
                });
            }

            category.Code = code;
            category.Name = request.Name.Trim();
            category.Description = request.Description?.Trim();
            category.IsActive = request.IsActive;

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("InvoiceCategory", category.Id.ToString(), "Update", null, request, "Updated invoice category.");

            return Ok(ToDto(category));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeactivateInvoiceCategory(int id)
        {
            var category = await dbContext.InvoiceCategories
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

            if (category is null)
            {
                return NotFound();
            }

            category.IsActive = false;

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("InvoiceCategory", id.ToString(), "Deactivate", null, null, "Deactivated invoice category.");

            return NoContent();
        }

        private static InvoiceCategoryDto ToDto(InvoiceCategory category)
        {
            return new InvoiceCategoryDto
            {
                Id = category.Id,
                Code = category.Code,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive
            };
        }
    }
}
