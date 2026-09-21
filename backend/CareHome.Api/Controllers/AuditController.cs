using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Audit;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [Route("api/audit")]
    [RequireTenant]
    [Authorize(Policy = CareHomePolicies.CanViewAudit)]
    public class AuditController(CareHomeDbContext dbContext, ITenantContext tenantContext) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<AuditLogDto>>> List(
            string? entityType,
            string? action,
            int page = 1,
            int pageSize = 50)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = dbContext.AuditLogs.AsNoTracking()
                .Where(x => x.TenantId == tenantContext.TenantId);
            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(x => x.EntityType == entityType);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(x => x.Action == action);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.LoggedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AuditLogDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    UserDisplayName = dbContext.Users
                        .Where(user => user.Id == x.UserId)
                        .Select(user => user.DisplayName)
                        .FirstOrDefault(),
                    LoggedAt = x.LoggedAt,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    Action = x.Action,
                    Description = x.Description
                })
                .ToListAsync();

            return Ok(new PagedResult<AuditLogDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }
    }
}

