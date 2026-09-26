using CareHome.Api.Data;
using CareHome.Api.Dtos.Common;
using CareHome.Api.Dtos.Email;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using CareHome.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/email-send-logs")]
[RequireTenant]
[Authorize(Policy = CareHomePolicies.CanViewFinancialReports)]
public class EmailSendLogsController(
    CareHomeDbContext dbContext,
    ITenantContext tenantContext,
    DocumentEmailService documentEmail) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<EmailSendLogDto>>> List(
        string? documentType,
        bool? success,
        int page = 1,
        int pageSize = 50)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var tenantId = tenantContext.TenantId;

        var query = dbContext.EmailSendLogs.AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(documentType))
        {
            query = query.Where(x => x.DocumentType == documentType.Trim());
        }

        if (success.HasValue)
        {
            query = query.Where(x => x.Success == success.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.AttemptedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new EmailSendLogDto
            {
                Id = x.Id,
                AttemptedAt = x.AttemptedAt,
                DocumentType = x.DocumentType,
                DocumentId = x.DocumentId,
                Recipient = x.Recipient,
                Success = x.Success,
                Simulated = x.Simulated,
                ErrorMessage = x.ErrorMessage
            })
            .ToListAsync();

        return Ok(new PagedResult<EmailSendLogDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    [HttpPost("{id:int}/resend")]
    [Authorize(Policy = CareHomePolicies.CanManageBilling)]
    public async Task<IActionResult> Resend(int id, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var log = await dbContext.EmailSendLogs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (log is null)
        {
            return NotFound();
        }

        if (!log.Success)
        {
            // Allow resend for failed attempts; also permit resend when operators need a duplicate delivery.
        }

        switch (log.DocumentType)
        {
            case "Invoice":
            {
                var result = await documentEmail.SendInvoiceAsync(tenantId, log.DocumentId, cancellationToken);
                if (result.BulkOutcome == "Skipped" && result.Reason == "Not found.")
                {
                    return NotFound();
                }

                if (result.BulkOutcome == "Skipped")
                {
                    return BadRequest(new { message = result.Reason });
                }

                if (result.BulkOutcome == "Failed")
                {
                    return BadRequest(new { message = result.Reason ?? "Email failed." });
                }

                return Ok(new { simulated = result.Simulated, sentAt = result.SentAt });
            }
            case "CreditNote":
            {
                var result = await documentEmail.SendCreditNoteAsync(tenantId, log.DocumentId, cancellationToken);
                if (result.IsNotFound)
                {
                    return NotFound();
                }

                if (!result.Success)
                {
                    return BadRequest(new { message = result.ErrorMessage ?? "Email failed." });
                }

                return Ok(new { simulated = result.Simulated });
            }
            case "CollectionReminder":
                return BadRequest(new
                {
                    message = "Collection reminders cannot be resent from this log. Use Send reminders on the Collections page."
                });
            default:
                return BadRequest(new { message = "This log entry cannot be resent." });
        }
    }
}
