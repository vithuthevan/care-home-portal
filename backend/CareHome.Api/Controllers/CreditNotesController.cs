using CareHome.Api.Audit;
using CareHome.Api.Billing;
using CareHome.Api.Data;
using CareHome.Api.Documents;
using CareHome.Api.Dtos.CreditNotes;
using CareHome.Api.Dtos.Email;
using CareHome.Api.Models;
using CareHome.Api.Security;
using CareHome.Api.Services;
using CareHome.Api.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers;

[ApiController]
[Route("api/credit-notes")]
[RequireTenant]
[Authorize(Policy = CareHomePolicies.CanViewFinancialReports)]
public class CreditNotesController(
    CareHomeDbContext dbContext,
    CreditNoteService creditNotes,
    InvoicePdfService pdfs,
    DocumentEmailService documentEmail,
    ITenantContext tenantContext,
    UserAccessService userAccess,
    AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(int? page = null, int? pageSize = null)
    {
        var homes = await userAccess.GetScopedCareHomeIdsAsync(tenantContext.TenantId);
        var query = dbContext.CreditNotes.AsNoTracking()
            .Where(x => x.TenantId == tenantContext.TenantId && homes.Contains(x.Invoice.CareHomeId))
            .OrderByDescending(x => x.CreditNoteDate)
            .Select(x => new CreditNoteDto
            {
                Id = x.Id,
                CreditNoteNumber = x.CreditNoteNumber,
                InvoiceId = x.InvoiceId,
                InvoicePublicId = x.Invoice.PublicId,
                InvoiceNumber = x.Invoice.InvoiceNumber,
                CreditNoteDate = x.CreditNoteDate,
                PeriodStart = x.PeriodStart,
                PeriodEnd = x.PeriodEnd,
                Reason = x.Reason,
                Status = x.Status,
                TotalAmount = x.TotalAmount,
                SentAt = x.SentAt,
                RecipientEmail = x.RecipientEmail
            });

        if (!Pagination.IsRequested(page, pageSize))
        {
            return Ok(await query.ToListAsync());
        }

        var (p, ps) = Pagination.Normalize(page, pageSize);
        var total = await query.CountAsync();
        var items = await query.Skip((p - 1) * ps).Take(ps).ToListAsync();
        return Ok(new PagedResult<CreditNoteDto>
        {
            Items = items,
            TotalCount = total,
            Page = p,
            PageSize = ps
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CreditNoteDto>> Get(int id)
    {
        var note = await dbContext.CreditNotes.AsNoTracking()
            .Include(x => x.Invoice)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

        if (note is null)
        {
            return NotFound();
        }

        if (!await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, note.Invoice.CareHomeId))
        {
            return NotFound();
        }

        return Ok(MapDetail(note));
    }

    [HttpPost("preview")]
    [Authorize(Policy = CareHomePolicies.CanManageBilling)]
    public async Task<ActionResult<CreditNotePreviewResponse>> Preview(CreditNotePreviewRequest request)
    {
        return Ok(await creditNotes.PreviewAsync(tenantContext.TenantId, request));
    }

    [HttpPost("generate")]
    [Authorize(Policy = CareHomePolicies.CanManageBilling)]
    public async Task<ActionResult<CreditNoteDto>> Generate(CreditNotePreviewRequest request)
    {
        var (note, error) = await creditNotes.GenerateAsync(tenantContext.TenantId, request);
        if (error is not null || note is null)
        {
            return BadRequest(new { message = error ?? "Unable to generate credit note." });
        }

        var created = await dbContext.CreditNotes.AsNoTracking()
            .Include(x => x.Invoice)
            .Include(x => x.Lines)
            .FirstAsync(x => x.Id == note.Id);

        return CreatedAtAction(nameof(Get), new { id = note.Id }, MapDetail(created));
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id)
    {
        var note = await LoadNote(id);
        if (note is null)
        {
            return NotFound();
        }

        if (!await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, note.Invoice.CareHomeId))
        {
            return NotFound();
        }

        var bytes = await pdfs.GetOrCreateCreditNotePdfAsync(note, await TenantPublicIdAsync());
        await dbContext.SaveChangesAsync();
        return File(bytes, "application/pdf", $"credit-note-{note.CreditNoteNumber}.pdf");
    }

    [HttpPatch("{id:int}/recipient-email")]
    [Authorize(Policy = CareHomePolicies.CanManageBilling)]
    public async Task<IActionResult> UpdateRecipientEmail(int id, UpdateRecipientEmailRequest request)
    {
        var note = await dbContext.CreditNotes
            .Include(x => x.Invoice)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);
        if (note is null || !await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, note.Invoice.CareHomeId))
        {
            return NotFound();
        }

        note.RecipientEmail = string.IsNullOrWhiteSpace(request.RecipientEmail)
            ? null
            : request.RecipientEmail.Trim();
        await dbContext.SaveChangesAsync();
        await audit.LogAsync(
            "CreditNote",
            id.ToString(),
            "UpdateRecipientEmail",
            null,
            new { note.RecipientEmail },
            "Updated credit note recipient email.");
        return Ok(new { note.Id, note.RecipientEmail });
    }

    [HttpPost("{id:int}/send")]
    [Authorize(Policy = CareHomePolicies.CanManageBilling)]
    public async Task<IActionResult> Send(int id)
    {
        var result = await documentEmail.SendCreditNoteAsync(
            tenantContext.TenantId,
            id,
            HttpContext.RequestAborted);

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

    private async Task<CreditNote?> LoadNote(int id)
    {
        return await dbContext.CreditNotes
            .Include(x => x.Lines)
            .Include(x => x.Invoice)
                .ThenInclude(x => x.Lines)
            .Include(x => x.Invoice)
                .ThenInclude(x => x.InvoiceTemplate)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);
    }

    private async Task<Guid> TenantPublicIdAsync()
    {
        return await dbContext.Tenants
            .Where(x => x.Id == tenantContext.TenantId)
            .Select(x => x.PublicId)
            .FirstAsync();
    }

    private static CreditNoteDto Map(CreditNote x)
    {
        return new CreditNoteDto
        {
            Id = x.Id,
            CreditNoteNumber = x.CreditNoteNumber,
            InvoiceId = x.InvoiceId,
            InvoicePublicId = x.Invoice.PublicId,
            InvoiceNumber = x.Invoice.InvoiceNumber,
            CreditNoteDate = x.CreditNoteDate,
            PeriodStart = x.PeriodStart,
            PeriodEnd = x.PeriodEnd,
            Reason = x.Reason,
            Status = x.Status,
            TotalAmount = x.TotalAmount,
            SentAt = x.SentAt,
            RecipientEmail = x.RecipientEmail
        };
    }

    private static CreditNoteDto MapDetail(CreditNote x)
    {
        var dto = Map(x);
        dto.Lines = x.Lines.Select(l => new CreditNoteLineDto
        {
            Id = l.Id,
            InvoiceLineId = l.InvoiceLineId,
            Description = l.Description,
            ServicePeriodStart = l.ServicePeriodStart,
            ServicePeriodEnd = l.ServicePeriodEnd,
            Amount = l.Amount
        }).ToList();
        return dto;
    }
}

