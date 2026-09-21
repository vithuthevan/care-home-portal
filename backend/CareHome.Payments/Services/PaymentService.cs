using System.Data;
using CareHome.Api.Abstractions;
using CareHome.Api.Data;
using CareHome.Api.Models;
using CareHome.Api.Payments.Domain;
using CareHome.Api.Payments.Dtos;
using CareHome.Api.Telemetry;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Payments.Services;

public sealed class PaymentService(
    CareHomeDbContext dbContext,
    ICareHomeAccessScope accessScope,
    IAuditWriter audit,
    TimeProvider timeProvider)
{
    public async Task<PaymentDetailDto> CreateManualAsync(
        int tenantId,
        CreatePaymentRequest request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateAmount(request.Amount);

        if (!await CanManagePaymentsForScopeAsync(tenantId, request.CareHomeId, cancellationToken))
        {
            throw new InvalidOperationException("You do not have permission to record payments for this scope.");
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? await ResolveTenantCurrencyAsync(tenantId, cancellationToken)
            : request.Currency.Trim().ToUpperInvariant();

        var now = timeProvider.GetUtcNow();
        var payment = new Payment
        {
            TenantId = tenantId,
            PublicId = Guid.NewGuid(),
            FundingAuthorityId = request.FundingAuthorityId,
            CareHomeId = request.CareHomeId,
            Amount = Money.Round(request.Amount),
            Currency = currency,
            ReceivedDate = request.ReceivedDate,
            Reference = request.Reference?.Trim(),
            Source = PaymentSources.Manual,
            Status = PaymentEntityStatuses.Received,
            Notes = request.Notes?.Trim(),
            CreatedAt = now,
            CreatedBy = actorUserId
        };

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        await audit.LogAsync(
            "Payment",
            payment.PublicId.ToString("D"),
            "PAYMENT_CREATED",
            null,
            new { payment.Amount, payment.Currency, payment.Reference, payment.ReceivedDate },
            "Manual payment recorded.",
            cancellationToken,
            tenantId);

        CareHomeTelemetry.PaymentCreated.Add(1);

        if (request.InitialAllocations is { Count: > 0 })
        {
            await AllocateInternalAsync(
                tenantId,
                payment,
                request.InitialAllocations,
                actorUserId,
                cancellationToken);
        }

        return await MapDetailAsync(tenantId, payment.PublicId, cancellationToken)
            ?? throw new InvalidOperationException("Payment was not found after creation.");
    }

    public async Task<PaymentDetailDto?> GetByPublicIdAsync(
        int tenantId,
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        if (!await CanViewPaymentAsync(tenantId, publicId, cancellationToken))
        {
            return null;
        }

        return await MapDetailAsync(tenantId, publicId, cancellationToken);
    }

    public async Task<(List<PaymentListDto> Items, int Total)> ListAsync(
        int tenantId,
        string? statusFilter,
        bool unappliedOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var scopedHomes = await accessScope.GetScopedCareHomeIdsAsync(tenantId, cancellationToken);
        var unrestricted = await accessScope.GetAllowedCareHomeIdsAsync(cancellationToken) is null;

        var query = dbContext.Payments.AsNoTracking()
            .Where(p => p.TenantId == tenantId);

        if (!unrestricted)
        {
            query = query.Where(p =>
                p.CareHomeId == null
                    ? p.Allocations.Any(a =>
                        !a.IsReversed
                        && scopedHomes.Contains(a.Invoice.CareHomeId))
                    : scopedHomes.Contains(p.CareHomeId.Value));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(p => p.Status == statusFilter.Trim());
        }

        if (unappliedOnly)
        {
            query = query.Where(p =>
                p.Status != PaymentEntityStatuses.Reversed
                && p.Amount > p.Allocations.Where(a => !a.IsReversed).Sum(a => a.AllocatedAmount));
        }

        var total = await query.CountAsync(cancellationToken);
        var payments = await query
            .OrderByDescending(p => p.ReceivedDate)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.FundingAuthority)
            .Include(p => p.Allocations.Where(a => !a.IsReversed))
            .ToListAsync(cancellationToken);

        var items = payments.Select(MapList).ToList();
        return (items, total);
    }

    public async Task<PaymentDetailDto> AllocateAsync(
        int tenantId,
        Guid paymentPublicId,
        AllocatePaymentRequest request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.Payments
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.PublicId == paymentPublicId, cancellationToken)
            ?? throw new InvalidOperationException("Payment not found.");

        await AllocateInternalAsync(tenantId, payment, request.Allocations, actorUserId, cancellationToken);

        return await MapDetailAsync(tenantId, paymentPublicId, cancellationToken)
            ?? throw new InvalidOperationException("Payment not found after allocation.");
    }

    public async Task<PaymentDetailDto> ReversePaymentAsync(
        int tenantId,
        Guid paymentPublicId,
        ReversePaymentRequest request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var payment = await dbContext.Payments
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.PublicId == paymentPublicId, cancellationToken)
            ?? throw new InvalidOperationException("Payment not found.");

        if (payment.Status == PaymentEntityStatuses.Reversed)
        {
            throw new InvalidOperationException("Payment is already reversed.");
        }

        if (!await CanManagePaymentEntityAsync(tenantId, payment, cancellationToken))
        {
            throw new InvalidOperationException("You do not have permission to reverse this payment.");
        }

        var now = timeProvider.GetUtcNow();
        payment.Status = PaymentEntityStatuses.Reversed;
        payment.ReversedAt = now;
        payment.ReversedBy = actorUserId;
        payment.ReversalReason = request.Reason?.Trim();
        payment.UpdatedAt = now;
        payment.UpdatedBy = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await audit.LogAsync(
            "Payment",
            payment.PublicId.ToString("D"),
            "PAYMENT_REVERSED",
            null,
            new { request.Reason },
            "Payment reversed.",
            cancellationToken,
            tenantId);

        CareHomeTelemetry.PaymentReversal.Add(1);

        return await MapDetailAsync(tenantId, paymentPublicId, cancellationToken)
            ?? throw new InvalidOperationException("Payment not found after reversal.");
    }

    public async Task<PaymentDetailDto> ReverseAllocationAsync(
        int tenantId,
        Guid paymentPublicId,
        Guid allocationPublicId,
        ReverseAllocationRequest request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var payment = await dbContext.Payments
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.PublicId == paymentPublicId, cancellationToken)
            ?? throw new InvalidOperationException("Payment not found.");

        if (payment.Status == PaymentEntityStatuses.Reversed)
        {
            throw new InvalidOperationException("Cannot reverse allocations on a reversed payment.");
        }

        if (!await CanManagePaymentEntityAsync(tenantId, payment, cancellationToken))
        {
            throw new InvalidOperationException("You do not have permission to correct this allocation.");
        }

        var allocation = payment.Allocations.FirstOrDefault(a => a.PublicId == allocationPublicId)
            ?? throw new InvalidOperationException("Allocation not found.");

        if (allocation.IsReversed)
        {
            throw new InvalidOperationException("Allocation is already reversed.");
        }

        var now = timeProvider.GetUtcNow();
        allocation.IsReversed = true;
        allocation.ReversedAt = now;
        allocation.ReversedBy = actorUserId;
        allocation.ReversalReason = request.Reason?.Trim();

        payment.Status = DerivePaymentStatus(payment);
        payment.UpdatedAt = now;
        payment.UpdatedBy = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await audit.LogAsync(
            "PaymentAllocation",
            allocation.PublicId.ToString("D"),
            "PAYMENT_ALLOCATION_REVERSED",
            null,
            new { paymentPublicId, allocation.AllocatedAmount, request.Reason },
            "Payment allocation reversed.",
            cancellationToken,
            tenantId);

        CareHomeTelemetry.PaymentAllocationReversal.Add(1);

        return await MapDetailAsync(tenantId, paymentPublicId, cancellationToken)
            ?? throw new InvalidOperationException("Payment not found after allocation reversal.");
    }

    public async Task<PaymentDetailDto> CreateBankImportPaymentAsync(
        int tenantId,
        decimal amount,
        string currency,
        DateOnly receivedDate,
        string externalReference,
        string? reference,
        int? fundingAuthorityId,
        int? careHomeId,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateAmount(amount);
        var ext = externalReference.Trim();
        var existing = await dbContext.Payments
            .FirstOrDefaultAsync(
                p => p.TenantId == tenantId
                     && p.Source == PaymentSources.BankImport
                     && p.ExternalReference == ext,
                cancellationToken);
        if (existing is not null)
        {
            return await MapDetailAsync(tenantId, existing.PublicId, cancellationToken)
                ?? throw new InvalidOperationException("Payment not found.");
        }

        var now = timeProvider.GetUtcNow();
        var payment = new Payment
        {
            TenantId = tenantId,
            PublicId = Guid.NewGuid(),
            FundingAuthorityId = fundingAuthorityId,
            CareHomeId = careHomeId,
            Amount = Money.Round(amount),
            Currency = currency,
            ReceivedDate = receivedDate,
            Reference = reference?.Trim(),
            ExternalReference = ext,
            Source = PaymentSources.BankImport,
            Status = PaymentEntityStatuses.Received,
            CreatedAt = now,
            CreatedBy = actorUserId
        };

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        await audit.LogAsync(
            "Payment",
            payment.PublicId.ToString("D"),
            "PAYMENT_CREATED",
            null,
            new { payment.Amount, payment.Source, payment.ExternalReference },
            "Payment created from bank import.",
            cancellationToken,
            tenantId);

        CareHomeTelemetry.PaymentCreated.Add(1);

        return await MapDetailAsync(tenantId, payment.PublicId, cancellationToken)
            ?? throw new InvalidOperationException("Payment was not found after creation.");
    }

    public async Task<PaymentDetailDto> CreateRemittancePaymentAsync(
        int tenantId,
        decimal amount,
        string currency,
        DateOnly receivedDate,
        string externalReference,
        string? reference,
        int? fundingAuthorityId,
        int? careHomeId,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateAmount(amount);
        var ext = externalReference.Trim();
        var existing = await dbContext.Payments
            .FirstOrDefaultAsync(
                p => p.TenantId == tenantId
                     && p.Source == PaymentSources.Remittance
                     && p.ExternalReference == ext,
                cancellationToken);
        if (existing is not null)
        {
            return await MapDetailAsync(tenantId, existing.PublicId, cancellationToken)
                ?? throw new InvalidOperationException("Payment not found.");
        }

        var now = timeProvider.GetUtcNow();
        var payment = new Payment
        {
            TenantId = tenantId,
            PublicId = Guid.NewGuid(),
            FundingAuthorityId = fundingAuthorityId,
            CareHomeId = careHomeId,
            Amount = Money.Round(amount),
            Currency = currency,
            ReceivedDate = receivedDate,
            Reference = reference?.Trim(),
            ExternalReference = ext,
            Source = PaymentSources.Remittance,
            Status = PaymentEntityStatuses.Received,
            CreatedAt = now,
            CreatedBy = actorUserId
        };

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        await audit.LogAsync(
            "Payment",
            payment.PublicId.ToString("D"),
            "PAYMENT_CREATED",
            null,
            new { payment.Amount, payment.Source, payment.ExternalReference },
            "Payment created from remittance.",
            cancellationToken,
            tenantId);

        CareHomeTelemetry.PaymentCreated.Add(1);

        return await MapDetailAsync(tenantId, payment.PublicId, cancellationToken)
            ?? throw new InvalidOperationException("Payment was not found after creation.");
    }

    public async Task<List<PaymentAllocationCandidateDto>> ListAllocationCandidatesAsync(
        int tenantId,
        Guid paymentPublicId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.PublicId == paymentPublicId, cancellationToken);
        if (payment is null || payment.Status == PaymentEntityStatuses.Reversed)
        {
            return [];
        }

        if (!await CanViewPaymentAsync(tenantId, paymentPublicId, cancellationToken))
        {
            return [];
        }

        var unapplied = await GetUnappliedAmountAsync(payment, cancellationToken);
        if (unapplied <= 0)
        {
            return [];
        }

        var scopedHomes = await accessScope.GetScopedCareHomeIdsAsync(tenantId, cancellationToken);
        var unrestricted = await accessScope.GetAllowedCareHomeIdsAsync(cancellationToken) is null;

        var query = dbContext.Invoices.AsNoTracking()
            .Where(i => i.TenantId == tenantId && i.Status != InvoiceStatuses.Void);

        if (!unrestricted)
        {
            query = query.Where(i => scopedHomes.Contains(i.CareHomeId));
        }

        if (payment.FundingAuthorityId is int funderId)
        {
            query = query.Where(i => i.FundingAuthorityId == funderId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i =>
                i.InvoiceNumber.Contains(term)
                || i.SnapshotCareHomeName.Contains(term)
                || i.Lines.Any(l => l.SnapshotClientName.Contains(term)));
        }

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Take(100)
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

        if (invoices.Count == 0)
        {
            return [];
        }

        var invoiceIds = invoices.Select(i => i.Id).ToList();
        var credits = await LoadCreditTotalsAsync(tenantId, invoiceIds, cancellationToken);
        var allocated = await LoadActiveAllocationTotalsAsync(tenantId, invoiceIds, cancellationToken);

        var results = new List<PaymentAllocationCandidateDto>();
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

            results.Add(new PaymentAllocationCandidateDto
            {
                InvoicePublicId = inv.PublicId,
                InvoiceNumber = inv.InvoiceNumber,
                ResidentName = inv.ResidentName,
                FunderName = inv.FunderName,
                CareHomeName = inv.SnapshotCareHomeName,
                OutstandingAmount = outstanding,
                DefaultAllocationAmount = Money.Round(Math.Min(outstanding, unapplied))
            });
        }

        return results
            .OrderByDescending(x => x.OutstandingAmount)
            .ThenBy(x => x.InvoiceNumber)
            .ToList();
    }

    public async Task<List<PaymentAllocationSuggestionDto>> SuggestAllocationsAsync(
        int tenantId,
        Guid paymentPublicId,
        CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.PublicId == paymentPublicId, cancellationToken);
        if (payment is null || payment.Status == PaymentEntityStatuses.Reversed)
        {
            return [];
        }

        var unapplied = await GetUnappliedAmountAsync(payment, cancellationToken);
        if (unapplied <= 0 || string.IsNullOrWhiteSpace(payment.Reference))
        {
            return [];
        }

        var refTerm = payment.Reference.Trim();
        var invoices = await dbContext.Invoices.AsNoTracking()
            .Where(i => i.TenantId == tenantId && i.Status != InvoiceStatuses.Void && i.InvoiceNumber.Contains(refTerm))
            .Take(20)
            .Select(i => new { i.PublicId, i.InvoiceNumber, i.TotalAmount, i.PaymentStatus, i.Id })
            .ToListAsync(cancellationToken);

        if (invoices.Count == 0)
        {
            return [];
        }

        var credits = await LoadCreditTotalsAsync(tenantId, invoices.Select(i => i.Id).ToList(), cancellationToken);
        var allocated = await LoadActiveAllocationTotalsAsync(tenantId, invoices.Select(i => i.Id).ToList(), cancellationToken);

        var suggestions = new List<PaymentAllocationSuggestionDto>();
        foreach (var inv in invoices)
        {
            var credited = credits.GetValueOrDefault(inv.Id);
            var alloc = allocated.GetValueOrDefault(inv.Id);
            var outstanding = InvoiceAllocationCapacity.RemainingCollectible(
                inv.TotalAmount,
                credited,
                inv.PaymentStatus,
                alloc);

            if (outstanding <= 0)
            {
                continue;
            }

            if (outstanding != unapplied)
            {
                continue;
            }

            suggestions.Add(new PaymentAllocationSuggestionDto
            {
                InvoicePublicId = inv.PublicId,
                InvoiceNumber = inv.InvoiceNumber,
                OutstandingAmount = outstanding,
                SuggestedAmount = Math.Min(outstanding, unapplied),
                Reason = "Reference matches invoice number and amount equals outstanding balance."
            });
        }

        return suggestions;
    }

    private async Task AllocateInternalAsync(
        int tenantId,
        Payment payment,
        IReadOnlyList<PaymentAllocationLineRequest> lines,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("At least one allocation line is required.");
        }

        if (payment.Status == PaymentEntityStatuses.Reversed)
        {
            throw new InvalidOperationException("Cannot allocate to a reversed payment.");
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        await dbContext.Entry(payment).ReloadAsync(cancellationToken);

        if (!await CanManagePaymentEntityAsync(tenantId, payment, cancellationToken))
        {
            throw new InvalidOperationException("You do not have permission to allocate this payment.");
        }

        var invoicePublicIds = lines.Select(l => l.InvoicePublicId).Distinct().ToList();
        var invoices = await dbContext.Invoices
            .Where(i => i.TenantId == tenantId && invoicePublicIds.Contains(i.PublicId))
            .ToListAsync(cancellationToken);

        if (invoices.Count != invoicePublicIds.Count)
        {
            CareHomeTelemetry.PaymentAllocationFailed.Add(1);
            throw new InvalidOperationException("One or more invoices were not found for this tenant.");
        }

        var invoiceIds = invoices.Select(i => i.Id).ToList();
        var credits = await LoadCreditTotalsAsync(tenantId, invoiceIds, cancellationToken);
        var allocated = await LoadActiveAllocationTotalsAsync(tenantId, invoiceIds, cancellationToken);

        var paymentAllocated = await dbContext.PaymentAllocations
            .Where(a => a.PaymentId == payment.Id && !a.IsReversed)
            .SumAsync(a => a.AllocatedAmount, cancellationToken);

        var paymentRemaining = Money.Round(payment.Amount - paymentAllocated);
        var totalNew = Money.Round(lines.Sum(l => l.Amount));

        if (totalNew > paymentRemaining)
        {
            CareHomeTelemetry.PaymentAllocationFailed.Add(1);
            throw new InvalidOperationException(
                $"Allocation total {totalNew:0.00} exceeds unapplied payment balance {paymentRemaining:0.00}.");
        }

        var now = timeProvider.GetUtcNow();
        foreach (var line in lines)
        {
            ValidateAmount(line.Amount);
            var invoice = invoices.First(i => i.PublicId == line.InvoicePublicId);

            if (invoice.Status == InvoiceStatuses.Void)
            {
                CareHomeTelemetry.PaymentAllocationFailed.Add(1);
                throw new InvalidOperationException($"Invoice {invoice.InvoiceNumber} is void and cannot receive allocations.");
            }

            if (invoice.TenantId != tenantId)
            {
                CareHomeTelemetry.PaymentAllocationFailed.Add(1);
                throw new InvalidOperationException("Invoice tenant mismatch.");
            }

            var priorAlloc = allocated.GetValueOrDefault(invoice.Id);
            var remaining = InvoiceAllocationCapacity.RemainingCollectible(
                invoice.TotalAmount,
                credits.GetValueOrDefault(invoice.Id),
                invoice.PaymentStatus,
                priorAlloc);

            if (line.Amount > remaining)
            {
                CareHomeTelemetry.PaymentAllocationFailed.Add(1);
                throw new InvalidOperationException(
                    $"Allocation {line.Amount:0.00} exceeds remaining collectible balance {remaining:0.00} on invoice {invoice.InvoiceNumber}.");
            }

            var allocation = new PaymentAllocation
            {
                TenantId = tenantId,
                PublicId = Guid.NewGuid(),
                PaymentId = payment.Id,
                InvoiceId = invoice.Id,
                AllocatedAmount = Money.Round(line.Amount),
                AllocatedAt = now,
                AllocatedBy = actorUserId
            };

            dbContext.PaymentAllocations.Add(allocation);
            allocated[invoice.Id] = priorAlloc + allocation.AllocatedAmount;

            await audit.LogAsync(
                "PaymentAllocation",
                allocation.PublicId.ToString("D"),
                "PAYMENT_ALLOCATED",
                null,
                new
                {
                    payment.PublicId,
                    invoice.InvoiceNumber,
                    allocation.AllocatedAmount
                },
                "Payment allocated to invoice.",
                cancellationToken,
                tenantId);

            CareHomeTelemetry.PaymentAllocationCreated.Add(1);
        }

        payment.Status = DerivePaymentStatus(payment, paymentAllocated + totalNew);
        payment.UpdatedAt = now;
        payment.UpdatedBy = actorUserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private static string DerivePaymentStatus(Payment payment, decimal? activeAllocatedOverride = null)
    {
        if (payment.Status == PaymentEntityStatuses.Reversed)
        {
            return PaymentEntityStatuses.Reversed;
        }

        var activeAllocated = activeAllocatedOverride
            ?? payment.Allocations.Where(a => !a.IsReversed).Sum(a => a.AllocatedAmount);

        activeAllocated = Money.Round(activeAllocated);
        if (activeAllocated <= 0m)
        {
            return PaymentEntityStatuses.Received;
        }

        if (activeAllocated >= payment.Amount)
        {
            return PaymentEntityStatuses.Allocated;
        }

        return PaymentEntityStatuses.PartiallyAllocated;
    }

    private async Task<decimal> GetUnappliedAmountAsync(Payment payment, CancellationToken cancellationToken)
    {
        var allocated = await dbContext.PaymentAllocations.AsNoTracking()
            .Where(a => a.PaymentId == payment.Id && !a.IsReversed)
            .SumAsync(a => a.AllocatedAmount, cancellationToken);
        return Money.Round(Math.Max(0m, payment.Amount - allocated));
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new InvalidOperationException("Amount must be greater than zero.");
        }
    }

    private async Task<string> ResolveTenantCurrencyAsync(int tenantId, CancellationToken cancellationToken)
    {
        var code = await dbContext.TenantSettings.AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .Select(s => s.CurrencyCode)
            .FirstOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(code) ? "GBP" : code;
    }

    private async Task<Dictionary<int, decimal>> LoadCreditTotalsAsync(
        int tenantId,
        List<int> invoiceIds,
        CancellationToken cancellationToken)
    {
        return await dbContext.CreditNotes.AsNoTracking()
            .Where(c => c.TenantId == tenantId && !c.Status.Equals(CreditNoteStatuses.Void) && invoiceIds.Contains(c.InvoiceId))
            .GroupBy(c => c.InvoiceId)
            .Select(g => new { InvoiceId = g.Key, Credited = g.Sum(c => -c.TotalAmount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => Money.Round(x.Credited), cancellationToken);
    }

    private async Task<Dictionary<int, decimal>> LoadActiveAllocationTotalsAsync(
        int tenantId,
        List<int> invoiceIds,
        CancellationToken cancellationToken)
    {
        return await dbContext.PaymentAllocations.AsNoTracking()
            .Where(a => a.TenantId == tenantId
                        && !a.IsReversed
                        && invoiceIds.Contains(a.InvoiceId)
                        && a.Payment.Status != PaymentEntityStatuses.Reversed)
            .GroupBy(a => a.InvoiceId)
            .Select(g => new { InvoiceId = g.Key, Total = g.Sum(a => a.AllocatedAmount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => Money.Round(x.Total), cancellationToken);
    }

    private async Task<bool> CanViewPaymentAsync(int tenantId, Guid publicId, CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.PublicId == publicId)
            .Select(p => new { p.Id, p.CareHomeId })
            .FirstOrDefaultAsync(cancellationToken);
        if (payment is null)
        {
            return false;
        }

        if (await accessScope.GetAllowedCareHomeIdsAsync(cancellationToken) is null)
        {
            return true;
        }

        if (payment.CareHomeId is int homeId
            && await accessScope.CanAccessCareHomeAsync(tenantId, homeId, cancellationToken))
        {
            return true;
        }

        var scopedHomes = await accessScope.GetScopedCareHomeIdsAsync(tenantId, cancellationToken);
        return await dbContext.PaymentAllocations.AsNoTracking()
            .AnyAsync(
                a => a.PaymentId == payment.Id
                     && !a.IsReversed
                     && scopedHomes.Contains(a.Invoice.CareHomeId),
                cancellationToken);
    }

    private async Task<bool> CanManagePaymentEntityAsync(
        int tenantId,
        Payment payment,
        CancellationToken cancellationToken)
    {
        if (payment.CareHomeId is int homeId)
        {
            return await accessScope.CanAccessCareHomeAsync(tenantId, homeId, cancellationToken);
        }

        var unrestricted = await accessScope.GetAllowedCareHomeIdsAsync(cancellationToken) is null;
        if (unrestricted)
        {
            return true;
        }

        var invoiceHomes = await dbContext.PaymentAllocations.AsNoTracking()
            .Where(a => a.PaymentId == payment.Id && !a.IsReversed)
            .Select(a => a.Invoice.CareHomeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (invoiceHomes.Count == 0)
        {
            return false;
        }

        foreach (var home in invoiceHomes)
        {
            if (!await accessScope.CanAccessCareHomeAsync(tenantId, home, cancellationToken))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> CanManagePaymentsForScopeAsync(
        int tenantId,
        int? careHomeId,
        CancellationToken cancellationToken)
    {
        if (careHomeId is null)
        {
            return await accessScope.GetAllowedCareHomeIdsAsync(cancellationToken) is null;
        }

        return await accessScope.CanAccessCareHomeAsync(tenantId, careHomeId.Value, cancellationToken);
    }

    private async Task<PaymentDetailDto?> MapDetailAsync(
        int tenantId,
        Guid publicId,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.Payments.AsNoTracking()
            .Include(p => p.FundingAuthority)
            .Include(p => p.Allocations)
            .ThenInclude(a => a.Invoice)
            .ThenInclude(i => i.Lines)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.PublicId == publicId, cancellationToken);

        if (payment is null)
        {
            return null;
        }

        var activeAllocations = payment.Allocations.Where(a => !a.IsReversed).ToList();
        var invoiceIds = activeAllocations.Select(a => a.InvoiceId).Distinct().ToList();
        var credits = await LoadCreditTotalsAsync(tenantId, invoiceIds, cancellationToken);
        var allocatedBefore = await LoadActiveAllocationTotalsAsync(tenantId, invoiceIds, cancellationToken);

        var dto = new PaymentDetailDto
        {
            PublicId = payment.PublicId,
            ReceivedDate = payment.ReceivedDate,
            Reference = payment.Reference,
            PayerName = payment.FundingAuthority?.Name,
            Amount = payment.Amount,
            AllocatedAmount = Money.Round(activeAllocations.Sum(a => a.AllocatedAmount)),
            UnappliedAmount = await GetUnappliedAmountAsync(payment, cancellationToken),
            Currency = payment.Currency,
            Status = payment.Status,
            Source = payment.Source,
            FundingAuthorityId = payment.FundingAuthorityId,
            CareHomeId = payment.CareHomeId,
            ExternalReference = payment.ExternalReference,
            Notes = payment.Notes,
            CreatedAt = payment.CreatedAt,
            ReversedAt = payment.ReversedAt,
            ReversalReason = payment.ReversalReason
        };

        foreach (var alloc in activeAllocations.OrderBy(a => a.AllocatedAt))
        {
            var inv = alloc.Invoice;
            var credited = credits.GetValueOrDefault(inv.Id);
            var totalAlloc = allocatedBefore.GetValueOrDefault(inv.Id);
            var outstandingBefore = InvoiceAllocationCapacity.RemainingCollectible(
                inv.TotalAmount,
                credited,
                inv.PaymentStatus,
                totalAlloc - alloc.AllocatedAmount);
            var outstandingAfter = InvoiceAllocationCapacity.RemainingCollectible(
                inv.TotalAmount,
                credited,
                inv.PaymentStatus,
                totalAlloc);

            dto.Allocations.Add(new PaymentAllocationDto
            {
                PublicId = alloc.PublicId,
                InvoicePublicId = inv.PublicId,
                InvoiceNumber = inv.InvoiceNumber,
                ResidentName = inv.Lines.OrderBy(l => l.Id).Select(l => l.SnapshotClientName).FirstOrDefault(),
                CareHomeName = inv.SnapshotCareHomeName,
                InvoiceTotal = inv.TotalAmount,
                OutstandingBefore = outstandingBefore,
                AllocatedAmount = alloc.AllocatedAmount,
                OutstandingAfter = outstandingAfter,
                IsReversed = alloc.IsReversed,
                AllocatedAt = alloc.AllocatedAt
            });
        }

        return dto;
    }

    private static PaymentListDto MapList(Payment payment)
    {
        var allocated = Money.Round(payment.Allocations.Where(a => !a.IsReversed).Sum(a => a.AllocatedAmount));
        return new PaymentListDto
        {
            PublicId = payment.PublicId,
            ReceivedDate = payment.ReceivedDate,
            Reference = payment.Reference,
            PayerName = payment.FundingAuthority?.Name,
            Amount = payment.Amount,
            AllocatedAmount = allocated,
            UnappliedAmount = Money.Round(Math.Max(0m, payment.Amount - allocated)),
            Currency = payment.Currency,
            Status = payment.Status,
            Source = payment.Source
        };
    }
}
