using CareHome.Api.Abstractions;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Invoices;
using CareHome.Api.Receivables.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Services;

public sealed class InvoiceReceivableReadModel(
    CareHomeDbContext dbContext,
    IAllocatedPaymentQuery allocatedPayments,
    TimeProvider timeProvider)
{
    public async Task EnrichListAsync(int tenantId, IList<InvoiceListDto> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        var invoiceIds = items.Select(i => i.Id).ToList();
        var snapshots = await LoadSnapshotsAsync(tenantId, invoiceIds, cancellationToken);
        var asOf = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var netBilled = await LoadNetBilledAmountsAsync(tenantId, invoiceIds, cancellationToken);

        foreach (var item in items)
        {
            if (netBilled.TryGetValue(item.Id, out var net))
            {
                item.NetBilledAmount = net;
            }

            if (!snapshots.TryGetValue(item.Id, out var snap))
            {
                continue;
            }

            Apply(item, snap, asOf);
        }
    }

    public async Task EnrichDetailAsync(int tenantId, InvoiceDetailDto item, CancellationToken cancellationToken)
    {
        var snapshots = await LoadSnapshotsAsync(tenantId, [item.Id], cancellationToken);
        var netBilled = await LoadNetBilledAmountsAsync(tenantId, [item.Id], cancellationToken);
        if (netBilled.TryGetValue(item.Id, out var net))
        {
            item.NetBilledAmount = net;
        }

        if (snapshots.TryGetValue(item.Id, out var snap))
        {
            var asOf = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            Apply(item, snap, asOf);
        }
    }

    private async Task<Dictionary<int, decimal>> LoadNetBilledAmountsAsync(
        int tenantId,
        List<int> invoiceIds,
        CancellationToken cancellationToken)
    {
        if (invoiceIds.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        var lineParts = await dbContext.InvoiceLines.AsNoTracking()
            .Where(l => l.Invoice.TenantId == tenantId && invoiceIds.Contains(l.InvoiceId))
            .Select(l => new
            {
                l.InvoiceId,
                l.LineAmount,
                Credits = l.CreditNoteLines
                    .Where(c => c.CreditNote.Status != CreditNoteStatuses.Void)
                    .Sum(c => c.Amount)
            })
            .ToListAsync(cancellationToken);

        return lineParts
            .GroupBy(x => x.InvoiceId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => InvoiceLineNetAmount.FromParts(x.LineAmount, x.Credits)));
    }

    private static void Apply(InvoiceListDto item, InvoiceReceivableSnapshot snap, DateOnly asOf)
    {
        item.CollectionStatus = ReceivableCollectionStatuses.Resolve(snap.Amounts, snap.DueDate, asOf);
        item.PaidAmount = snap.Amounts.PaidAmount;
        item.CreditedAmount = snap.Amounts.CreditedAmount;
        item.OutstandingAmount = snap.Amounts.OutstandingAmount;
        item.IsOverdue = snap.Amounts.OutstandingAmount > 0 && snap.DueDate < asOf;
    }

    private async Task<Dictionary<int, InvoiceReceivableSnapshot>> LoadSnapshotsAsync(
        int tenantId,
        List<int> invoiceIds,
        CancellationToken cancellationToken)
    {
        var invoices = await dbContext.Invoices.AsNoTracking()
            .Where(i => i.TenantId == tenantId && invoiceIds.Contains(i.Id))
            .Select(i => new
            {
                i.Id,
                i.TotalAmount,
                i.PaymentStatus,
                i.DueDate
            })
            .ToListAsync(cancellationToken);

        var credits = await dbContext.CreditNotes.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Status != CreditNoteStatuses.Void && invoiceIds.Contains(c.InvoiceId))
            .GroupBy(c => c.InvoiceId)
            .Select(g => new { InvoiceId = g.Key, Credited = g.Sum(c => -c.TotalAmount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => Money.Round(x.Credited), cancellationToken);

        var allocated = await allocatedPayments.GetActiveAllocatedAmountsForInvoicesAsync(
            tenantId,
            invoiceIds,
            cancellationToken);

        return invoices.ToDictionary(
            i => i.Id,
            i =>
            {
                var credited = credits.GetValueOrDefault(i.Id);
                var alloc = allocated.GetValueOrDefault(i.Id);
                var amounts = ReceivableBalance.Calculate(i.TotalAmount, credited, i.PaymentStatus, alloc);
                return new InvoiceReceivableSnapshot(amounts, i.DueDate);
            });
    }

    private sealed record InvoiceReceivableSnapshot(ReceivableAmounts Amounts, DateOnly DueDate);
}
