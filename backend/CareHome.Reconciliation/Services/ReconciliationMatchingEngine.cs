using CareHome.Api.Data;
using CareHome.Api.Models;
using CareHome.Api.Payments.Domain;
using CareHome.Api.Reconciliation.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Reconciliation.Services;

public sealed class ReconciliationMatchingEngine(CareHomeDbContext dbContext, TimeProvider timeProvider)
{
    public async Task GenerateSuggestionsAsync(int tenantId, int bankTransactionId, CancellationToken cancellationToken)
    {
        var txn = await dbContext.BankTransactions
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == bankTransactionId, cancellationToken);
        if (txn is null || txn.Direction != BankTransactionDirections.In)
        {
            return;
        }

        if (txn.Status == BankTransactionStatuses.Reconciled)
        {
            return;
        }

        await SupersedeActiveSuggestionsAsync(tenantId, txn.Id, cancellationToken);

        var candidates = await LoadInvoiceCandidatesAsync(tenantId, txn, cancellationToken);
        ReconciliationMatchCandidate? best = null;

        foreach (var invoice in candidates)
        {
            var single = ReconciliationMatchScorer.ScoreSingleInvoice(
                txn.Amount,
                txn.TransactionDate,
                txn.NormalizedReference ?? string.Empty,
                txn.Counterparty,
                invoice);
            if (single is not null && (best is null || single.TotalScore > best.TotalScore))
            {
                best = single;
            }
        }

        var multi = ReconciliationMatchScorer.ScoreMultiInvoiceExactSum(
            txn.Amount,
            txn.NormalizedReference ?? string.Empty,
            candidates);
        if (multi is not null && (best is null || multi.TotalScore > best.TotalScore))
        {
            best = multi;
        }

        if (best is null)
        {
            txn.Status = BankTransactionStatuses.Unreconciled;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var suggestion = new ReconciliationSuggestion
        {
            TenantId = tenantId,
            PublicId = Guid.NewGuid(),
            BankTransactionId = txn.Id,
            TotalScore = best.TotalScore,
            ExplanationJson = ReconciliationMatchScorer.SerializeFactors(best.Factors),
            Status = ReconciliationSuggestionStatuses.Active,
            CreatedAt = timeProvider.GetUtcNow()
        };

        foreach (var line in best.Lines)
        {
            var invoiceId = candidates.First(c => c.PublicId == line.InvoicePublicId).InvoiceId;
            suggestion.Lines.Add(new ReconciliationSuggestionLine
            {
                InvoiceId = invoiceId,
                SuggestedAmount = line.Amount
            });
        }

        dbContext.ReconciliationSuggestions.Add(suggestion);
        txn.Status = BankTransactionStatuses.Suggested;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static string ResolveConfidenceBand(int score) =>
        score >= 90 ? "High" : score >= 70 ? "Medium" : "Low";

    private async Task SupersedeActiveSuggestionsAsync(int tenantId, int bankTransactionId, CancellationToken cancellationToken)
    {
        var active = await dbContext.ReconciliationSuggestions
            .Where(s => s.TenantId == tenantId
                        && s.BankTransactionId == bankTransactionId
                        && s.Status == ReconciliationSuggestionStatuses.Active)
            .ToListAsync(cancellationToken);

        foreach (var s in active)
        {
            s.Status = ReconciliationSuggestionStatuses.Superseded;
        }
    }

    private async Task<List<InvoiceMatchTarget>> LoadInvoiceCandidatesAsync(
        int tenantId,
        BankTransaction txn,
        CancellationToken cancellationToken)
    {
        var windowStart = txn.TransactionDate.AddDays(-90);
        var windowEnd = txn.TransactionDate.AddDays(30);
        var amountMin = Money.Round(txn.Amount * 0.5m);
        var amountMax = Money.Round(txn.Amount * 1.5m);

        var invoices = await dbContext.Invoices.AsNoTracking()
            .Where(i => i.TenantId == tenantId
                        && i.Status != InvoiceStatuses.Void
                        && i.InvoiceDate >= windowStart
                        && i.InvoiceDate <= windowEnd)
            .Select(i => new
            {
                i.Id,
                i.PublicId,
                i.InvoiceNumber,
                i.TotalAmount,
                i.PaymentStatus,
                i.InvoiceDate,
                FunderName = i.FundingAuthority.Name,
                i.FundingAuthorityId
            })
            .Take(500)
            .ToListAsync(cancellationToken);

        if (invoices.Count == 0)
        {
            return [];
        }

        var invoiceIds = invoices.Select(i => i.Id).ToList();
        var credits = await dbContext.CreditNotes.AsNoTracking()
            .Where(c => c.TenantId == tenantId && !c.Status.Equals(CreditNoteStatuses.Void) && invoiceIds.Contains(c.InvoiceId))
            .GroupBy(c => c.InvoiceId)
            .Select(g => new { InvoiceId = g.Key, Credited = g.Sum(c => -c.TotalAmount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => Money.Round(x.Credited), cancellationToken);

        var allocated = await dbContext.PaymentAllocations.AsNoTracking()
            .Where(a => a.TenantId == tenantId
                        && !a.IsReversed
                        && invoiceIds.Contains(a.InvoiceId)
                        && a.Payment.Status != PaymentEntityStatuses.Reversed)
            .GroupBy(a => a.InvoiceId)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(a => a.AllocatedAmount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => Money.Round(x.Total), cancellationToken);

        var normalized = txn.NormalizedReference ?? string.Empty;
        var results = new List<InvoiceMatchTarget>();
        foreach (var inv in invoices)
        {
            var outstanding = InvoiceAllocationCapacity.RemainingCollectible(
                inv.TotalAmount,
                credits.GetValueOrDefault(inv.Id),
                inv.PaymentStatus,
                allocated.GetValueOrDefault(inv.Id));

            if (outstanding <= 0)
            {
                continue;
            }

            if (outstanding < amountMin || outstanding > amountMax)
            {
                if (!normalized.Contains(inv.InvoiceNumber, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            results.Add(new InvoiceMatchTarget(
                inv.Id,
                inv.PublicId,
                inv.InvoiceNumber,
                outstanding,
                inv.InvoiceDate,
                inv.FunderName,
                inv.FundingAuthorityId));
        }

        return results;
    }
}
