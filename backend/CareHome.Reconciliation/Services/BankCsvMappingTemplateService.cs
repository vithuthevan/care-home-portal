using System.Text.Json;
using CareHome.Api.Data;
using CareHome.Api.Models;
using CareHome.Api.Reconciliation.Domain;
using CareHome.Api.Reconciliation.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Reconciliation.Services;

public sealed class BankCsvMappingTemplateService(CareHomeDbContext dbContext, TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<List<BankCsvMappingTemplateDto>> ListAsync(
        int tenantId,
        Guid? bankAccountPublicId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.BankCsvMappingTemplates.AsNoTracking()
            .Where(t => t.TenantId == tenantId);

        if (bankAccountPublicId is Guid acctId)
        {
            query = query.Where(t =>
                t.BankAccount == null || t.BankAccount.PublicId == acctId);
        }

        return await query
            .OrderBy(t => t.Name)
            .Select(t => new BankCsvMappingTemplateDto
            {
                PublicId = t.PublicId,
                Name = t.Name,
                BankAccountPublicId = t.BankAccount != null ? t.BankAccount.PublicId : null,
                ColumnMapping = Deserialize(t.MappingJson)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BankCsvMappingTemplateDto> SaveAsync(
        int tenantId,
        SaveBankCsvMappingTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            throw new InvalidOperationException("Template name is required.");
        }

        int? bankAccountId = null;
        if (request.BankAccountPublicId is Guid acctPublicId)
        {
            var acct = await dbContext.BankAccounts.AsNoTracking()
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PublicId == acctPublicId, cancellationToken)
                ?? throw new InvalidOperationException("Bank account not found.");
            bankAccountId = acct.Id;
        }

        var now = timeProvider.GetUtcNow();
        var json = JsonSerializer.Serialize(request.ColumnMapping, JsonOptions);

        BankCsvMappingTemplate entity;
        if (request.PublicId is Guid publicId)
        {
            entity = await dbContext.BankCsvMappingTemplates
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.PublicId == publicId, cancellationToken)
                ?? throw new InvalidOperationException("Template not found.");
            entity.Name = name;
            entity.BankAccountId = bankAccountId;
            entity.MappingJson = json;
            entity.UpdatedAt = now;
        }
        else
        {
            entity = new BankCsvMappingTemplate
            {
                TenantId = tenantId,
                PublicId = Guid.NewGuid(),
                Name = name,
                BankAccountId = bankAccountId,
                MappingJson = json,
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.BankCsvMappingTemplates.Add(entity);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new BankCsvMappingTemplateDto
        {
            PublicId = entity.PublicId,
            Name = entity.Name,
            BankAccountPublicId = request.BankAccountPublicId,
            ColumnMapping = request.ColumnMapping
        };
    }

    public async Task DeleteAsync(int tenantId, Guid publicId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.BankCsvMappingTemplates
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.PublicId == publicId, cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.BankCsvMappingTemplates.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static BankCsvColumnMapping Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<BankCsvColumnMapping>(json, JsonOptions) ?? new BankCsvColumnMapping();
        }
        catch
        {
            return new BankCsvColumnMapping();
        }
    }
}
