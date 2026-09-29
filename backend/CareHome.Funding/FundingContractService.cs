using CareHome.Api.Abstractions;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.FundingContracts;
using CareHome.Api.Funding.Contracts;
using CareHome.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Funding;

public sealed record FundingOperationError(string Message, string? Code = null);

public class FundingContractService(
    CareHomeDbContext dbContext,
    IFundingContractQuery fundingContractQuery,
    ICareHomeAccessScope userAccess,
    IAuditWriter audit)
{
    public async Task<List<FundingContractDto>?> ListForClientAsync(
        int tenantId,
        int clientId,
        CancellationToken cancellationToken = default)
    {
        if (!await CanAccessClientAsync(tenantId, clientId, cancellationToken))
        {
            return null;
        }

        var contracts = await dbContext.ClientFundingContracts
            .AsNoTracking()
            .Include(x => x.FundingAuthority)
            .Include(x => x.InvoiceCategory)
            .Include(x => x.NominalCode)
            .Include(x => x.InvoiceTemplate)
            .Include(x => x.Rates)
            .Where(x => x.ClientId == clientId && x.TenantId == tenantId)
            .OrderByDescending(x => x.ContractStartDate)
            .ToListAsync(cancellationToken);

        return contracts.Select(Map).ToList();
    }

    public async Task<FundingContractDto?> GetAsync(
        int tenantId,
        int contractId,
        CancellationToken cancellationToken = default)
    {
        if (!await CanAccessContractAsync(tenantId, contractId, cancellationToken))
        {
            return null;
        }

        return await LoadDtoAsync(tenantId, contractId, cancellationToken);
    }

    public async Task<(FundingContractDto? Contract, FundingOperationError? Error, bool ClientMissing)> CreateAsync(
        int tenantId,
        int clientId,
        CreateFundingContractRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = await dbContext.Clients.FirstOrDefaultAsync(
            x => x.Id == clientId && x.TenantId == tenantId,
            cancellationToken);
        if (client is null)
        {
            return (null, null, true);
        }

        if (!await userAccess.CanAccessCareHomeAsync(tenantId, client.CareHomeId, cancellationToken))
        {
            return (null, null, true);
        }

        var validation = ValidateContractDates(request.ContractStartDate, request.ContractEndDate);
        if (validation is not null)
        {
            return (null, validation, false);
        }

        var relatedError = await EnsureRelatedEntitiesAsync(
            tenantId,
            request.FundingAuthorityId,
            request.InvoiceCategoryId,
            request.NominalCodeId,
            request.InvoiceTemplateId,
            cancellationToken);
        if (relatedError is not null)
        {
            return (null, relatedError, false);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await SqlAppLock.AcquireExclusiveAsync(
            dbContext.Database,
            FundingContractLockResource(
                tenantId,
                clientId,
                request.FundingAuthorityId,
                request.InvoiceCategoryId));

        if (await fundingContractQuery.HasOverlappingActiveContractAsync(
                tenantId,
                clientId,
                request.FundingAuthorityId,
                request.InvoiceCategoryId,
                request.ContractStartDate,
                request.ContractEndDate,
                excludeContractId: null,
                cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return (null, OverlapError(), false);
        }

        var now = DateTimeOffset.UtcNow;
        var contract = new ClientFundingContract
        {
            TenantId = tenantId,
            ClientId = clientId,
            FundingAuthorityId = request.FundingAuthorityId,
            InvoiceCategoryId = request.InvoiceCategoryId,
            NominalCodeId = request.NominalCodeId,
            InvoiceTemplateId = request.InvoiceTemplateId,
            ContractStartDate = request.ContractStartDate,
            ContractEndDate = request.ContractEndDate,
            Status = FundingContractStatuses.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.ClientFundingContracts.Add(contract);
        await dbContext.SaveChangesAsync(cancellationToken);
        await audit.LogAsync(
            "ClientFundingContract",
            contract.Id.ToString(),
            "Create",
            null,
            request,
            "Created funding contract.",
            cancellationToken,
            tenantId);
        await transaction.CommitAsync(cancellationToken);

        var dto = await LoadDtoAsync(tenantId, contract.Id, cancellationToken);
        return (dto, null, false);
    }

    public async Task<(FundingContractDto? Contract, FundingOperationError? Error, bool Missing)> UpdateAsync(
        int tenantId,
        int contractId,
        UpdateFundingContractRequest request,
        CancellationToken cancellationToken = default)
    {
        var contract = await dbContext.ClientFundingContracts.FirstOrDefaultAsync(
            x => x.Id == contractId && x.TenantId == tenantId,
            cancellationToken);
        if (contract is null)
        {
            return (null, null, true);
        }

        if (!await CanAccessClientAsync(tenantId, contract.ClientId, cancellationToken))
        {
            return (null, null, true);
        }

        var validation = ValidateContractDates(request.ContractStartDate, request.ContractEndDate);
        if (validation is not null)
        {
            return (null, validation, false);
        }

        var relatedError = await EnsureRelatedEntitiesAsync(
            tenantId,
            request.FundingAuthorityId,
            request.InvoiceCategoryId,
            request.NominalCodeId,
            request.InvoiceTemplateId,
            cancellationToken);
        if (relatedError is not null)
        {
            return (null, relatedError, false);
        }

        if (await fundingContractQuery.IsContractUsedOnNonVoidInvoiceAsync(tenantId, contractId, cancellationToken)
            && (contract.FundingAuthorityId != request.FundingAuthorityId
                || contract.InvoiceCategoryId != request.InvoiceCategoryId
                || contract.NominalCodeId != request.NominalCodeId
                || contract.ContractStartDate != request.ContractStartDate))
        {
            return (null, new FundingOperationError(
                "This contract has been used on finalized invoices. Historical fields cannot be changed. Add a new contract or close this one instead."),
                false);
        }

        if (request.Status is not FundingContractStatuses.Active and not FundingContractStatuses.Inactive)
        {
            return (null, new FundingOperationError("Status must be Active or Inactive."), false);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var previousResource = FundingContractLockResource(
            tenantId,
            contract.ClientId,
            contract.FundingAuthorityId,
            contract.InvoiceCategoryId);
        var nextResource = FundingContractLockResource(
            tenantId,
            contract.ClientId,
            request.FundingAuthorityId,
            request.InvoiceCategoryId);

        foreach (var resource in new[] { previousResource, nextResource }
                     .Distinct(StringComparer.Ordinal)
                     .OrderBy(x => x, StringComparer.Ordinal))
        {
            await SqlAppLock.AcquireExclusiveAsync(dbContext.Database, resource);
        }

        if (request.Status == FundingContractStatuses.Active
            && await fundingContractQuery.HasOverlappingActiveContractAsync(
                tenantId,
                contract.ClientId,
                request.FundingAuthorityId,
                request.InvoiceCategoryId,
                request.ContractStartDate,
                request.ContractEndDate,
                excludeContractId: contract.Id,
                cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return (null, OverlapError(), false);
        }

        contract.FundingAuthorityId = request.FundingAuthorityId;
        contract.InvoiceCategoryId = request.InvoiceCategoryId;
        contract.NominalCodeId = request.NominalCodeId;
        contract.InvoiceTemplateId = request.InvoiceTemplateId;
        contract.ContractStartDate = request.ContractStartDate;
        contract.ContractEndDate = request.ContractEndDate;
        contract.Status = request.Status;
        contract.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await audit.LogAsync(
            "ClientFundingContract",
            contractId.ToString(),
            "Update",
            null,
            request,
            "Updated funding contract.",
            cancellationToken,
            tenantId);
        await transaction.CommitAsync(cancellationToken);

        var dto = await LoadDtoAsync(tenantId, contractId, cancellationToken);
        return (dto, null, false);
    }

    public async Task<List<FundingRateDto>?> ListRatesAsync(
        int tenantId,
        int contractId,
        CancellationToken cancellationToken = default)
    {
        if (!await CanAccessContractAsync(tenantId, contractId, cancellationToken))
        {
            return null;
        }

        return await dbContext.FundingRates.AsNoTracking()
            .Where(x => x.ClientFundingContractId == contractId)
            .OrderBy(x => x.EffectiveFrom)
            .Select(x => new FundingRateDto
            {
                Id = x.Id,
                ClientFundingContractId = x.ClientFundingContractId,
                EffectiveFrom = x.EffectiveFrom,
                EffectiveTo = x.EffectiveTo,
                Frequency = x.Frequency,
                Amount = x.Amount,
                Notes = x.Notes
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(FundingRateDto? Rate, FundingOperationError? Error, bool Missing)> AddRateAsync(
        int tenantId,
        int contractId,
        CreateFundingRateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            return (null, new FundingOperationError("Rate amount must be greater than zero."), false);
        }

        if (!RateFrequencies.All.Contains(request.Frequency))
        {
            return (null, new FundingOperationError("Frequency must be Daily, Weekly, or Monthly."), false);
        }

        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value < request.EffectiveFrom)
        {
            return (null, new FundingOperationError("Effective to cannot be before effective from."), false);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await SqlAppLock.AcquireExclusiveAsync(
            dbContext.Database,
            $"funding-rate-{tenantId}-{contractId}");

        var contract = await dbContext.ClientFundingContracts
            .Include(x => x.Rates)
            .FirstOrDefaultAsync(x => x.Id == contractId && x.TenantId == tenantId, cancellationToken);

        if (contract is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (null, null, true);
        }

        if (!await CanAccessClientAsync(tenantId, contract.ClientId, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return (null, null, true);
        }

        if (request.ClosePreviousOpenEnded)
        {
            var open = contract.Rates
                .Where(x => x.EffectiveTo == null)
                .OrderByDescending(x => x.EffectiveFrom)
                .FirstOrDefault();

            if (open is not null && open.EffectiveFrom < request.EffectiveFrom)
            {
                var closedTo = request.EffectiveFrom.AddDays(-1);
                if (closedTo < open.EffectiveFrom)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return (null, new FundingOperationError(
                        "Closing the previous open-ended rate would make its period invalid."), false);
                }

                open.EffectiveTo = closedTo;
            }
        }

        var overlap = contract.Rates.Any(existing =>
            DateRanges.Overlaps(
                existing.EffectiveFrom,
                existing.EffectiveTo,
                request.EffectiveFrom,
                request.EffectiveTo));

        if (overlap)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (null, new FundingOperationError(
                "This rate period overlaps an existing rate on the same contract."), false);
        }

        var rate = new FundingRate
        {
            ClientFundingContractId = contractId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Frequency = request.Frequency,
            Amount = Money.Round(request.Amount),
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.FundingRates.Add(rate);
        await dbContext.SaveChangesAsync(cancellationToken);
        await audit.LogAsync(
            "FundingRate",
            rate.Id.ToString(),
            "Create",
            null,
            request,
            "Added funding rate.",
            cancellationToken,
            tenantId);
        await transaction.CommitAsync(cancellationToken);

        return (new FundingRateDto
        {
            Id = rate.Id,
            ClientFundingContractId = rate.ClientFundingContractId,
            EffectiveFrom = rate.EffectiveFrom,
            EffectiveTo = rate.EffectiveTo,
            Frequency = rate.Frequency,
            Amount = rate.Amount,
            Notes = rate.Notes
        }, null, false);
    }

    private async Task<bool> CanAccessClientAsync(int tenantId, int clientId, CancellationToken cancellationToken)
    {
        var client = await dbContext.Clients.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == clientId && x.TenantId == tenantId, cancellationToken);
        if (client is null)
        {
            return false;
        }

        return await userAccess.CanAccessCareHomeAsync(tenantId, client.CareHomeId, cancellationToken);
    }

    private async Task<bool> CanAccessContractAsync(int tenantId, int contractId, CancellationToken cancellationToken)
    {
        var clientId = await dbContext.ClientFundingContracts.AsNoTracking()
            .Where(x => x.Id == contractId && x.TenantId == tenantId)
            .Select(x => (int?)x.ClientId)
            .FirstOrDefaultAsync(cancellationToken);
        if (clientId is null)
        {
            return false;
        }

        return await CanAccessClientAsync(tenantId, clientId.Value, cancellationToken);
    }

    private async Task<FundingContractDto?> LoadDtoAsync(
        int tenantId,
        int id,
        CancellationToken cancellationToken)
    {
        var contract = await dbContext.ClientFundingContracts.AsNoTracking()
            .Include(x => x.FundingAuthority)
            .Include(x => x.InvoiceCategory)
            .Include(x => x.NominalCode)
            .Include(x => x.InvoiceTemplate)
            .Include(x => x.Rates)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        return contract is null ? null : Map(contract);
    }

    private async Task<FundingOperationError?> EnsureRelatedEntitiesAsync(
        int tenantId,
        int fundingAuthorityId,
        int invoiceCategoryId,
        int nominalCodeId,
        int? invoiceTemplateId,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.FundingAuthorities.AnyAsync(
                x => x.Id == fundingAuthorityId && x.TenantId == tenantId,
                cancellationToken))
        {
            return new FundingOperationError("Funding authority was not found in this organisation.");
        }

        if (!await dbContext.InvoiceCategories.AnyAsync(
                x => x.Id == invoiceCategoryId && x.TenantId == tenantId,
                cancellationToken))
        {
            return new FundingOperationError("Invoice category was not found in this organisation.");
        }

        if (!await dbContext.NominalCodes.AnyAsync(
                x => x.Id == nominalCodeId && x.TenantId == tenantId,
                cancellationToken))
        {
            return new FundingOperationError("Nominal code was not found in this organisation.");
        }

        if (invoiceTemplateId is int templateId)
        {
            var template = await dbContext.InvoiceTemplates.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == templateId && x.TenantId == tenantId, cancellationToken);
            if (template is null)
            {
                return new FundingOperationError("Invoice template was not found in this organisation.");
            }

            if (!template.IsActive)
            {
                return new FundingOperationError("Invoice template must be active.");
            }

            if (template.InvoiceCategoryId != invoiceCategoryId)
            {
                return new FundingOperationError("Invoice template must belong to the same invoice category as the contract.");
            }
        }

        return null;
    }

    private static FundingOperationError OverlapError() =>
        new(FundingContractOverlap.ConflictMessage, FundingContractOverlap.ConflictCode);

    private static FundingOperationError? ValidateContractDates(DateOnly start, DateOnly? end) =>
        end.HasValue && end.Value < start
            ? new FundingOperationError("Contract end date cannot be before start date.")
            : null;

    private static string FundingContractLockResource(
        int tenantId,
        int clientId,
        int fundingAuthorityId,
        int invoiceCategoryId) =>
        $"funding-contract-{tenantId}-{clientId}-{fundingAuthorityId}-{invoiceCategoryId}";

    private static FundingContractDto Map(ClientFundingContract x) =>
        new()
        {
            Id = x.Id,
            ClientId = x.ClientId,
            FundingAuthorityId = x.FundingAuthorityId,
            FundingAuthorityPublicId = x.FundingAuthority.PublicId,
            FundingAuthorityName = x.FundingAuthority.Name,
            InvoiceCategoryId = x.InvoiceCategoryId,
            InvoiceCategoryName = x.InvoiceCategory.Name,
            NominalCodeId = x.NominalCodeId,
            NominalCode = x.NominalCode.Code,
            InvoiceTemplateId = x.InvoiceTemplateId,
            InvoiceTemplateName = x.InvoiceTemplate?.Name,
            ContractStartDate = x.ContractStartDate,
            ContractEndDate = x.ContractEndDate,
            Status = x.Status,
            Rates = x.Rates.OrderBy(r => r.EffectiveFrom).Select(r => new FundingRateDto
            {
                Id = r.Id,
                ClientFundingContractId = r.ClientFundingContractId,
                EffectiveFrom = r.EffectiveFrom,
                EffectiveTo = r.EffectiveTo,
                Frequency = r.Frequency,
                Amount = r.Amount,
                Notes = r.Notes
            }).ToList()
        };
}
