using CareHome.Api.Audit;
using CareHome.Api.Billing;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.FundingContracts;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [RequireTenant]
    public class FundingContractsController(
        CareHomeDbContext dbContext,
        AuditService audit,
        ITenantContext tenantContext,
        UserAccessService userAccess) : ControllerBase
    {
        [HttpGet("api/clients/{clientId:int}/funding-contracts")]
        public async Task<ActionResult<List<FundingContractDto>>> GetForClient(int clientId)
        {
            var access = await EnsureClientAccessAsync(clientId);
            if (access is not null)
            {
                return access;
            }

            var contracts = await dbContext.ClientFundingContracts
                .AsNoTracking()
                .Include(x => x.FundingAuthority)
                .Include(x => x.InvoiceCategory)
                .Include(x => x.NominalCode)
                .Include(x => x.Rates)
                .Where(x => x.ClientId == clientId && x.TenantId == tenantContext.TenantId)
                .OrderByDescending(x => x.ContractStartDate)
                .ToListAsync();

            return Ok(contracts.Select(Map).ToList());
        }

        [HttpPost("api/clients/{clientId:int}/funding-contracts")]
        public async Task<ActionResult<FundingContractDto>> Create(int clientId, CreateFundingContractRequest request)
        {
            var client = await dbContext.Clients.FirstOrDefaultAsync(x => x.Id == clientId && x.TenantId == tenantContext.TenantId);
            if (client is null)
            {
                return NotFound();
            }

            if (!await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, client.CareHomeId))
            {
                return NotFound();
            }

            var error = ValidateContract(request);
            if (error is not null)
            {
                return error;
            }

            var relatedError = await EnsureRelatedEntities(request.FundingAuthorityId, request.InvoiceCategoryId, request.NominalCodeId, request.InvoiceTemplateId);
            if (relatedError is not null)
            {
                return relatedError;
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            await SqlAppLock.AcquireExclusiveAsync(
                dbContext.Database,
                FundingContractLockResource(
                    tenantContext.TenantId,
                    clientId,
                    request.FundingAuthorityId,
                    request.InvoiceCategoryId));

            var overlapError = await EnsureNoOverlappingContract(
                clientId,
                request.FundingAuthorityId,
                request.InvoiceCategoryId,
                request.ContractStartDate,
                request.ContractEndDate,
                excludeContractId: null);
            if (overlapError is not null)
            {
                await transaction.RollbackAsync();
                return overlapError;
            }

            var now = DateTimeOffset.UtcNow;
            var contract = new ClientFundingContract
            {
                TenantId = tenantContext.TenantId,
                ClientId = clientId,
                FundingAuthorityId = request.FundingAuthorityId,
                InvoiceCategoryId = request.InvoiceCategoryId,
                NominalCodeId = request.NominalCodeId,
                InvoiceTemplateId = request.InvoiceTemplateId,
                ContractStartDate = request.ContractStartDate,
                ContractEndDate = request.ContractEndDate,
                Status = "Active",
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.ClientFundingContracts.Add(contract);
            await dbContext.SaveChangesAsync();
            await audit.LogAsync("ClientFundingContract", contract.Id.ToString(), "Create", null, request, "Created funding contract.");
            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetOne), new { id = contract.Id }, await LoadDto(contract.Id));
        }

        [HttpGet("api/funding-contracts/{id:int}")]
        public async Task<ActionResult<FundingContractDto>> GetOne(int id)
        {
            var access = await EnsureContractAccessAsync(id);
            if (access is not null)
            {
                return access;
            }

            var dto = await LoadDto(id);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPut("api/funding-contracts/{id:int}")]
        public async Task<ActionResult<FundingContractDto>> Update(int id, UpdateFundingContractRequest request)
        {
            var contract = await dbContext.ClientFundingContracts.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);
            if (contract is null)
            {
                return NotFound();
            }

            if (!await CanAccessContractClientAsync(contract.ClientId))
            {
                return NotFound();
            }

            var error = ValidateContract(request);
            if (error is not null)
            {
                return error;
            }

            var relatedError = await EnsureRelatedEntities(request.FundingAuthorityId, request.InvoiceCategoryId, request.NominalCodeId, request.InvoiceTemplateId);
            if (relatedError is not null)
            {
                return relatedError;
            }

            var used = await dbContext.InvoiceLines.AnyAsync(x =>
                x.ClientFundingContractId == id && x.Invoice.Status != "Void");

            if (used &&
                (contract.FundingAuthorityId != request.FundingAuthorityId ||
                 contract.InvoiceCategoryId != request.InvoiceCategoryId ||
                 contract.NominalCodeId != request.NominalCodeId ||
                 contract.ContractStartDate != request.ContractStartDate))
            {
                return BadRequest(new
                {
                    message = "This contract has been used on finalized invoices. Historical fields cannot be changed. Add a new contract or close this one instead."
                });
            }

            if (request.Status is not "Active" and not "Inactive")
            {
                return BadRequest(new { message = "Status must be Active or Inactive." });
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            var previousResource = FundingContractLockResource(
                tenantContext.TenantId,
                contract.ClientId,
                contract.FundingAuthorityId,
                contract.InvoiceCategoryId);
            var nextResource = FundingContractLockResource(
                tenantContext.TenantId,
                contract.ClientId,
                request.FundingAuthorityId,
                request.InvoiceCategoryId);

            // Acquire in deterministic order to avoid deadlocks when two updates swap streams.
            foreach (var resource in new[] { previousResource, nextResource }
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal))
            {
                await SqlAppLock.AcquireExclusiveAsync(dbContext.Database, resource);
            }

            if (request.Status == "Active")
            {
                var overlapError = await EnsureNoOverlappingContract(
                    contract.ClientId,
                    request.FundingAuthorityId,
                    request.InvoiceCategoryId,
                    request.ContractStartDate,
                    request.ContractEndDate,
                    excludeContractId: contract.Id);
                if (overlapError is not null)
                {
                    await transaction.RollbackAsync();
                    return overlapError;
                }
            }

            contract.FundingAuthorityId = request.FundingAuthorityId;
            contract.InvoiceCategoryId = request.InvoiceCategoryId;
            contract.NominalCodeId = request.NominalCodeId;
            contract.InvoiceTemplateId = request.InvoiceTemplateId;
            contract.ContractStartDate = request.ContractStartDate;
            contract.ContractEndDate = request.ContractEndDate;
            contract.Status = request.Status;
            contract.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("ClientFundingContract", id.ToString(), "Update", null, request, "Updated funding contract.");
            await transaction.CommitAsync();
            return Ok(await LoadDto(id));
        }

        [HttpGet("api/funding-contracts/{id:int}/rates")]
        public async Task<ActionResult<List<FundingRateDto>>> GetRates(int id)
        {
            var access = await EnsureContractAccessAsync(id);
            if (access is not null)
            {
                return access;
            }

            var rates = await dbContext.FundingRates.AsNoTracking()
                .Where(x => x.ClientFundingContractId == id)
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
                .ToListAsync();

            return Ok(rates);
        }

        [HttpPost("api/funding-contracts/{id:int}/rates")]
        public async Task<ActionResult<FundingRateDto>> AddRate(int id, CreateFundingRateRequest request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { message = "Rate amount must be greater than zero." });
            }

            if (!RateFrequencies.All.Contains(request.Frequency))
            {
                return BadRequest(new { message = "Frequency must be Daily, Weekly, or Monthly." });
            }

            if (request.EffectiveTo.HasValue && request.EffectiveTo.Value < request.EffectiveFrom)
            {
                return BadRequest(new { message = "Effective to cannot be before effective from." });
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            await SqlAppLock.AcquireExclusiveAsync(
                dbContext.Database,
                $"funding-rate-{tenantContext.TenantId}-{id}");

            var contract = await dbContext.ClientFundingContracts
                .Include(x => x.Rates)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

            if (contract is null)
            {
                await transaction.RollbackAsync();
                return NotFound();
            }

            if (!await CanAccessContractClientAsync(contract.ClientId))
            {
                await transaction.RollbackAsync();
                return NotFound();
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
                        await transaction.RollbackAsync();
                        return BadRequest(new { message = "Closing the previous open-ended rate would make its period invalid." });
                    }

                    open.EffectiveTo = closedTo;
                }
            }

            // Re-check overlap under the lock (includes any close-previous mutation above).
            var overlap = contract.Rates.Any(existing =>
                DateRanges.Overlaps(
                    existing.EffectiveFrom,
                    existing.EffectiveTo,
                    request.EffectiveFrom,
                    request.EffectiveTo));

            if (overlap)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = "This rate period overlaps an existing rate on the same contract." });
            }

            var rate = new FundingRate
            {
                ClientFundingContractId = id,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = request.EffectiveTo,
                Frequency = request.Frequency,
                Amount = Money.Round(request.Amount),
                Notes = request.Notes?.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            dbContext.FundingRates.Add(rate);
            await dbContext.SaveChangesAsync();
            await audit.LogAsync("FundingRate", rate.Id.ToString(), "Create", null, request, "Added funding rate.");
            await transaction.CommitAsync();

            return Ok(new FundingRateDto
            {
                Id = rate.Id,
                ClientFundingContractId = rate.ClientFundingContractId,
                EffectiveFrom = rate.EffectiveFrom,
                EffectiveTo = rate.EffectiveTo,
                Frequency = rate.Frequency,
                Amount = rate.Amount,
                Notes = rate.Notes
            });
        }

        private async Task<ActionResult?> EnsureClientAccessAsync(int clientId)
        {
            var client = await dbContext.Clients.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == clientId && x.TenantId == tenantContext.TenantId);
            if (client is null)
            {
                return NotFound();
            }

            if (!await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, client.CareHomeId))
            {
                return NotFound();
            }

            return null;
        }

        private async Task<ActionResult?> EnsureContractAccessAsync(int contractId)
        {
            var clientId = await dbContext.ClientFundingContracts.AsNoTracking()
                .Where(x => x.Id == contractId && x.TenantId == tenantContext.TenantId)
                .Select(x => (int?)x.ClientId)
                .FirstOrDefaultAsync();
            if (clientId is null)
            {
                return NotFound();
            }

            if (!await CanAccessContractClientAsync(clientId.Value))
            {
                return NotFound();
            }

            return null;
        }

        private async Task<bool> CanAccessContractClientAsync(int clientId)
        {
            var careHomeId = await dbContext.Clients.AsNoTracking()
                .Where(x => x.Id == clientId && x.TenantId == tenantContext.TenantId)
                .Select(x => (int?)x.CareHomeId)
                .FirstOrDefaultAsync();
            if (careHomeId is null)
            {
                return false;
            }

            return await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, careHomeId.Value);
        }

        private async Task<FundingContractDto?> LoadDto(int id)
        {
            var contract = await dbContext.ClientFundingContracts.AsNoTracking()
                .Include(x => x.FundingAuthority)
                .Include(x => x.InvoiceCategory)
                .Include(x => x.NominalCode)
                .Include(x => x.Rates)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

            return contract is null ? null : Map(contract);
        }

        private async Task<ActionResult?> EnsureRelatedEntities(
            int fundingAuthorityId,
            int invoiceCategoryId,
            int nominalCodeId,
            int? invoiceTemplateId)
        {
            var tenantId = tenantContext.TenantId;
            if (!await dbContext.FundingAuthorities.AnyAsync(x => x.Id == fundingAuthorityId && x.TenantId == tenantId))
            {
                return BadRequest(new { message = "Funding authority was not found in this organisation." });
            }

            if (!await dbContext.InvoiceCategories.AnyAsync(x => x.Id == invoiceCategoryId && x.TenantId == tenantId))
            {
                return BadRequest(new { message = "Invoice category was not found in this organisation." });
            }

            if (!await dbContext.NominalCodes.AnyAsync(x => x.Id == nominalCodeId && x.TenantId == tenantId))
            {
                return BadRequest(new { message = "Nominal code was not found in this organisation." });
            }

            if (invoiceTemplateId is int templateId
                && !await dbContext.InvoiceTemplates.AnyAsync(x => x.Id == templateId && x.TenantId == tenantId))
            {
                return BadRequest(new { message = "Invoice template was not found in this organisation." });
            }

            return null;
        }

        private static string FundingContractLockResource(
            int tenantId,
            int clientId,
            int fundingAuthorityId,
            int invoiceCategoryId) =>
            $"funding-contract-{tenantId}-{clientId}-{fundingAuthorityId}-{invoiceCategoryId}";

        private async Task<ActionResult?> EnsureNoOverlappingContract(
            int clientId,
            int fundingAuthorityId,
            int invoiceCategoryId,
            DateOnly start,
            DateOnly? end,
            int? excludeContractId)
        {
            var tenantId = tenantContext.TenantId;
            var others = await dbContext.ClientFundingContracts
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId
                    && x.ClientId == clientId
                    && x.FundingAuthorityId == fundingAuthorityId
                    && x.InvoiceCategoryId == invoiceCategoryId
                    && x.Status == "Active"
                    && (excludeContractId == null || x.Id != excludeContractId))
                .Select(x => new { x.ContractStartDate, x.ContractEndDate })
                .ToListAsync();

            if (others.Any(x => FundingContractOverlap.PeriodsOverlap(
                    x.ContractStartDate,
                    x.ContractEndDate,
                    start,
                    end)))
            {
                return BadRequest(new
                {
                    message = FundingContractOverlap.ConflictMessage,
                    code = FundingContractOverlap.ConflictCode
                });
            }

            return null;
        }

        private static FundingContractDto Map(ClientFundingContract x)
        {
            return new FundingContractDto
            {
                Id = x.Id,
                ClientId = x.ClientId,
                FundingAuthorityId = x.FundingAuthorityId,
                FundingAuthorityName = x.FundingAuthority.Name,
                InvoiceCategoryId = x.InvoiceCategoryId,
                InvoiceCategoryName = x.InvoiceCategory.Name,
                NominalCodeId = x.NominalCodeId,
                NominalCode = x.NominalCode.Code,
                InvoiceTemplateId = x.InvoiceTemplateId,
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

        private ActionResult? ValidateContract(CreateFundingContractRequest request)
        {
            if (request.ContractEndDate.HasValue && request.ContractEndDate.Value < request.ContractStartDate)
            {
                return BadRequest(new { message = "Contract end date cannot be before start date." });
            }

            return null;
        }
    }
}

