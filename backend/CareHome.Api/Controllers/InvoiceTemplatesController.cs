using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Documents;
using CareHome.Api.Dtos.Common;
using CareHome.Api.Dtos.InvoiceTemplates;
using CareHome.Api.Models;
using CareHome.Api.Security;
using CareHome.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/invoice-templates")]
[RequireTenant]
public class InvoiceTemplatesController(
    CareHomeDbContext dbContext,
    ITenantContext tenantContext,
    IDocumentStore documents,
    AuditService audit,
    MasterDataUsageService usage) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<InvoiceTemplateDto>>> List()
    {
        var tenantId = tenantContext.TenantId;
        var templates = await dbContext.InvoiceTemplates.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Include(x => x.InvoiceCategory)
            .Include(x => x.FundingAuthority)
            .Include(x => x.CareHome)
            .Include(x => x.Company)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var usageMap = await usage.GetInvoiceTemplateUsagesAsync(
            tenantId,
            templates.Select(x => x.Id).ToList());

        return Ok(templates.Select(x => ToDto(x, usageMap.GetValueOrDefault(x.Id))).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceTemplateDto>> Get(int id)
    {
        var tenantId = tenantContext.TenantId;
        var template = await dbContext.InvoiceTemplates.AsNoTracking()
            .Include(x => x.InvoiceCategory)
            .Include(x => x.FundingAuthority)
            .Include(x => x.CareHome)
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

        if (template is null)
        {
            return NotFound();
        }

        var usageDto = await usage.GetInvoiceTemplateUsageAsync(tenantId, template.Id);
        return Ok(ToDto(template, usageDto));
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceTemplateDto>> Create(UpsertInvoiceTemplateRequest request)
    {
        var tenantId = tenantContext.TenantId;
        var relatedError = await EnsureRelatedEntities(tenantId, request);
        if (relatedError is not null)
        {
            return relatedError;
        }

        var template = FromRequest(tenantId, request);
        dbContext.InvoiceTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        await audit.LogAsync("InvoiceTemplate", template.Id.ToString(), "Create", null, new { template.Name }, "Created invoice template.");
        var created = await dbContext.InvoiceTemplates
            .Include(x => x.InvoiceCategory)
            .Include(x => x.FundingAuthority)
            .Include(x => x.CareHome)
            .Include(x => x.Company)
            .FirstAsync(x => x.Id == template.Id);
        var createdUsage = await usage.GetInvoiceTemplateUsageAsync(tenantId, template.Id);
        return CreatedAtAction(nameof(Get), new { id = template.Id }, ToDto(created, createdUsage));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<InvoiceTemplateDto>> Update(int id, UpsertInvoiceTemplateRequest request)
    {
        var tenantId = tenantContext.TenantId;
        var template = await dbContext.InvoiceTemplates.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (template is null)
        {
            return NotFound();
        }

        var relatedError = await EnsureRelatedEntities(tenantId, request);
        if (relatedError is not null)
        {
            return relatedError;
        }

        template.Name = request.Name.Trim();
        template.InvoiceCategoryId = request.InvoiceCategoryId;
        template.FundingAuthorityId = request.FundingAuthorityId;
        template.CareHomeId = request.CareHomeId;
        template.CompanyId = request.CompanyId;
        template.HeaderText1 = LimitedHtml.Sanitize(request.HeaderText1);
        template.HeaderText2 = request.HeaderText2?.Trim();
        template.FooterText = LimitedHtml.Sanitize(request.FooterText);
        template.ContactName = request.ContactName?.Trim();
        template.ContactJobTitle = request.ContactJobTitle?.Trim();
        template.ContactEmail = request.ContactEmail?.Trim();
        template.ContactPhone = request.ContactPhone?.Trim();
        template.EmailSubjectTemplate = request.EmailSubjectTemplate?.Trim();
        template.EmailBodyTemplate = request.EmailBodyTemplate?.Trim();
        template.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync();
        await audit.LogAsync("InvoiceTemplate", id.ToString(), "Update", null, request, "Updated invoice template.");
        var updated = await dbContext.InvoiceTemplates
            .Include(x => x.InvoiceCategory)
            .Include(x => x.FundingAuthority)
            .Include(x => x.CareHome)
            .Include(x => x.Company)
            .FirstAsync(x => x.Id == id);
        var updatedUsage = await usage.GetInvoiceTemplateUsageAsync(tenantId, id);
        return Ok(ToDto(updated, updatedUsage));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var tenantId = tenantContext.TenantId;
        var template = await dbContext.InvoiceTemplates.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (template is null)
        {
            return NotFound();
        }

        template.IsActive = false;
        await dbContext.SaveChangesAsync();
        await audit.LogAsync("InvoiceTemplate", id.ToString(), "Deactivate", null, null, "Deactivated invoice template.");
        return NoContent();
    }

    [HttpGet("{id:int}/logo/{kind}")]
    public async Task<IActionResult> GetLogo(int id, string kind)
    {
        var tenantId = tenantContext.TenantId;
        var template = await dbContext.InvoiceTemplates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (template is null)
        {
            return NotFound();
        }

        var path = LogoPathForKind(template, kind);
        if (string.IsNullOrWhiteSpace(path))
        {
            return NotFound();
        }

        var bytes = await documents.ReadAsync(path);
        return bytes is null ? NotFound() : File(bytes, LogoStorage.ContentType(path));
    }

    [HttpPost("{id:int}/logo/{kind}")]
    [RequestSizeLimit(LogoStorage.MaxBytes)]
    public async Task<ActionResult<InvoiceTemplateDto>> UploadLogo(int id, string kind, IFormFile file)
    {
        if (!IsValidLogoKind(kind))
        {
            return BadRequest(new { message = "Logo kind must be 'company' or 'authority'." });
        }

        var validationError = LogoStorage.Validate(file);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var tenantId = tenantContext.TenantId;
        var template = await dbContext.InvoiceTemplates.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (template is null)
        {
            return NotFound();
        }

        var extension = LogoStorage.Extension(file)!;
        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        var tenantPublicId = await dbContext.Tenants
            .Where(x => x.Id == tenantId)
            .Select(x => x.PublicId)
            .FirstAsync();
        var folder = TenantDocumentPaths.Folder(tenantPublicId, "invoice-template-logos");
        var fileName = $"template-{id}-{kind}{extension}";
        var savedPath = await documents.SaveAsync(folder, fileName, buffer.ToArray());

        if (string.Equals(kind, "company", StringComparison.OrdinalIgnoreCase))
        {
            template.CompanyLogoPath = savedPath;
        }
        else
        {
            template.AuthorityLogoPath = savedPath;
        }

        await dbContext.SaveChangesAsync();
        await audit.LogAsync(
            "InvoiceTemplate",
            id.ToString(),
            "UploadLogo",
            null,
            new { kind, savedPath },
            "Uploaded invoice template logo.");

        var updated = await dbContext.InvoiceTemplates
            .Include(x => x.InvoiceCategory)
            .Include(x => x.FundingAuthority)
            .Include(x => x.CareHome)
            .Include(x => x.Company)
            .FirstAsync(x => x.Id == id);
        var usageDto = await usage.GetInvoiceTemplateUsageAsync(tenantId, id);
        return Ok(ToDto(updated, usageDto));
    }

    [HttpDelete("{id:int}/logo/{kind}")]
    public async Task<ActionResult<InvoiceTemplateDto>> ClearLogo(int id, string kind)
    {
        if (!IsValidLogoKind(kind))
        {
            return BadRequest(new { message = "Logo kind must be 'company' or 'authority'." });
        }

        var tenantId = tenantContext.TenantId;
        var template = await dbContext.InvoiceTemplates.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);
        if (template is null)
        {
            return NotFound();
        }

        if (string.Equals(kind, "company", StringComparison.OrdinalIgnoreCase))
        {
            template.CompanyLogoPath = null;
        }
        else
        {
            template.AuthorityLogoPath = null;
        }

        await dbContext.SaveChangesAsync();
        var updated = await dbContext.InvoiceTemplates
            .Include(x => x.InvoiceCategory)
            .Include(x => x.FundingAuthority)
            .Include(x => x.CareHome)
            .Include(x => x.Company)
            .FirstAsync(x => x.Id == id);
        var usageDto = await usage.GetInvoiceTemplateUsageAsync(tenantId, id);
        return Ok(ToDto(updated, usageDto));
    }

    private async Task<ActionResult?> EnsureRelatedEntities(int tenantId, UpsertInvoiceTemplateRequest request)
    {
        var categoryExists = await dbContext.InvoiceCategories
            .AnyAsync(x => x.Id == request.InvoiceCategoryId && x.TenantId == tenantId);
        if (!categoryExists)
        {
            return BadRequest(new { message = "Invoice category was not found in this organisation." });
        }

        if (request.FundingAuthorityId is int authorityId
            && !await dbContext.FundingAuthorities.AnyAsync(x => x.Id == authorityId && x.TenantId == tenantId))
        {
            return BadRequest(new { message = "Funding authority was not found in this organisation." });
        }

        if (request.CareHomeId is int homeId
            && !await dbContext.CareHomes.AnyAsync(x => x.Id == homeId && x.TenantId == tenantId))
        {
            return BadRequest(new { message = "Care home was not found in this organisation." });
        }

        if (request.CompanyId is int companyId
            && !await dbContext.Companies.AnyAsync(x => x.Id == companyId && x.TenantId == tenantId))
        {
            return BadRequest(new { message = "Company was not found in this organisation." });
        }

        return null;
    }

    private static InvoiceTemplateDto ToDto(InvoiceTemplate x, MasterDataUsageDto? usageDto = null)
    {
        return new InvoiceTemplateDto
        {
            Id = x.Id,
            Name = x.Name,
            InvoiceCategoryId = x.InvoiceCategoryId,
            InvoiceCategoryName = x.InvoiceCategory.Name,
            FundingAuthorityId = x.FundingAuthorityId,
            FundingAuthorityName = x.FundingAuthority == null ? null : x.FundingAuthority.Name,
            CareHomeId = x.CareHomeId,
            CareHomeName = x.CareHome == null ? null : x.CareHome.Name,
            CompanyId = x.CompanyId,
            CompanyName = x.Company == null ? null : x.Company.Name,
            HeaderText1 = x.HeaderText1,
            HeaderText2 = x.HeaderText2,
            FooterText = x.FooterText,
            BankAccountName = x.BankAccountName,
            SortCode = x.SortCode,
            AccountNumber = x.AccountNumber,
            ContactName = x.ContactName,
            ContactJobTitle = x.ContactJobTitle,
            ContactEmail = x.ContactEmail,
            ContactPhone = x.ContactPhone,
            EmailSubjectTemplate = x.EmailSubjectTemplate,
            EmailBodyTemplate = x.EmailBodyTemplate,
            CompanyLogoPath = x.CompanyLogoPath,
            AuthorityLogoPath = x.AuthorityLogoPath,
            IsActive = x.IsActive,
            Usage = usageDto
        };
    }

    private static bool IsValidLogoKind(string kind) =>
        string.Equals(kind, "company", StringComparison.OrdinalIgnoreCase)
        || string.Equals(kind, "authority", StringComparison.OrdinalIgnoreCase);

    private static string? LogoPathForKind(InvoiceTemplate template, string kind) =>
        string.Equals(kind, "company", StringComparison.OrdinalIgnoreCase)
            ? template.CompanyLogoPath
            : string.Equals(kind, "authority", StringComparison.OrdinalIgnoreCase)
                ? template.AuthorityLogoPath
                : null;

    private static InvoiceTemplate FromRequest(int tenantId, UpsertInvoiceTemplateRequest request)
    {
        return new InvoiceTemplate
        {
            TenantId = tenantId,
            Name = request.Name.Trim(),
            InvoiceCategoryId = request.InvoiceCategoryId,
            FundingAuthorityId = request.FundingAuthorityId,
            CareHomeId = request.CareHomeId,
            CompanyId = request.CompanyId,
            HeaderText1 = LimitedHtml.Sanitize(request.HeaderText1),
            HeaderText2 = request.HeaderText2?.Trim(),
            FooterText = LimitedHtml.Sanitize(request.FooterText),
            ContactName = request.ContactName?.Trim(),
            ContactJobTitle = request.ContactJobTitle?.Trim(),
            ContactEmail = request.ContactEmail?.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
            EmailSubjectTemplate = request.EmailSubjectTemplate?.Trim(),
            EmailBodyTemplate = request.EmailBodyTemplate?.Trim(),
            IsActive = true
        };
    }
}

