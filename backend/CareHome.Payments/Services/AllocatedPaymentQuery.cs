using CareHome.Api.Abstractions;
using CareHome.Api.Data;
using CareHome.Api.Models;
using CareHome.Api.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Payments.Services;

public sealed class AllocatedPaymentQuery(CareHomeDbContext dbContext) : IAllocatedPaymentQuery
{
    public async Task<IReadOnlyDictionary<int, decimal>> GetActiveAllocatedAmountsForInvoicesAsync(
        int tenantId,
        IReadOnlyCollection<int> invoiceIds,
        CancellationToken cancellationToken = default)
    {
        if (invoiceIds.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        var idList = invoiceIds.Distinct().ToList();
        var rows = await dbContext.PaymentAllocations.AsNoTracking()
            .Where(a => a.TenantId == tenantId
                        && !a.IsReversed
                        && idList.Contains(a.InvoiceId)
                        && a.Payment.Status != PaymentEntityStatuses.Reversed)
            .GroupBy(a => a.InvoiceId)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(a => a.AllocatedAmount) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.InvoiceId, x => Money.Round(x.Total));
    }
}
