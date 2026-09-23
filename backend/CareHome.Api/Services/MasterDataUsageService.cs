using CareHome.Api.Data;
using CareHome.Api.Dtos.Common;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Services;

public sealed class MasterDataUsageService(CareHomeDbContext dbContext)
{
    public async Task<MasterDataUsageDto> GetNominalCodeUsageAsync(
        int tenantId,
        int nominalCodeId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var trimmedCode = code.Trim();
        var contractCount = await dbContext.ClientFundingContracts
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.NominalCodeId == nominalCodeId, cancellationToken);

        var miscCount = await dbContext.MiscCharges
            .AsNoTracking()
            .CountAsync(
                x => x.TenantId == tenantId && x.NominalCodeId == nominalCodeId,
                cancellationToken);

        var lineCount = await dbContext.InvoiceLines
            .AsNoTracking()
            .Where(x => x.SnapshotNominalCode == trimmedCode)
            .Where(x => x.Invoice.TenantId == tenantId)
            .CountAsync(cancellationToken);

        return new MasterDataUsageDto
        {
            FundingContractCount = contractCount,
            MiscChargeCount = miscCount,
            InvoiceLineSnapshotCount = lineCount
        };
    }

    public async Task<MasterDataUsageDto> GetFundingAuthorityUsageAsync(
        int tenantId,
        int fundingAuthorityId,
        CancellationToken cancellationToken = default)
    {
        var contractCount = await dbContext.ClientFundingContracts
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.FundingAuthorityId == fundingAuthorityId, cancellationToken);

        var invoiceCount = await dbContext.Invoices
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.FundingAuthorityId == fundingAuthorityId, cancellationToken);

        return new MasterDataUsageDto
        {
            FundingContractCount = contractCount,
            InvoiceCount = invoiceCount
        };
    }

    public async Task<MasterDataUsageDto> GetInvoiceCategoryUsageAsync(
        int tenantId,
        int invoiceCategoryId,
        CancellationToken cancellationToken = default)
    {
        var contractCount = await dbContext.ClientFundingContracts
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.InvoiceCategoryId == invoiceCategoryId, cancellationToken);

        var templateCount = await dbContext.InvoiceTemplates
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.InvoiceCategoryId == invoiceCategoryId, cancellationToken);

        var invoiceCount = await dbContext.Invoices
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.InvoiceCategoryId == invoiceCategoryId, cancellationToken);

        return new MasterDataUsageDto
        {
            FundingContractCount = contractCount,
            InvoiceTemplateCount = templateCount,
            InvoiceCount = invoiceCount
        };
    }

    public async Task<MasterDataUsageDto> GetInvoiceTemplateUsageAsync(
        int tenantId,
        int invoiceTemplateId,
        CancellationToken cancellationToken = default)
    {
        var pinnedContractCount = await dbContext.ClientFundingContracts
            .AsNoTracking()
            .CountAsync(
                x => x.TenantId == tenantId && x.InvoiceTemplateId == invoiceTemplateId,
                cancellationToken);

        var invoiceCount = await dbContext.Invoices
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.InvoiceTemplateId == invoiceTemplateId, cancellationToken);

        return new MasterDataUsageDto
        {
            PinnedContractCount = pinnedContractCount,
            InvoiceCount = invoiceCount
        };
    }

    public async Task<Dictionary<int, MasterDataUsageDto>> GetNominalCodeUsagesAsync(
        int tenantId,
        IReadOnlyList<(int Id, string Code)> codes,
        CancellationToken cancellationToken = default)
    {
        if (codes.Count == 0)
        {
            return new Dictionary<int, MasterDataUsageDto>();
        }

        var ids = codes.Select(x => x.Id).ToList();
        var contractCounts = await dbContext.ClientFundingContracts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && ids.Contains(x.NominalCodeId))
            .GroupBy(x => x.NominalCodeId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        var miscCounts = await dbContext.MiscCharges
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.NominalCodeId != null && ids.Contains(x.NominalCodeId.Value))
            .GroupBy(x => x.NominalCodeId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        var codeLookup = codes.ToDictionary(x => x.Id, x => x.Code.Trim());
        var snapshotCodes = codeLookup.Values.Where(x => x.Length > 0).Distinct().ToList();
        var lineCountsByCode = await dbContext.InvoiceLines
            .AsNoTracking()
            .Where(x => x.Invoice.TenantId == tenantId && snapshotCodes.Contains(x.SnapshotNominalCode))
            .GroupBy(x => x.SnapshotNominalCode)
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Code, x => x.Count, cancellationToken);

        var result = new Dictionary<int, MasterDataUsageDto>();
        foreach (var (id, code) in codes)
        {
            lineCountsByCode.TryGetValue(code.Trim(), out var lineCount);
            contractCounts.TryGetValue(id, out var contractCount);
            miscCounts.TryGetValue(id, out var miscCount);
            result[id] = new MasterDataUsageDto
            {
                FundingContractCount = contractCount,
                MiscChargeCount = miscCount,
                InvoiceLineSnapshotCount = lineCount
            };
        }

        return result;
    }

    public async Task<Dictionary<int, MasterDataUsageDto>> GetFundingAuthorityUsagesAsync(
        int tenantId,
        IReadOnlyList<int> authorityIds,
        CancellationToken cancellationToken = default)
    {
        if (authorityIds.Count == 0)
        {
            return new Dictionary<int, MasterDataUsageDto>();
        }

        var contractCounts = await dbContext.ClientFundingContracts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && authorityIds.Contains(x.FundingAuthorityId))
            .GroupBy(x => x.FundingAuthorityId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        var invoiceCounts = await dbContext.Invoices
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && authorityIds.Contains(x.FundingAuthorityId))
            .GroupBy(x => x.FundingAuthorityId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        return authorityIds.ToDictionary(
            id => id,
            id => new MasterDataUsageDto
            {
                FundingContractCount = contractCounts.GetValueOrDefault(id),
                InvoiceCount = invoiceCounts.GetValueOrDefault(id)
            });
    }

    public async Task<Dictionary<int, MasterDataUsageDto>> GetInvoiceCategoryUsagesAsync(
        int tenantId,
        IReadOnlyList<int> categoryIds,
        CancellationToken cancellationToken = default)
    {
        if (categoryIds.Count == 0)
        {
            return new Dictionary<int, MasterDataUsageDto>();
        }

        var contractCounts = await dbContext.ClientFundingContracts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && categoryIds.Contains(x.InvoiceCategoryId))
            .GroupBy(x => x.InvoiceCategoryId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        var templateCounts = await dbContext.InvoiceTemplates
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && categoryIds.Contains(x.InvoiceCategoryId))
            .GroupBy(x => x.InvoiceCategoryId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        var invoiceCounts = await dbContext.Invoices
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && categoryIds.Contains(x.InvoiceCategoryId))
            .GroupBy(x => x.InvoiceCategoryId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        return categoryIds.ToDictionary(
            id => id,
            id => new MasterDataUsageDto
            {
                FundingContractCount = contractCounts.GetValueOrDefault(id),
                InvoiceTemplateCount = templateCounts.GetValueOrDefault(id),
                InvoiceCount = invoiceCounts.GetValueOrDefault(id)
            });
    }

    public async Task<Dictionary<int, MasterDataUsageDto>> GetInvoiceTemplateUsagesAsync(
        int tenantId,
        IReadOnlyList<int> templateIds,
        CancellationToken cancellationToken = default)
    {
        if (templateIds.Count == 0)
        {
            return new Dictionary<int, MasterDataUsageDto>();
        }

        var pinnedCounts = await dbContext.ClientFundingContracts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.InvoiceTemplateId != null && templateIds.Contains(x.InvoiceTemplateId.Value))
            .GroupBy(x => x.InvoiceTemplateId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        var invoiceCounts = await dbContext.Invoices
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.InvoiceTemplateId != null && templateIds.Contains(x.InvoiceTemplateId.Value))
            .GroupBy(x => x.InvoiceTemplateId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        return templateIds.ToDictionary(
            id => id,
            id => new MasterDataUsageDto
            {
                PinnedContractCount = pinnedCounts.GetValueOrDefault(id),
                InvoiceCount = invoiceCounts.GetValueOrDefault(id)
            });
    }
}
