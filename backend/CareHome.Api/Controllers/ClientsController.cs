using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Clients;
using CareHome.Api.Models;
using CareHome.Api.Security;
using CareHome.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RequireTenant]
    public class ClientsController(
        CareHomeDbContext dbContext,
        ITenantContext tenantContext,
        UserAccessService userAccess,
        AuditService audit,
        ClientIdentifierService clientIdentifiers) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<ClientDto>>> GetClients(
            string? search = null,
            int? companyId = null,
            int? careHomeId = null,
            int? fundingAuthorityId = null,
            string? status = null,
            string? contractStatus = null,
            bool includeArchived = false,
            int page = 1,
            int pageSize = 50)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var tenantId = tenantContext.TenantId;
            var homes = await userAccess.GetScopedCareHomeIdsAsync(tenantId);
            var query = dbContext.Clients.AsNoTracking()
                .Where(x => x.TenantId == tenantId && homes.Contains(x.CareHomeId));

            if (!includeArchived)
            {
                query = query.Where(x => !x.IsArchived);
            }

            if (companyId.HasValue)
            {
                query = query.Where(x => x.CareHome.CompanyId == companyId.Value);
            }

            if (careHomeId.HasValue)
            {
                query = query.Where(x => x.CareHomeId == careHomeId.Value);
            }

            if (fundingAuthorityId.HasValue)
            {
                query = query.Where(x => x.FundingContracts.Any(c =>
                    c.FundingAuthorityId == fundingAuthorityId.Value));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(contractStatus))
            {
                query = query.Where(x => x.FundingContracts.Any(c => c.Status == contractStatus));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var value = search.Trim();

                query = query.Where(x =>
                    x.FirstName.Contains(value) ||
                    x.LastName.Contains(value) ||
                    x.SageId.Contains(value) ||
                    x.ReferenceNumber.Contains(value));
            }

            var total = await query.CountAsync();
            var clients = await ProjectToDto(query)
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PagedResult<ClientDto>
            {
                Items = clients,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ClientDto>> GetClient(int id)
        {
            var client = await ProjectToDto(
                    dbContext.Clients.AsNoTracking()
                        .Where(x => x.TenantId == tenantContext.TenantId))
                .FirstOrDefaultAsync(x => x.Id == id);

            if (client is null)
            {
                return NotFound();
            }

            if (!await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, client.CareHomeId))
            {
                return NotFound();
            }

            return Ok(client);
        }

        [HttpPost]
        public async Task<ActionResult<ClientDto>> CreateClient(
            CreateClientRequest request)
        {
            var careHomeError =
                await ValidateSelectedCareHome(request.CareHomeId);

            if (careHomeError is not null)
            {
                return careHomeError;
            }

            var (referenceNumber, sageId) = await clientIdentifiers.ResolveCreateIdentifiersAsync(
                tenantContext.TenantId,
                request.CareHomeId,
                request.ReferenceNumber,
                request.SageId);

            if (await dbContext.Clients.AnyAsync(
                    x => x.TenantId == tenantContext.TenantId && x.SageId == sageId))
            {
                return BadRequest(new
                {
                    message =
                        "A client with this Sage ID already exists."
                });
            }

            if (await dbContext.Clients.AnyAsync(
                    x => x.TenantId == tenantContext.TenantId && x.ReferenceNumber == referenceNumber))
            {
                return BadRequest(new
                {
                    message =
                        "A client with this reference number already exists."
                });
            }

            if (request.CareType != "Nursing" &&
                request.CareType != "Residential")
            {
                return BadRequest(new
                {
                    message =
                        "Care type must be Nursing or Residential."
                });
            }

            var dateError = ValidateClientDates(
                request.DateOfBirth,
                request.AdmissionDate);

            if (dateError is not null)
            {
                return dateError;
            }

            var client = new Client
            {
                TenantId = tenantContext.TenantId,
                CareHomeId = request.CareHomeId,

                SageId = sageId,
                ReferenceNumber = referenceNumber,

                Title = request.Title?.Trim(),

                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),

                DateOfBirth = request.DateOfBirth,

                CareType = request.CareType,

                Status = "Current",

                AdmissionDate = request.AdmissionDate,

                Email = request.Email?.Trim(),
                Phone = request.Phone?.Trim(),
                Notes = request.Notes?.Trim(),

                IsArchived = false
            };

            dbContext.Clients.Add(client);

            await dbContext.SaveChangesAsync();

            await audit.LogAsync(
                "Client",
                client.Id.ToString(),
                "Create",
                null,
                new { client.ReferenceNumber, client.SageId, client.FirstName, client.LastName },
                $"Created client {client.FirstName} {client.LastName}.");

            var dto = await ProjectToDto(
                    dbContext.Clients.AsNoTracking())
                .FirstAsync(x => x.Id == client.Id);

            return CreatedAtAction(
                nameof(GetClient),
                new { id = client.Id },
                dto);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ClientDto>> UpdateClient(
            int id,
            UpdateClientRequest request)
        {
            var client = await dbContext.Clients
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

            if (client is null)
            {
                return NotFound();
            }

            var careHomeError =
                await ValidateSelectedCareHome(
                    request.CareHomeId,
                    client.CareHomeId);

            if (careHomeError is not null)
            {
                return careHomeError;
            }

            var sageId = request.SageId.Trim();

            var referenceNumber =
                request.ReferenceNumber.Trim();

            var duplicateSageId =
                await dbContext.Clients.AnyAsync(x =>
                    x.TenantId == tenantContext.TenantId &&
                    x.Id != id &&
                    x.SageId == sageId);

            if (duplicateSageId)
            {
                return BadRequest(new
                {
                    message =
                        "A client with this Sage ID already exists."
                });
            }

            var duplicateReference =
                await dbContext.Clients.AnyAsync(x =>
                    x.TenantId == tenantContext.TenantId &&
                    x.Id != id &&
                    x.ReferenceNumber == referenceNumber);

            if (duplicateReference)
            {
                return BadRequest(new
                {
                    message =
                        "A client with this reference number already exists."
                });
            }

            if (request.CareType != "Nursing" &&
                request.CareType != "Residential")
            {
                return BadRequest(new
                {
                    message =
                        "Care type must be Nursing or Residential."
                });
            }

            var allowedStatuses = new[]
            {
                "Current",
                "Left",
                "Deceased"
            };

            if (!allowedStatuses.Contains(request.Status))
            {
                return BadRequest(new
                {
                    message = "Invalid client status."
                });
            }

            var dateError = ValidateClientDates(
                request.DateOfBirth,
                request.AdmissionDate,
                request.DischargeDate,
                request.Status);

            if (dateError is not null)
            {
                return dateError;
            }

            client.CareHomeId = request.CareHomeId;

            client.SageId = sageId;

            client.ReferenceNumber =
                referenceNumber;

            client.Title =
                request.Title?.Trim();

            client.FirstName =
                request.FirstName.Trim();

            client.LastName =
                request.LastName.Trim();

            client.DateOfBirth =
                request.DateOfBirth;

            client.CareType =
                request.CareType;

            client.Status =
                request.Status;

            client.AdmissionDate =
                request.AdmissionDate;

            if (request.Status == "Current")
            {
                client.DischargeDate = null;
                client.DischargeReason = null;
                client.IsArchived = false;
            }
            else
            {
                client.DischargeDate =
                    request.DischargeDate;

                client.DischargeReason =
                    request.DischargeReason?.Trim();

                client.IsArchived =
                    request.IsArchived;
            }

            client.Email =
                request.Email?.Trim();

            client.Phone =
                request.Phone?.Trim();

            client.Notes =
                request.Notes?.Trim();

            await dbContext.SaveChangesAsync();

            await audit.LogAsync(
                "Client",
                client.Id.ToString(),
                "Update",
                null,
                new { client.ReferenceNumber, client.FirstName, client.LastName, client.Status },
                $"Updated client {client.FirstName} {client.LastName}.");

            var dto = await ProjectToDto(
                    dbContext.Clients.AsNoTracking())
                .FirstAsync(x => x.Id == client.Id);

            return Ok(dto);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> ArchiveClient(int id)
        {
            var client = await dbContext.Clients
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

            if (client is null)
            {
                return NotFound();
            }

            if (client.Status == "Current")
            {
                return BadRequest(new
                {
                    message =
                        "A current client must be discharged before being archived."
                });
            }

            client.IsArchived = true;

            await dbContext.SaveChangesAsync();

            await audit.LogAsync(
                "Client",
                client.Id.ToString(),
                "Archive",
                null,
                new { client.ReferenceNumber, client.IsArchived },
                $"Archived client {client.FirstName} {client.LastName}.");

            return NoContent();
        }

        private static IQueryable<ClientDto> ProjectToDto(
            IQueryable<Client> query)
        {
            return query.Select(x => new ClientDto
            {
                Id = x.Id,
                CareHomeId = x.CareHomeId,
                CompanyId = x.CareHome.CompanyId,
                CareHomeName = x.CareHome.Name,
                CompanyName = x.CareHome.Company.Name,
                SageId = x.SageId,
                ReferenceNumber = x.ReferenceNumber,
                Title = x.Title,
                FirstName = x.FirstName,
                LastName = x.LastName,
                DateOfBirth = x.DateOfBirth,
                CareType = x.CareType,
                Status = x.Status,
                AdmissionDate = x.AdmissionDate,
                DischargeDate = x.DischargeDate,
                DischargeReason = x.DischargeReason,
                Email = x.Email,
                Phone = x.Phone,
                Notes = x.Notes,
                IsArchived = x.IsArchived
            });
        }

        private BadRequestObjectResult? ValidateClientDates(
            DateOnly? dateOfBirth,
            DateOnly admissionDate,
            DateOnly? dischargeDate = null,
            string? status = null)
        {
            if (admissionDate == default)
            {
                return BadRequest(new
                {
                    message = "Admission date is required."
                });
            }

            if (dateOfBirth > DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    message =
                        "Date of birth cannot be in the future."
                });
            }

            if (status is not null &&
                status != "Current" &&
                dischargeDate is null)
            {
                return BadRequest(new
                {
                    message =
                        "Discharge date is required when client is no longer current."
                });
            }

            if (status != "Current" &&
                dischargeDate.HasValue &&
                dischargeDate.Value < admissionDate)
            {
                return BadRequest(new
                {
                    message =
                        "Discharge date cannot be before admission date."
                });
            }

            return null;
        }

        private async Task<ActionResult?> ValidateSelectedCareHome(
            int careHomeId,
            int? currentCareHomeId = null)
        {
            var careHome = await dbContext.CareHomes
                .FirstOrDefaultAsync(x => x.Id == careHomeId && x.TenantId == tenantContext.TenantId);

            if (careHome is null)
            {
                return BadRequest(new
                {
                    message =
                        "Selected care home does not exist or is inactive."
                });
            }

            var careHomeUnchanged =
                currentCareHomeId == careHomeId;

            if (!careHomeUnchanged && !careHome.IsActive)
            {
                return BadRequest(new
                {
                    message =
                        "Selected care home does not exist or is inactive."
                });
            }

            return null;
        }
    }
}
