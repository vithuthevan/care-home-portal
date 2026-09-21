using System.Data;
using CareHome.Api.Abstractions;
using CareHome.Api.Data;
using CareHome.Api.Models;
using CareHome.Api.Payments.Dtos;
using CareHome.Api.Payments.Services;
using CareHome.Api.Reconciliation.Domain;
using CareHome.Api.Reconciliation.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Reconciliation.Services;

public sealed class ReconciliationService(
    CareHomeDbContext dbContext,
    PaymentService payments,
    ReconciliationMatchingEngine matchingEngine,
    IAuditWriter audit,
    TimeProvider timeProvider)
{
    public async Task<ReconciliationWorkspaceSummaryDto> GetWorkspaceAsync(
        int tenantId,
        string? statusFilter,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.BankTransactions.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.Direction == BankTransactionDirections.In);

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(t => t.Status == statusFilter.Trim());
        }
        else
        {
            query = query.Where(t =>
                t.Status == BankTransactionStatuses.Unreconciled
                || t.Status == BankTransactionStatuses.Suggested);
        }

        var txns = await query
            .OrderByDescending(t => t.TransactionDate)
            .Take(100)
            .Include(t => t.Suggestions.Where(s => s.Status == ReconciliationSuggestionStatuses.Active))
            .ThenInclude(s => s.Lines)
            .ThenInclude(l => l.Invoice)
            .ToListAsync(cancellationToken);

        var allOpen = await dbContext.BankTransactions.AsNoTracking()
            .Where(t => t.TenantId == tenantId
                        && t.Direction == BankTransactionDirections.In
                        && (t.Status == BankTransactionStatuses.Unreconciled
                            || t.Status == BankTransactionStatuses.Suggested))
            .Select(t => new { t.Status, SuggestionScore = t.Suggestions
                .Where(s => s.Status == ReconciliationSuggestionStatuses.Active)
                .Select(s => (int?)s.TotalScore)
                .FirstOrDefault() })
            .ToListAsync(cancellationToken);

        var summary = new ReconciliationWorkspaceSummaryDto
        {
            Unreconciled = allOpen.Count(x => x.Status == BankTransactionStatuses.Unreconciled),
            Suggested = allOpen.Count(x => x.Status == BankTransactionStatuses.Suggested),
            HighConfidence = allOpen.Count(x => x.SuggestionScore >= 90),
            NeedsReview = allOpen.Count(x => x.SuggestionScore is int s && s < 70),
            Transactions = txns.Select(MapWorkspace).ToList()
        };

        return summary;
    }

    public async Task ConfirmAsync(
        int tenantId,
        Guid bankTransactionPublicId,
        ConfirmReconciliationRequest request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var bankTxn = await dbContext.BankTransactions
            .Include(t => t.Suggestions)
            .ThenInclude(s => s.Lines)
            .ThenInclude(l => l.Invoice)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.PublicId == bankTransactionPublicId, cancellationToken)
            ?? throw new InvalidOperationException("Bank transaction not found.");

        if (bankTxn.Status == BankTransactionStatuses.Reconciled)
        {
            throw new InvalidOperationException("Transaction is already reconciled.");
        }

        if (bankTxn.Direction != BankTransactionDirections.In)
        {
            throw new InvalidOperationException("Only incoming transactions can be reconciled to payments.");
        }

        var existingRec = await dbContext.PaymentReconciliations
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId
                     && r.BankTransactionId == bankTxn.Id
                     && r.Status == PaymentReconciliationStatuses.Confirmed,
                cancellationToken);
        if (existingRec is not null)
        {
            throw new InvalidOperationException("Transaction was already reconciled.");
        }

        List<PaymentAllocationLineRequest> allocationLines;
        ReconciliationSuggestion? suggestion = null;
        int confidence = 0;
        string? explanation = null;
        ReconciliationMatchGroup? matchGroup = null;

        if (request.SuggestionPublicId is Guid suggestionId)
        {
            suggestion = bankTxn.Suggestions.FirstOrDefault(s => s.PublicId == suggestionId && s.Status == ReconciliationSuggestionStatuses.Active)
                ?? throw new InvalidOperationException("Suggestion not found.");

            allocationLines = suggestion.Lines.Select(l => new PaymentAllocationLineRequest
            {
                InvoicePublicId = l.Invoice.PublicId,
                Amount = l.SuggestedAmount
            }).ToList();

            confidence = suggestion.TotalScore;
            explanation = suggestion.ExplanationJson;
        }
        else if (request.ManualAllocations.Count > 0)
        {
            allocationLines = request.ManualAllocations;
            confidence = 0;
            var allocTotal = Money.Round(allocationLines.Sum(l => l.Amount));
            if (allocTotal > Money.Round(bankTxn.Amount))
            {
                throw new InvalidOperationException("Allocated amounts exceed the bank transaction amount.");
            }
        }
        else
        {
            throw new InvalidOperationException("A suggestion or manual allocation is required.");
        }

        var externalRef = $"bank-txn:{bankTxn.PublicId:D}";
        var paymentDetail = await payments.CreateBankImportPaymentAsync(
            tenantId,
            bankTxn.Amount,
            bankTxn.Currency,
            bankTxn.TransactionDate,
            externalRef,
            bankTxn.Reference,
            request.FundingAuthorityId,
            null,
            actorUserId,
            cancellationToken);

        if (allocationLines.Count > 0)
        {
            matchGroup = new ReconciliationMatchGroup
            {
                TenantId = tenantId,
                PublicId = Guid.NewGuid(),
                CreatedAt = timeProvider.GetUtcNow()
            };
            dbContext.ReconciliationMatchGroups.Add(matchGroup);

            paymentDetail = await payments.AllocateAsync(
                tenantId,
                paymentDetail.PublicId,
                new AllocatePaymentRequest { Allocations = allocationLines },
                actorUserId,
                cancellationToken);

            var invoicePublicIds = allocationLines.Select(l => l.InvoicePublicId).ToList();
            var invoices = await dbContext.Invoices
                .Where(i => i.TenantId == tenantId && invoicePublicIds.Contains(i.PublicId))
                .ToListAsync(cancellationToken);

            foreach (var line in allocationLines)
            {
                var inv = invoices.First(i => i.PublicId == line.InvoicePublicId);
                matchGroup.Lines.Add(new ReconciliationMatchGroupLine
                {
                    InvoiceId = inv.Id,
                    AllocatedAmount = line.Amount
                });
            }
        }

        var paymentEntity = await dbContext.Payments
            .FirstAsync(p => p.TenantId == tenantId && p.PublicId == paymentDetail.PublicId, cancellationToken);

        var reconciliation = new PaymentReconciliation
        {
            TenantId = tenantId,
            PublicId = Guid.NewGuid(),
            BankTransactionId = bankTxn.Id,
            PaymentId = paymentEntity.Id,
            MatchGroupId = matchGroup?.Id,
            ConfidenceScore = confidence,
            ExplanationJson = explanation,
            ConfirmedAt = timeProvider.GetUtcNow(),
            ConfirmedBy = actorUserId,
            Status = PaymentReconciliationStatuses.Confirmed
        };

        dbContext.PaymentReconciliations.Add(reconciliation);
        bankTxn.Status = BankTransactionStatuses.Reconciled;

        if (suggestion is not null)
        {
            suggestion.Status = ReconciliationSuggestionStatuses.Confirmed;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await audit.LogAsync(
            "PaymentReconciliation",
            reconciliation.PublicId.ToString("D"),
            "RECONCILIATION_CONFIRMED",
            null,
            new { BankTransactionPublicId = bankTxn.PublicId, PaymentPublicId = paymentDetail.PublicId, confidence },
            "Bank transaction reconciled.",
            cancellationToken,
            tenantId);
    }

    public async Task CreateUnappliedPaymentAsync(
        int tenantId,
        Guid bankTransactionPublicId,
        CreateUnappliedPaymentFromBankRequest request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var bankTxn = await dbContext.BankTransactions
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.PublicId == bankTransactionPublicId, cancellationToken)
            ?? throw new InvalidOperationException("Bank transaction not found.");

        if (bankTxn.Status == BankTransactionStatuses.Reconciled)
        {
            throw new InvalidOperationException("Transaction is already reconciled.");
        }

        var externalRef = $"bank-txn:{bankTxn.PublicId:D}";
        var paymentDetail = await payments.CreateBankImportPaymentAsync(
            tenantId,
            bankTxn.Amount,
            bankTxn.Currency,
            bankTxn.TransactionDate,
            externalRef,
            bankTxn.Reference,
            request.FundingAuthorityId,
            null,
            actorUserId,
            cancellationToken);

        bankTxn.Status = BankTransactionStatuses.Reconciled;
        var paymentEntity = await dbContext.Payments
            .FirstAsync(p => p.TenantId == tenantId && p.PublicId == paymentDetail.PublicId, cancellationToken);

        dbContext.PaymentReconciliations.Add(new PaymentReconciliation
        {
            TenantId = tenantId,
            PublicId = Guid.NewGuid(),
            BankTransactionId = bankTxn.Id,
            PaymentId = paymentEntity.Id,
            ConfidenceScore = 0,
            ConfirmedAt = timeProvider.GetUtcNow(),
            ConfirmedBy = actorUserId,
            Status = PaymentReconciliationStatuses.Confirmed
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        await audit.LogAsync(
            "Payment",
            paymentDetail.PublicId.ToString("D"),
            "UNAPPLIED_PAYMENT_CREATED",
            null,
            new { bankTxn.PublicId },
            "Unapplied payment created from bank transaction.",
            cancellationToken,
            tenantId);
    }

    public async Task IgnoreAsync(
        int tenantId,
        Guid bankTransactionPublicId,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var bankTxn = await dbContext.BankTransactions
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.PublicId == bankTransactionPublicId, cancellationToken)
            ?? throw new InvalidOperationException("Bank transaction not found.");

        bankTxn.Status = BankTransactionStatuses.Ignored;
        await dbContext.SaveChangesAsync(cancellationToken);

        await audit.LogAsync(
            "BankTransaction",
            bankTxn.PublicId.ToString("D"),
            "MATCH_REJECTED",
            null,
            null,
            "Bank transaction ignored for reconciliation.",
            cancellationToken,
            tenantId);
    }

    public async Task ReverseAsync(
        int tenantId,
        Guid bankTransactionPublicId,
        ReverseReconciliationRequest request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var rec = await dbContext.PaymentReconciliations
            .Include(r => r.BankTransaction)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(
                r => r.TenantId == tenantId
                     && r.BankTransaction.PublicId == bankTransactionPublicId
                     && r.Status == PaymentReconciliationStatuses.Confirmed,
                cancellationToken)
            ?? throw new InvalidOperationException("Active reconciliation not found.");

        var payment = rec.Payment;
        if (payment.Status != PaymentEntityStatuses.Reversed)
        {
            var activeAllocations = await dbContext.PaymentAllocations
                .Where(a => a.PaymentId == payment.Id && !a.IsReversed)
                .ToListAsync(cancellationToken);

            foreach (var alloc in activeAllocations)
            {
                await payments.ReverseAllocationAsync(
                    tenantId,
                    payment.PublicId,
                    alloc.PublicId,
                    new ReverseAllocationRequest { Reason = request.Reason },
                    actorUserId,
                    cancellationToken);
            }
        }

        rec.Status = PaymentReconciliationStatuses.Reversed;
        rec.ReversedAt = timeProvider.GetUtcNow();
        rec.ReversedBy = actorUserId;
        rec.ReversalReason = request.Reason?.Trim();
        rec.BankTransaction.Status = BankTransactionStatuses.Unreconciled;

        await dbContext.SaveChangesAsync(cancellationToken);
        await matchingEngine.GenerateSuggestionsAsync(tenantId, rec.BankTransactionId, cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await audit.LogAsync(
            "PaymentReconciliation",
            rec.PublicId.ToString("D"),
            "RECONCILIATION_REVERSED",
            null,
            new { request.Reason },
            "Reconciliation reversed.",
            cancellationToken,
            tenantId);
    }

    public async Task<BankTransactionDetailDto?> GetTransactionDetailAsync(
        int tenantId,
        Guid bankTransactionPublicId,
        CancellationToken cancellationToken = default)
    {
        var txn = await dbContext.BankTransactions.AsNoTracking()
            .Include(t => t.Suggestions.Where(s => s.Status == ReconciliationSuggestionStatuses.Active))
            .ThenInclude(s => s.Lines)
            .ThenInclude(l => l.Invoice)
            .Include(t => t.ActiveReconciliation)
            .ThenInclude(r => r!.MatchGroup)
            .ThenInclude(g => g!.Lines)
            .ThenInclude(l => l.Invoice)
            .Include(t => t.ActiveReconciliation)
            .ThenInclude(r => r!.Payment)
            .ThenInclude(p => p!.Allocations.Where(a => !a.IsReversed))
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.PublicId == bankTransactionPublicId, cancellationToken);

        if (txn is null)
        {
            return null;
        }

        var detail = new BankTransactionDetailDto
        {
            PublicId = txn.PublicId,
            TransactionDate = txn.TransactionDate,
            Amount = txn.Amount,
            Status = txn.Status,
            Reference = txn.Reference,
            Description = txn.Description,
            Counterparty = txn.Counterparty,
            Suggestions = txn.Suggestions
                .Where(s => s.Status == ReconciliationSuggestionStatuses.Active)
                .OrderByDescending(s => s.TotalScore)
                .Select(s =>
                {
                    var factors = ReconciliationMatchScorer.DeserializeFactors(s.ExplanationJson)
                        .Select(f => new MatchScoreFactorDto { Label = f.Label, Detail = f.Detail, Points = f.Points })
                        .ToList();
                    return new ReconciliationSuggestionDto
                    {
                        PublicId = s.PublicId,
                        TotalScore = s.TotalScore,
                        ConfidenceBand = ReconciliationMatchingEngine.ResolveConfidenceBand(s.TotalScore),
                        Factors = factors,
                        Lines = s.Lines.Select(l => new ReconciliationSuggestionLineDto
                        {
                            InvoicePublicId = l.Invoice.PublicId,
                            InvoiceNumber = l.Invoice.InvoiceNumber,
                            SuggestedAmount = l.SuggestedAmount
                        }).ToList()
                    };
                })
                .ToList()
        };

        var rec = txn.ActiveReconciliation;
        if (rec is not null)
        {
            detail.ReconciliationStatus = rec.Status;
            detail.ConfidenceScore = rec.ConfidenceScore;
            if (rec.Payment is not null)
            {
                detail.PaymentPublicId = rec.Payment.PublicId;
                var allocated = rec.Payment.Allocations.Where(a => !a.IsReversed).Sum(a => a.AllocatedAmount);
                detail.PaymentUnappliedAmount = Money.Round(rec.Payment.Amount - allocated);
            }

            if (rec.MatchGroup is not null)
            {
                detail.MatchGroupLines = rec.MatchGroup.Lines.Select(l => new ReconciliationMatchGroupLineDto
                {
                    InvoicePublicId = l.Invoice.PublicId,
                    InvoiceNumber = l.Invoice.InvoiceNumber,
                    AllocatedAmount = l.AllocatedAmount
                }).ToList();
            }
        }

        return detail;
    }

    public async Task<List<ManualInvoiceSearchResultDto>> SearchInvoicesAsync(
        int tenantId,
        string? search,
        decimal? amount,
        int? fundingAuthorityId,
        int? careHomeId,
        int? clientId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Invoices.AsNoTracking()
            .Where(i => i.TenantId == tenantId && i.Status != InvoiceStatuses.Void);

        if (fundingAuthorityId is int funderId)
        {
            query = query.Where(i => i.FundingAuthorityId == funderId);
        }

        if (careHomeId is int homeId)
        {
            query = query.Where(i => i.CareHomeId == homeId);
        }

        if (clientId is int residentId)
        {
            query = query.Where(i => i.Lines.Any(l => l.ClientId == residentId));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i =>
                i.InvoiceNumber.Contains(term)
                || i.SnapshotCareHomeName.Contains(term)
                || i.Lines.Any(l => l.SnapshotClientName.Contains(term))
                || i.FundingAuthority.Name.Contains(term));
        }

        var invoices = await query.OrderByDescending(i => i.InvoiceDate).Take(50)
            .Select(i => new
            {
                i.Id,
                i.PublicId,
                i.InvoiceNumber,
                i.TotalAmount,
                i.PaymentStatus,
                ResidentName = i.Lines.OrderBy(l => l.Id).Select(l => l.SnapshotClientName).FirstOrDefault(),
                i.SnapshotCareHomeName,
                FunderName = i.FundingAuthority.Name
            })
            .ToListAsync(cancellationToken);

        var ids = invoices.Select(i => i.Id).ToList();
        var credits = await dbContext.CreditNotes.AsNoTracking()
            .Where(c => c.TenantId == tenantId && !c.Status.Equals(CreditNoteStatuses.Void) && ids.Contains(c.InvoiceId))
            .GroupBy(c => c.InvoiceId)
            .Select(g => new { InvoiceId = g.Key, Credited = g.Sum(c => -c.TotalAmount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => Money.Round(x.Credited), cancellationToken);

        var allocated = await dbContext.PaymentAllocations.AsNoTracking()
            .Where(a => a.TenantId == tenantId && !a.IsReversed && ids.Contains(a.InvoiceId)
                        && a.Payment.Status != PaymentEntityStatuses.Reversed)
            .GroupBy(a => a.InvoiceId)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(a => a.AllocatedAmount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => Money.Round(x.Total), cancellationToken);

        return invoices
            .Select(inv =>
            {
                var outstanding = InvoiceAllocationCapacity.RemainingCollectible(
                    inv.TotalAmount,
                    credits.GetValueOrDefault(inv.Id),
                    inv.PaymentStatus,
                    allocated.GetValueOrDefault(inv.Id));
                return new { inv, outstanding };
            })
            .Where(x => x.outstanding > 0)
            .Where(x => amount is null || Math.Abs(x.outstanding - amount.Value) < 0.01m || x.outstanding >= amount)
            .Select(x => new ManualInvoiceSearchResultDto
            {
                InvoicePublicId = x.inv.PublicId,
                InvoiceNumber = x.inv.InvoiceNumber,
                ResidentName = x.inv.ResidentName,
                FunderName = x.inv.FunderName,
                CareHomeName = x.inv.SnapshotCareHomeName,
                OutstandingAmount = x.outstanding
            })
            .ToList();
    }

    private static BankTransactionWorkspaceDto MapWorkspace(BankTransaction txn)
    {
        var top = txn.Suggestions
            .Where(s => s.Status == ReconciliationSuggestionStatuses.Active)
            .OrderByDescending(s => s.TotalScore)
            .FirstOrDefault();

        ReconciliationSuggestionDto? suggestionDto = null;
        if (top is not null)
        {
            var factors = ReconciliationMatchScorer.DeserializeFactors(top.ExplanationJson)
                .Select(f => new MatchScoreFactorDto { Label = f.Label, Detail = f.Detail, Points = f.Points })
                .ToList();

            suggestionDto = new ReconciliationSuggestionDto
            {
                PublicId = top.PublicId,
                TotalScore = top.TotalScore,
                ConfidenceBand = ReconciliationMatchingEngine.ResolveConfidenceBand(top.TotalScore),
                Factors = factors,
                Lines = top.Lines.Select(l => new ReconciliationSuggestionLineDto
                {
                    InvoicePublicId = l.Invoice.PublicId,
                    InvoiceNumber = l.Invoice.InvoiceNumber,
                    SuggestedAmount = l.SuggestedAmount
                }).ToList()
            };
        }

        return new BankTransactionWorkspaceDto
        {
            PublicId = txn.PublicId,
            TransactionDate = txn.TransactionDate,
            Amount = txn.Amount,
            Direction = txn.Direction,
            Reference = txn.Reference,
            Description = txn.Description,
            Counterparty = txn.Counterparty,
            Status = txn.Status,
            TopSuggestion = suggestionDto
        };
    }
}
