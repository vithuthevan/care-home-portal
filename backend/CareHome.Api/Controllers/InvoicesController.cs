using CareHome.Api.Audit;
using CareHome.Api.Billing;
using CareHome.Billing.Billing;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Documents;
using CareHome.Api.Dtos.Invoices;
using CareHome.Api.Email;
using CareHome.Api.Models;
using CareHome.Api.Security;
using CareHome.Api.Services;
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
        ITenantContext tenantContext,
        InvoiceReceivableReadModel receivableReadModel) : ControllerBase
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
                    PublicId = x.PublicId,
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

            await receivableReadModel.EnrichListAsync(tenantContext.TenantId, items, HttpContext.RequestAborted);

            return Ok(new PagedResult<InvoiceListDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("{key}")]
        public async Task<ActionResult<InvoiceDetailDto>> Get(string key)
        {
            if (!EntityRouteKey.TryParse(key, out var publicId, out var id))
            {
                return NotFound();
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var invoice = await dbContext.Invoices.AsNoTracking()
                .Where(x => x.TenantId == tenantContext.TenantId)
                .Where(x => publicId != default ? x.PublicId == publicId : x.Id == id)
                .Select(x => new InvoiceDetailDto
                {
                    Id = x.Id,
                    PublicId = x.PublicId,
                    InvoiceNumber = x.InvoiceNumber,
                    CompanyId = x.CompanyId,
                    CompanyPublicId = x.Company.PublicId,
                    CareHomeId = x.CareHomeId,
                    CareHomePublicId = x.CareHome.PublicId,
                    FundingAuthorityId = x.FundingAuthorityId,
                    FundingAuthorityPublicId = x.FundingAuthority.PublicId,
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
                        ClientPublicId = l.Client.PublicId,
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
                        Description = l.Description,
                        AmountBasis = l.AmountBasis
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

            await receivableReadModel.EnrichDetailAsync(tenantContext.TenantId, invoice, HttpContext.RequestAborted);

            foreach (var line in invoice.Lines)
            {
                if (!string.IsNullOrWhiteSpace(line.AmountBasis))
                {
                    continue;
                }

                line.AmountBasis = InvoiceLineAmountBasis.Format(
                    line.EligibleDays,
                    line.RateFrequency,
                    line.RateAmount,
                    string.Equals(line.RateFrequency, "AdHoc", StringComparison.OrdinalIgnoreCase));
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
            var outcome = await TrySendInvoiceAsync(id);
            return outcome.HttpResult;
        }

        [HttpPost("bulk-send")]
        public async Task<ActionResult<BulkSendResultDto>> BulkSend(BulkSendRequest request)
        {
            var summary = new BulkSendResultDto();
            foreach (var id in request.InvoiceIds.Distinct())
            {
                var outcome = await TrySendInvoiceAsync(id, forBulk: true);
                summary.Items.Add(new BulkSendItemDto
                {
                    InvoiceId = id,
                    InvoiceNumber = outcome.InvoiceNumber ?? string.Empty,
                    Outcome = outcome.BulkOutcome,
                    Reason = outcome.Reason
                });

                switch (outcome.BulkOutcome)
                {
                    case "Succeeded":
                    case "Simulated":
                        summary.Succeeded++;
                        break;
                    case "Failed":
                        summary.Failed++;
                        break;
                    default:
                        summary.Skipped++;
                        break;
                }
            }

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
            var invoice = await dbContext.Invoices
                .Include(x => x.Lines)
                .Include(x => x.CreditNotes)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);
            if (invoice is null)
            {
                return NotFound();
            }

            if (!await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, invoice.CareHomeId))
            {
                return NotFound();
            }

            var voidError = InvoiceVoidRules.ValidateCanVoid(
                invoice.Status,
                invoice.PaymentStatus,
                invoice.CreditNotes.Count > 0);
            if (voidError is not null)
            {
                return BadRequest(new { message = voidError });
            }

            invoice.Status = "Void";
            var miscIds = invoice.Lines
                .Where(x => x.MiscChargeId.HasValue)
                .Select(x => x.MiscChargeId!.Value)
                .Distinct()
                .ToList();
            if (miscIds.Count > 0)
            {
                var charges = await dbContext.MiscCharges
                    .Where(x => x.TenantId == tenantContext.TenantId && miscIds.Contains(x.Id))
                    .ToListAsync();
                foreach (var charge in charges)
                {
                    charge.IsInvoiced = false;
                }
            }

            await dbContext.SaveChangesAsync();
            await audit.LogAsync("Invoice", id.ToString(), "Void", null, new { invoice.InvoiceNumber }, "Invoice voided.");
            return Ok(new { invoice.Id, invoice.Status });
        }

        private async Task<SendInvoiceOutcome> TrySendInvoiceAsync(int id, bool forBulk = false)
        {
            var invoice = await dbContext.Invoices
                .Include(x => x.Lines)
                .Include(x => x.InvoiceTemplate)
                .Include(x => x.CareHome)
                .Include(x => x.Tenant)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantContext.TenantId);

            if (invoice is null || !await userAccess.CanAccessCareHomeAsync(tenantContext.TenantId, invoice.CareHomeId))
            {
                return SendInvoiceOutcome.NotFound(id, forBulk);
            }

            if (invoice.Status == "Void")
            {
                return SendInvoiceOutcome.Skipped(id, invoice.InvoiceNumber, "A void invoice cannot be emailed.", forBulk, "Void.");
            }

            if (string.IsNullOrWhiteSpace(invoice.RecipientEmail))
            {
                return SendInvoiceOutcome.Skipped(id, invoice.InvoiceNumber, "This invoice has no recipient email.", forBulk, "No recipient email.");
            }

            var tenantPublicId = await TenantPublicIdAsync();
            var pdf = await pdfs.GetOrCreateInvoicePdfAsync(invoice, tenantPublicId);
            var result = await email.SendAsync(
                invoice.RecipientEmail,
                $"Invoice {invoice.InvoiceNumber}",
                $"Please find invoice {invoice.InvoiceNumber} attached.",
                $"invoice-{invoice.InvoiceNumber}.pdf",
                pdf);

            var sentAt = DateTimeOffset.UtcNow;
            var statusUpdated = false;
            string? statusNote = null;

            if (result.Success)
            {
                // Do not resurrect a concurrently voided invoice.
                var updated = await dbContext.Invoices
                    .Where(x => x.Id == invoice.Id
                        && x.TenantId == tenantContext.TenantId
                        && x.Status != "Void")
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, "Sent")
                        .SetProperty(x => x.SentAt, sentAt)
                        .SetProperty(x => x.PdfPath, invoice.PdfPath));
                statusUpdated = updated > 0;
                if (!statusUpdated)
                {
                    statusNote = "Email was sent but invoice status was not updated because the invoice is now void.";
                }
            }

            dbContext.EmailSendLogs.Add(new EmailSendLog
            {
                TenantId = tenantContext.TenantId,
                AttemptedAt = DateTimeOffset.UtcNow,
                DocumentType = "Invoice",
                DocumentId = invoice.Id,
                Recipient = invoice.RecipientEmail,
                Success = result.Success,
                Simulated = result.Simulated,
                ErrorMessage = result.ErrorMessage ?? statusNote
            });
            await dbContext.SaveChangesAsync();

            await audit.LogAsync(
                "Invoice",
                invoice.Id.ToString(),
                "Send",
                null,
                new { invoice.InvoiceNumber, result.Success, result.Simulated, statusUpdated },
                result.Success
                    ? (statusUpdated
                        ? $"Sent invoice {invoice.InvoiceNumber}."
                        : $"Sent invoice {invoice.InvoiceNumber} but status was not updated (voided concurrently).")
                    : $"Failed to send invoice {invoice.InvoiceNumber}.");

            if (!result.Success)
            {
                return SendInvoiceOutcome.Failed(id, invoice.InvoiceNumber, result.ErrorMessage ?? "Email failed.", forBulk);
            }

            return SendInvoiceOutcome.Succeeded(
                id,
                invoice.InvoiceNumber,
                result.Simulated,
                statusUpdated ? sentAt : null,
                forBulk,
                statusNote);
        }

        private async Task<Guid> TenantPublicIdAsync()
        {
            return await dbContext.Tenants
                .Where(x => x.Id == tenantContext.TenantId)
                .Select(x => x.PublicId)
                .FirstAsync();
        }

        private sealed class SendInvoiceOutcome
        {
            public required IActionResult HttpResult { get; init; }
            public string? InvoiceNumber { get; init; }
            public required string BulkOutcome { get; init; }
            public string? Reason { get; init; }

            public static SendInvoiceOutcome NotFound(int id, bool forBulk) => new()
            {
                HttpResult = new NotFoundResult(),
                BulkOutcome = "Skipped",
                Reason = "Not found."
            };

            public static SendInvoiceOutcome Skipped(
                int id,
                string invoiceNumber,
                string message,
                bool forBulk,
                string bulkReason) => new()
            {
                HttpResult = new BadRequestObjectResult(new { message }),
                InvoiceNumber = invoiceNumber,
                BulkOutcome = "Skipped",
                Reason = bulkReason
            };

            public static SendInvoiceOutcome Failed(
                int id,
                string invoiceNumber,
                string message,
                bool forBulk) => new()
            {
                HttpResult = new BadRequestObjectResult(new { message }),
                InvoiceNumber = invoiceNumber,
                BulkOutcome = "Failed",
                Reason = message
            };

            public static SendInvoiceOutcome Succeeded(
                int id,
                string invoiceNumber,
                bool simulated,
                DateTimeOffset? sentAt,
                bool forBulk,
                string? statusNote) => new()
            {
                HttpResult = new OkObjectResult(new
                {
                    simulated,
                    sentAt,
                    statusUpdated = sentAt.HasValue,
                    warning = statusNote
                }),
                InvoiceNumber = invoiceNumber,
                BulkOutcome = simulated ? "Simulated" : "Succeeded",
                Reason = statusNote
            };
        }
    }
}

