using CareHome.Api.Abstractions;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Disputes;
using CareHome.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Services;

public sealed class DisputeWorkflowService(
    CareHomeDbContext dbContext,
    IAuditWriter audit,
    TimeProvider timeProvider)
{
    public async Task<List<DisputeListDto>> ListAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.InvoiceDisputes.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.Status != InvoiceDisputeStatuses.Closed)
            .OrderByDescending(d => d.OpenedDate)
            .Take(200)
            .Select(d => new DisputeListDto
            {
                PublicId = d.PublicId,
                InvoiceNumber = d.Invoice.InvoiceNumber,
                FunderName = d.FundingAuthority.Name,
                DisputedAmount = d.DisputedAmount,
                Status = d.Status,
                ReasonCode = d.ReasonCode,
                OpenedDate = d.OpenedDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DisputeDetailDto?> GetAsync(int tenantId, Guid publicId, CancellationToken cancellationToken = default)
    {
        var dispute = await dbContext.InvoiceDisputes.AsNoTracking()
            .Include(d => d.Messages)
            .Include(d => d.Invoice)
            .Include(d => d.FundingAuthority)
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.PublicId == publicId, cancellationToken);

        return dispute is null
            ? null
            : new DisputeDetailDto
            {
                PublicId = dispute.PublicId,
                InvoicePublicId = dispute.Invoice.PublicId,
                InvoiceNumber = dispute.Invoice.InvoiceNumber,
                FunderName = dispute.FundingAuthority.Name,
                DisputedAmount = dispute.DisputedAmount,
                Status = dispute.Status,
                ReasonCode = dispute.ReasonCode,
                OpenedDate = dispute.OpenedDate,
                Messages = dispute.Messages.OrderBy(m => m.CreatedAt).Select(m => new DisputeMessageDto
                {
                    PublicId = m.PublicId,
                    Body = m.Body,
                    CreatedAt = m.CreatedAt
                }).ToList()
            };
    }

    public async Task<DisputeDetailDto> OpenAsync(
        int tenantId,
        OpenDisputeRequest request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.PublicId == request.InvoicePublicId, cancellationToken)
            ?? throw new InvalidOperationException("Invoice not found.");

        var now = timeProvider.GetUtcNow();
        var dispute = new InvoiceDispute
        {
            TenantId = tenantId,
            PublicId = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            FundingAuthorityId = invoice.FundingAuthorityId,
            ReasonCode = request.ReasonCode.Trim(),
            DisputedAmount = Money.Round(request.DisputedAmount),
            Status = InvoiceDisputeStatuses.Open,
            OpenedDate = DateOnly.FromDateTime(now.UtcDateTime),
            DueDate = request.DueDate,
            RemittanceLineId = request.RemittanceLineId,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.InvoiceDisputes.Add(dispute);
        await dbContext.SaveChangesAsync(cancellationToken);

        await audit.LogAsync(
            "InvoiceDispute",
            dispute.PublicId.ToString("D"),
            "DISPUTE_OPENED",
            null,
            new { request.DisputedAmount, request.ReasonCode },
            "Invoice dispute opened.",
            cancellationToken,
            tenantId);

        return (await GetAsync(tenantId, dispute.PublicId, cancellationToken))!;
    }

    public async Task AddMessageAsync(
        int tenantId,
        Guid publicId,
        string body,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var dispute = await dbContext.InvoiceDisputes
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.PublicId == publicId, cancellationToken)
            ?? throw new InvalidOperationException("Dispute not found.");

        dbContext.DisputeMessages.Add(new DisputeMessage
        {
            TenantId = tenantId,
            PublicId = Guid.NewGuid(),
            InvoiceDisputeId = dispute.Id,
            Body = body.Trim(),
            AuthorUserId = actorUserId,
            CreatedAt = timeProvider.GetUtcNow()
        });

        dispute.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResolveAsync(
        int tenantId,
        Guid publicId,
        ResolveDisputeRequest request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var dispute = await dbContext.InvoiceDisputes
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.PublicId == publicId, cancellationToken)
            ?? throw new InvalidOperationException("Dispute not found.");

        dispute.Status = InvoiceDisputeStatuses.Resolved;
        dispute.Resolution = request.Resolution?.Trim();
        dispute.ResolvedDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        dispute.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);

        await audit.LogAsync(
            "InvoiceDispute",
            dispute.PublicId.ToString("D"),
            "DISPUTE_RESOLVED",
            null,
            new { request.Resolution },
            "Invoice dispute resolved.",
            cancellationToken,
            tenantId);
    }
}
