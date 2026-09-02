using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Documents;
using CareHome.Api.Dtos.Invoices;
using CareHome.Api.Email;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Controllers
{
    [ApiController]
    [Route("api/invoices")]
    [RequireTenant]
    public class InvoicesController(
        CareHomeDbContext dbContext,
        InvoicePdfService pdfs,
        IEmailSender email,
        AuditService audit,
        UserAccessService userAccess,
        ITenantContext tenantContext) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<InvoiceListDto>>> List(
            string? invoiceNumber,
            int? companyId,
            int? careHomeId,
            int? fundingAuthorityId,
            int? clientId,
            int? categoryId,
            DateOnly? from,
            DateOnly? to,
            string? status,
            string? paymentStatus,
            int page = 1,
            int pageSize = 50)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var homes = await userAccess.GetScopedCareHomeIdsAsync(tenantContext.TenantId);
            var query = dbContext.Invoices.AsNoTracking()
                .Where(x => x.TenantId == tenantContext.TenantId && homes.Contains(x.CareHomeId));

            if (!string.IsNullOrWhiteSpace(invoiceNumber))
            {
                query = query.Where(x => x.InvoiceNumber.Contains(invoiceNumber.Trim()));
            }

            if (companyId.HasValue)
            {
                query = query.Where(x => x.CompanyId == companyId);
            }

            if (careHomeId.HasValue)
            {
                query = query.Where(x => x.CareHomeId == careHomeId);
            }

            if (fundingAuthorityId.HasValue)
            {
                query = query.Where(x => x.FundingAuthorityId == fundingAuthorityId);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(x => x.InvoiceCategoryId == categoryId);
            }

            if (clientId.HasValue)
            {
                query = query.Where(x => x.Lines.Any(l => l.ClientId == clientId));
            }

            if (from.HasValue)
            {
                query = query.Where(x => x.InvoiceDate >= from);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.InvoiceDate <= to);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                query = query.Where(x => x.PaymentStatus == paymentStatus);
            }

            var total = await query.CountAsync();
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var items = await query
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new InvoiceListDto
                {
                    Id = x.Id,
                    InvoiceNumber = x.InvoiceNumber,
                    CompanyName = x.SnapshotCompanyName,
                    CareHomeName = x.SnapshotCareHomeName,
                    FundingAuthorityName = x.SnapshotFundingAuthorityName,
                    InvoiceCategoryName = x.SnapshotInvoiceCategoryName,
                    InvoiceDate = x.InvoiceDate,
                    PeriodStart = x.PeriodStart,
                    PeriodEnd = x.PeriodEnd,
                    Status = x.Status,
                    PaymentStatus = x.PaymentStatus,
                    IsDue = x.PaymentStatus != "Paid" && x.Status != "Void" && x.DueDate < today,
                    TotalAmount = x.TotalAmount,
                    SentAt = x.SentAt
                })
                .ToListAsync();

            return Ok(new PagedResult<InvoiceListDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InvoiceDetailDto>> Get(int id)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var invoice = await dbContext.Invoices.AsNoTracking()
                .Where(x => x.Id == id && x.TenantId == tenantContext.TenantId)
                .Select(x => new InvoiceDetailDto
                {
                    Id = x.Id,
                    InvoiceNumber = x.InvoiceNumber,
                    CompanyId = x.CompanyId,
                    CareHomeId = x.CareHomeId,
                    FundingAuthorityId = x.FundingAuthorityId,
                    InvoiceCategoryId = x.InvoiceCategoryId,
                    CompanyName = x.SnapshotCompanyName,
                    CareHomeName = x.SnapshotCareHomeName,
                    FundingAuthorityName = x.SnapshotFundingAuthorityName,
                    InvoiceCategoryName = x.SnapshotInvoiceCategoryName,
                    InvoiceDate = x.InvoiceDate,
                    DueDate = x.DueDate,
                    PeriodStart = x.PeriodStart,
                    PeriodEnd = x.PeriodEnd,
                    Status = x.Status,
                    PaymentStatus = x.PaymentStatus,
                    IsDue = x.PaymentStatus != "Paid" && x.Status != "Void" && x.DueDate < today,
                    TotalAmount = x.TotalAmount,
                    SentAt = x.SentAt,
                    RecipientEmail = x.RecipientEmail,
                    Lines = x.Lines.Select(l => new InvoiceLineDto
                    {
                        Id = l.Id,
                        ClientId = l.ClientId,
                        ClientName = l.SnapshotClientName,
                        ClientReference = l.SnapshotClientReferenceNumber,
                        SageId = l.SnapshotSageId,
                        NominalCode = l.SnapshotNominalCode,
                        ServicePeriodStart = l.ServicePeriodStart,
                        ServicePeriodEnd = l.ServicePeriodEnd,
                        EligibleDays = l.EligibleDays,
                        RateFrequency = l.RateFrequency,
                        RateAmount = l.RateAmount,
                        LineAmount = l.LineAmount,
                        Description = l.Description
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (invoice is null)
            {
                return NotFound();
            }

            if (!await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, invoice.CareHomeId))
            {
                return NotFound();
            }

            return Ok(invoice);
        }

        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> Pdf(int id)
        {
            var invoice = await dbContext.Invoices
                .Include(x => x.Lines)
                .Include(x => x.InvoiceTemplate)
                .Include(x => x.CareHome)
                .Include(x => x.Tenant)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

            if (invoice is null)
            {
                return NotFound();
            }

            if (!await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, invoice.CareHomeId))
            {
                return NotFound();
            }

            var bytes = await pdfs.GetOrCreateInvoicePdfAsync(invoice, await TenantPublicIdAsync());
            await dbContext.SaveChangesAsync();
            return File(bytes, "application/pdf", $"invoice-{invoice.InvoiceNumber}.pdf");
        }

        [HttpPost("{id:int}/send")]
        public async Task<IActionResult> Send(int id)
        {
            var invoice = await dbContext.Invoices
                .Include(x => x.Lines)
                .Include(x => x.InvoiceTemplate)
                .Include(x => x.CareHome)
                .Include(x => x.Tenant)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);
            if (invoice is null)
            {
                return NotFound();
            }

            if (!await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, invoice.CareHomeId))
            {
                return NotFound();
            }

            if (invoice.Status == "Void")
            {
                return BadRequest(new { message = "A void invoice cannot be emailed." });
            }

            if (string.IsNullOrWhiteSpace(invoice.RecipientEmail))
            {
                return BadRequest(new { message = "This invoice has no recipient email." });
            }

            var pdf = await pdfs.GetOrCreateInvoicePdfAsync(invoice, await TenantPublicIdAsync());
            var subject = $"Invoice {invoice.InvoiceNumber}";
            var body = $"Please find invoice {invoice.InvoiceNumber} attached.";
            var result = await email.SendAsync(invoice.RecipientEmail, subject, body, $"invoice-{invoice.InvoiceNumber}.pdf", pdf);

            dbContext.EmailSendLogs.Add(new EmailSendLog
            {
                TenantId = tenantContext.TenantId,
                AttemptedAt = DateTimeOffset.UtcNow,
                DocumentType = "Invoice",
                DocumentId = invoice.Id,
                Recipient = invoice.RecipientEmail,
                Success = result.Success,
                Simulated = result.Simulated,
                ErrorMessage = result.ErrorMessage
            });

            if (result.Success)
            {
                invoice.SentAt = DateTimeOffset.UtcNow;
                invoice.Status = "Sent";
            }

            await dbContext.SaveChangesAsync();

            await audit.LogAsync(
                "Invoice",
                invoice.Id.ToString(),
                "Send",
                null,
                new { invoice.InvoiceNumber, result.Success, result.Simulated },
                result.Success
                    ? $"Sent invoice {invoice.InvoiceNumber}."
                    : $"Failed to send invoice {invoice.InvoiceNumber}.");

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage ?? "Email failed." });
            }

            return Ok(new { simulated = result.Simulated, sentAt = invoice.SentAt });
        }

        [HttpPost("bulk-send")]
        public async Task<ActionResult<BulkSendResultDto>> BulkSend(BulkSendRequest request)
        {
            var summary = new BulkSendResultDto();
            foreach (var id in request.InvoiceIds.Distinct())
            {
                var invoice = await dbContext.Invoices
                    .Include(x => x.Lines)
                    .Include(x => x.InvoiceTemplate)
                    .Include(x => x.CareHome)
                    .Include(x => x.Tenant)
                    .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);
                if (invoice is null || !await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, invoice.CareHomeId))
                {
                    summary.Skipped++;
                    summary.Items.Add(new BulkSendItemDto { InvoiceId = id, Outcome = "Skipped", Reason = "Not found." });
                    continue;
                }

                if (invoice.Status == "Void" || string.IsNullOrWhiteSpace(invoice.RecipientEmail))
                {
                    summary.Skipped++;
                    summary.Items.Add(new BulkSendItemDto
                    {
                        InvoiceId = id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        Outcome = "Skipped",
                        Reason = invoice.Status == "Void" ? "Void." : "No recipient email."
                    });
                    continue;
                }

                var pdf = await pdfs.GetOrCreateInvoicePdfAsync(invoice, await TenantPublicIdAsync());
                var result = await email.SendAsync(
                    invoice.RecipientEmail,
                    $"Invoice {invoice.InvoiceNumber}",
                    $"Please find invoice {invoice.InvoiceNumber} attached.",
                    $"invoice-{invoice.InvoiceNumber}.pdf",
                    pdf);

                dbContext.EmailSendLogs.Add(new EmailSendLog
                {
                    TenantId = tenantContext.TenantId,
                    AttemptedAt = DateTimeOffset.UtcNow,
                    DocumentType = "Invoice",
                    DocumentId = invoice.Id,
                    Recipient = invoice.RecipientEmail,
                    Success = result.Success,
                    Simulated = result.Simulated,
                    ErrorMessage = result.ErrorMessage
                });

                if (result.Success)
                {
                    invoice.SentAt = DateTimeOffset.UtcNow;
                    invoice.Status = "Sent";
                    summary.Succeeded++;
                    summary.Items.Add(new BulkSendItemDto
                    {
                        InvoiceId = id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        Outcome = result.Simulated ? "Simulated" : "Succeeded"
                    });
                }
                else
                {
                    summary.Failed++;
                    summary.Items.Add(new BulkSendItemDto
                    {
                        InvoiceId = id,
                        InvoiceNumber = invoice.InvoiceNumber,
                        Outcome = "Failed",
                        Reason = result.ErrorMessage
                    });
                }
            }

            await dbContext.SaveChangesAsync();
            return Ok(summary);
        }

        [HttpPost("{id:int}/payment-status")]
        public async Task<IActionResult> PaymentStatus(int id, UpdatePaymentStatusRequest request)
        {
            var invoice = await dbContext.Invoices
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);
            if (invoice is null)
            {
                return NotFound();
            }

            if (!await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, invoice.CareHomeId))
            {
                return NotFound();
            }

            if (request.PaymentStatus is not "Paid" and not "NotPaid")
            {
                return BadRequest(new { message = "Payment status must be Paid or NotPaid." });
            }

            if (invoice.Status == "Void")
            {
                return BadRequest(new { message = "A void invoice cannot have its payment status changed." });
            }

            var old = invoice.PaymentStatus;
            invoice.PaymentStatus = request.PaymentStatus;
            await dbContext.SaveChangesAsync();
            await audit.LogAsync("Invoice", id.ToString(), "PaymentStatus", new { old }, new { request.PaymentStatus }, "Payment status updated.");
            return Ok(new { invoice.Id, invoice.PaymentStatus });
        }

        [HttpPost("bulk-payment-status")]
        public async Task<IActionResult> BulkPaymentStatus(BulkPaymentStatusRequest request)
        {
            if (request.PaymentStatus is not "Paid" and not "NotPaid")
            {
                return BadRequest(new { message = "Payment status must be Paid or NotPaid." });
            }

            var homes = await userAccess.GetScopedCareHomeIdsAsync(tenantContext.TenantId);
            var invoices = await dbContext.Invoices
                .Where(x => x.TenantId == tenantContext.TenantId
                    && request.InvoiceIds.Contains(x.Id)
                    && homes.Contains(x.CareHomeId)
                    && x.Status != "Void")
                .ToListAsync();

            foreach (var invoice in invoices)
            {
                invoice.PaymentStatus = request.PaymentStatus;
            }

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("Invoice", string.Join(",", invoices.Select(x => x.Id)), "PaymentStatus", null, request, "Bulk payment status update.");
            return Ok(new { updated = invoices.Count });
        }

        [HttpPost("{id:int}/void")]
        public async Task<IActionResult> Void(int id)
        {
            var invoice = await dbContext.Invoices.Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);
            if (invoice is null)
            {
                return NotFound();
            }

            if (!await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, invoice.CareHomeId))
            {
                return NotFound();
            }

            if (invoice.Status == "Void")
            {
                return BadRequest(new { message = "Invoice is already void." });
            }

            invoice.Status = "Void";
            foreach (var line in invoice.Lines.Where(x => x.MiscChargeId.HasValue))
            {
                var charge = await dbContext.MiscCharges
                    .FirstOrDefaultAsync(x => x.Id == line.MiscChargeId && x.TenantId == tenantContext.TenantId);
                if (charge is not null)
                {
                    charge.IsInvoiced = false;
                }
            }

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("Invoice", id.ToString(), "Void", null, new { invoice.InvoiceNumber }, "Invoice voided.");
            return Ok(new { invoice.Id, invoice.Status });
        }

        private async Task<Guid> TenantPublicIdAsync()
        {
            return await dbContext.Tenants
                .Where(x => x.Id == tenantContext.TenantId)
                .Select(x => x.PublicId)
                .FirstAsync();
        }
    }
}

