using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Documents;
using CareHome.Api.Dtos.Companies;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequireTenant]
public class CompaniesController(
    CareHomeDbContext dbContext,
    ITenantContext tenantContext,
    IDocumentStore documents,
    AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetCompanies(
        string? search = null,
        int? page = null,
        int? pageSize = null)
    {
        var tenantId = tenantContext.TenantId;
        var query = dbContext.Companies
            .AsNoTracking()
            .ForTenant(tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(company => company.Name.Contains(value));
        }

        var projected = query
            .OrderBy(company => company.Name)
            .Select(company => new CompanyDto
            {
                Id = company.Id,
                PublicId = company.PublicId,
                Name = company.Name,
                Address = company.Address,
                Phone = company.Phone,
                Email = company.Email,
                LogoPath = company.LogoPath,
                IsActive = company.IsActive,
                CareHomeCount = company.CareHomes.Count,
                ActiveCareHomeCount = company.CareHomes.Count(x => x.IsActive),
                ResidentCount = company.CareHomes.SelectMany(x => x.Clients).Count(x => !x.IsArchived),
            });

        if (!Pagination.IsRequested(page, pageSize))
        {
            return Ok(await projected.ToListAsync());
        }

        var (p, ps) = Pagination.Normalize(page, pageSize);
        var total = await projected.CountAsync();
        var items = await projected.Skip((p - 1) * ps).Take(ps).ToListAsync();
        return Ok(new PagedResult<CompanyDto>
        {
            Items = items,
            TotalCount = total,
            Page = p,
            PageSize = ps
        });
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<CompanyDto>> GetCompany(string key)
    {
        if (!EntityRouteKey.TryParse(key, out var publicId, out var id))
        {
            return NotFound();
        }

        var tenantId = tenantContext.TenantId;
        var company = await dbContext.Companies
            .AsNoTracking()
            .Where(company => company.TenantId == tenantId)
            .Where(company => publicId != default ? company.PublicId == publicId : company.Id == id)
            .Select(company => new CompanyDto
            {
                Id = company.Id,
                PublicId = company.PublicId,
                Name = company.Name,
                Address = company.Address,
                Phone = company.Phone,
                Email = company.Email,
                LogoPath = company.LogoPath,
                IsActive = company.IsActive,
                CareHomeCount = company.CareHomes.Count,
                ActiveCareHomeCount = company.CareHomes.Count(x => x.IsActive),
                ResidentCount = company.CareHomes.SelectMany(x => x.Clients).Count(x => !x.IsArchived),
            })
            .FirstOrDefaultAsync();

        if (company is null)
        {
            return NotFound();
        }

        return Ok(company);
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> CreateCompany(
        CreateCompanyRequest request)
    {
        var tenantId = tenantContext.TenantId;
        var companyName = request.Name.Trim();

        var companyExists = await dbContext.Companies
            .AnyAsync(company =>
                company.TenantId == tenantId &&
                company.Name == companyName);

        if (companyExists)
        {
            return BadRequest(new
            {
                message = "A company with this name already exists."
            });
        }

        var company = new Company
        {
            TenantId = tenantId,
            Name = companyName,
            Address = Normalize(request.Address),
            Phone = Normalize(request.Phone),
            Email = OptionalContactFields.NormalizeEmail(request.Email),
            IsActive = true
        };

        dbContext.Companies.Add(company);

        await dbContext.SaveChangesAsync();
        await audit.LogAsync("Company", company.Id.ToString(), "Create", null, new { company.Name }, "Created company.");

        return CreatedAtAction(
            nameof(GetCompany),
            new { key = company.PublicId },
            ToDto(company));
    }

    [HttpPut("{key}")]
    public async Task<ActionResult<CompanyDto>> UpdateCompany(
        string key,
        UpdateCompanyRequest request)
    {
        if (!EntityRouteKey.TryParse(key, out var publicId, out var id))
        {
            return NotFound();
        }

        var tenantId = tenantContext.TenantId;
        var company = await dbContext.Companies
            .FirstOrDefaultAsync(company =>
                company.TenantId == tenantId &&
                (publicId != default ? company.PublicId == publicId : company.Id == id));

        if (company is null)
        {
            return NotFound();
        }

        var companyName = request.Name.Trim();

        var duplicateExists = await dbContext.Companies
            .AnyAsync(existingCompany =>
                existingCompany.TenantId == tenantId &&
                existingCompany.Id != company.Id &&
                existingCompany.Name == companyName);

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message = "A company with this name already exists."
            });
        }

        if (company.IsActive && !request.IsActive)
        {
            var deactivationError =
                await RejectIfDeactivatingWithActiveCareHomes(company.Id);

            if (deactivationError is not null)
            {
                return deactivationError;
            }
        }

        company.Name = companyName;
        company.Address = Normalize(request.Address);
        company.Phone = Normalize(request.Phone);
        company.Email = OptionalContactFields.NormalizeEmail(request.Email);
        company.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync();
        await audit.LogAsync("Company", company.Id.ToString(), "Update", null, request, "Updated company.");

        return Ok(ToDto(company));
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> DeactivateCompany(string key)
    {
        if (!EntityRouteKey.TryParse(key, out var publicId, out var id))
        {
            return NotFound();
        }

        var tenantId = tenantContext.TenantId;
        var company = await dbContext.Companies
            .FirstOrDefaultAsync(company =>
                company.TenantId == tenantId &&
                (publicId != default ? company.PublicId == publicId : company.Id == id));

        if (company is null)
        {
            return NotFound();
        }

        if (company.IsActive)
        {
            var deactivationError =
                await RejectIfDeactivatingWithActiveCareHomes(company.Id);

            if (deactivationError is not null)
            {
                return deactivationError;
            }
        }

        company.IsActive = false;

        await dbContext.SaveChangesAsync();
        await audit.LogAsync("Company", company.Id.ToString(), "Deactivate", null, null, "Deactivated company.");

        return NoContent();
    }

    [HttpGet("{key}/logo")]
    public async Task<IActionResult> GetLogo(string key)
    {
        var company = await FindCompanyAsync(key);
        if (company is null || string.IsNullOrWhiteSpace(company.LogoPath))
        {
            return NotFound();
        }

        var bytes = await documents.ReadAsync(company.LogoPath);
        if (bytes is null)
        {
            return NotFound();
        }

        return File(bytes, LogoStorage.ContentType(company.LogoPath));
    }

    [HttpPost("{key}/logo")]
    [RequestSizeLimit(LogoStorage.MaxBytes)]
    public async Task<ActionResult<CompanyDto>> UploadLogo(string key, IFormFile file)
    {
        var validationError = LogoStorage.Validate(file);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var company = await FindCompanyAsync(key, tracked: true);
        if (company is null)
        {
            return NotFound();
        }

        var extension = LogoStorage.Extension(file)!;
        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        var tenantPublicId = await TenantPublicIdAsync();
        company.LogoPath = await documents.SaveAsync(
            TenantDocumentPaths.Folder(tenantPublicId, "logos"),
            $"company-{company.PublicId:N}{extension}",
            buffer.ToArray());
        await dbContext.SaveChangesAsync();
        await audit.LogAsync("Company", company.Id.ToString(), "UploadLogo", null, new { company.LogoPath }, "Uploaded company logo.");
        return Ok(ToDto(company));
    }

    private async Task<ActionResult?> RejectIfDeactivatingWithActiveCareHomes(
        int companyId)
    {
        var hasActiveCareHomes =
            await dbContext.CareHomes.AnyAsync(x =>
                x.CompanyId == companyId &&
                x.IsActive);

        if (hasActiveCareHomes)
        {
            return BadRequest(new
            {
                message =
                    "Deactivate all care homes under this company before deactivating the company."
            });
        }

        return null;
    }

    private async Task<Company?> FindCompanyAsync(string key, bool tracked = false)
    {
        if (!EntityRouteKey.TryParse(key, out var publicId, out var id))
        {
            return null;
        }

        var query = tracked
            ? dbContext.Companies.AsQueryable()
            : dbContext.Companies.AsNoTracking();

        return await query.FirstOrDefaultAsync(company =>
            company.TenantId == tenantContext.TenantId &&
            (publicId != default ? company.PublicId == publicId : company.Id == id));
    }

    private async Task<Guid> TenantPublicIdAsync()
    {
        if (tenantContext.TenantPublicId is Guid publicId && publicId != Guid.Empty)
        {
            return publicId;
        }

        return await dbContext.Tenants
            .Where(x => x.Id == tenantContext.TenantId)
            .Select(x => x.PublicId)
            .FirstAsync();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CompanyDto ToDto(Company company)
    {
        return new CompanyDto
        {
            Id = company.Id,
            PublicId = company.PublicId,
            Name = company.Name,
            Address = company.Address,
            Phone = company.Phone,
            Email = company.Email,
            LogoPath = company.LogoPath,
            IsActive = company.IsActive
        };
    }
}
