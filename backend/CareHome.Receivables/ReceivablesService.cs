using CareHome.Api.Abstractions;
using CareHome.Api.Data;
using CareHome.Api.Models;
using CareHome.Api.Receivables.Contracts;
using CareHome.Api.Receivables.Domain;
using CareHome.Api.Receivables.Dtos;
using CareHome.Api.Telemetry;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Receivables;

public sealed class ReceivablesService(
    CareHomeDbContext dbContext,
    ICareHomeAccessScope accessScope,
    IAllocatedPaymentQuery allocatedPayments,
    TimeProvider timeProvider) : IReceivablesService
{
    private sealed class ReceivableSourceRow
    {
        public int InvoiceId { get; init; }

        public Guid PublicId { get; init; }

        public string InvoiceNumber { get; init; } = string.Empty;

        public int CareHomeId { get; init; }

        public string CareHomeName { get; init; } = string.Empty;

        public string CareHomeCode { get; init; } = string.Empty;

        public int? CompanyId { get; init; }

        public string CompanyName { get; init; } = string.Empty;

        public int FundingAuthorityId { get; init; }

        public string FunderName { get; init; } = string.Empty;

        public string FunderCode { get; init; } = string.Empty;

        public string? ResidentName { get; init; }

        public DateOnly InvoiceDate { get; init; }

        public DateOnly DueDate { get; init; }

        public string DocumentStatus { get; init; } = string.Empty;

        public decimal GrossAmount { get; init; }

        public decimal CreditedAmount { get; init; }

        public string StoredPaymentStatus { get; init; } = string.Empty;
    }

    public async Task<ReceivablesSummaryDto> GetTenantSummaryAsync(
        int tenantId,
        ReceivableInvoiceQuery? filters = null,
        CancellationToken cancellationToken = default)
    {
        CareHomeTelemetry.ReceivablesOutstandingQuery.Add(1);
        var asOf = ResolveAsOf(filters);
        var rows = await LoadComputedRowsAsync(tenantId, filters, asOf, cancellationToken);
        return BuildSummary(rows, asOf);
    }

    public async Task<ReceivablesAgeingDto> GetAgeingAsync(
        int tenantId,
        ReceivableInvoiceQuery? filters = null,
        CancellationToken cancellationToken = default)
    {
        CareHomeTelemetry.ReceivablesAgeingQuery.Add(1);
        var asOf = ResolveAsOf(filters);
        var rows = await LoadComputedRowsAsync(tenantId, filters, asOf, cancellationToken);
        return SumAgeing(rows);
    }

    public async Task<(List<ReceivableInvoiceDto> Items, int TotalCount)> ListInvoicesAsync(
        int tenantId,
        ReceivableInvoiceQuery query,
        CancellationToken cancellationToken = default)
    {
        CareHomeTelemetry.ReceivablesOutstandingQuery.Add(1);
        var asOf = ResolveAsOf(query);
        var rows = await LoadComputedRowsAsync(tenantId, query, asOf, cancellationToken);
        var filtered = ApplyReceivableFilters(rows, query, asOf).ToList();

        query.Page = Math.Max(query.Page, 1);
        query.PageSize = Math.Clamp(query.PageSize, 1, 200);
        var total = filtered.Count;
        var page = filtered
            .OrderByDescending(x => x.DueDate)
            .ThenByDescending(x => x.OutstandingAmount)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return (page, total);
    }

    public async Task<List<FunderReceivableSummaryDto>> ListFunderSummariesAsync(
        int tenantId,
        ReceivableInvoiceQuery? filters = null,
        CancellationToken cancellationToken = default)
    {
        var asOf = ResolveAsOf(filters);
        var rows = await LoadComputedRowsAsync(tenantId, filters, asOf, cancellationToken);
        return rows
            .GroupBy(x => new { x.FundingAuthorityId, x.FunderName, x.FunderCode })
            .Select(g =>
            {
                var open = g.Where(x => x.OutstandingAmount > 0).ToList();
                var oldest = open.OrderBy(x => x.DueDate).FirstOrDefault();
                return new FunderReceivableSummaryDto
                {
                    FundingAuthorityId = g.Key.FundingAuthorityId,
                    FunderName = g.Key.FunderName,
                    FunderCode = g.Key.FunderCode,
                    TotalInvoiced = Money.Round(g.Sum(x => x.OriginalAmount)),
                    TotalOutstanding = Money.Round(open.Sum(x => x.OutstandingAmount)),
                    Ageing = SumAgeing(open),
                    OldestUnpaidDueDate = oldest?.DueDate,
                    OldestUnpaidInvoiceNumber = oldest?.InvoiceNumber
                };
            })
            .OrderByDescending(x => x.TotalOutstanding)
            .ToList();
    }

    public async Task<CareHomeReceivableSummaryDto?> GetCareHomeSummaryAsync(
        int tenantId,
        int careHomeId,
        ReceivableInvoiceQuery? filters = null,
        CancellationToken cancellationToken = default)
    {
        if (!await accessScope.CanAccessCareHomeAsync(tenantId, careHomeId, cancellationToken))
        {
            return null;
        }

        filters ??= new ReceivableInvoiceQuery();
        filters.CareHomeId = careHomeId;
        var asOf = ResolveAsOf(filters);
        var rows = await LoadComputedRowsAsync(tenantId, filters, asOf, cancellationToken);
        var homeRows = rows.Where(x => x.CareHomeId == careHomeId).ToList();
        if (homeRows.Count == 0)
        {
            var exists = await dbContext.CareHomes.AsNoTracking()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == careHomeId, cancellationToken);
            if (!exists)
            {
                return null;
            }

            var home = await dbContext.CareHomes.AsNoTracking()
                .FirstAsync(x => x.TenantId == tenantId && x.Id == careHomeId, cancellationToken);
            return new CareHomeReceivableSummaryDto
            {
                CareHomeId = home.Id,
                CareHomeName = home.Name,
                CareHomeCode = home.Code,
                OpenInvoiceCount = 0
            };
        }

        var sample = homeRows[0];
        var open = homeRows.Where(x => x.OutstandingAmount > 0).ToList();
        return new CareHomeReceivableSummaryDto
        {
            CareHomeId = careHomeId,
            CareHomeName = sample.CareHomeName,
            CareHomeCode = sample.CareHomeCode,
            TotalInvoiced = Money.Round(homeRows.Sum(x => x.OriginalAmount)),
            TotalOutstanding = Money.Round(open.Sum(x => x.OutstandingAmount)),
            TotalOverdue = Money.Round(open.Where(x => x.DaysOverdue > 0).Sum(x => x.OutstandingAmount)),
            Ageing = SumAgeing(open),
            OpenInvoiceCount = open.Count
        };
    }

    public async Task<List<CareHomeReceivableSummaryDto>> ListCareHomeSummariesAsync(
        int tenantId,
        ReceivableInvoiceQuery? filters = null,
        CancellationToken cancellationToken = default)
    {
        var asOf = ResolveAsOf(filters);
        var rows = await LoadComputedRowsAsync(tenantId, filters, asOf, cancellationToken);
        return rows
            .GroupBy(x => new { x.CareHomeId, x.CareHomeName, x.CareHomeCode })
            .Select(g =>
            {
                var open = g.Where(x => x.OutstandingAmount > 0).ToList();
                return new CareHomeReceivableSummaryDto
                {
                    CareHomeId = g.Key.CareHomeId,
                    CareHomeName = g.Key.CareHomeName,
                    CareHomeCode = g.Key.CareHomeCode,
                    TotalInvoiced = Money.Round(g.Sum(x => x.OriginalAmount)),
                    TotalOutstanding = Money.Round(open.Sum(x => x.OutstandingAmount)),
                    TotalOverdue = Money.Round(open.Where(x => x.DaysOverdue > 0).Sum(x => x.OutstandingAmount)),
                    Ageing = SumAgeing(open),
                    OpenInvoiceCount = open.Count
                };
            })
            .OrderByDescending(x => x.TotalOutstanding)
            .ToList();
    }

    private DateOnly ResolveAsOf(ReceivableInvoiceQuery? query)
    {
        if (query?.AsOfDate is { } asOf)
        {
            return asOf;
        }

        return DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
    }

    private async Task<List<ReceivableInvoiceDto>> LoadComputedRowsAsync(
        int tenantId,
        ReceivableInvoiceQuery? query,
        DateOnly asOf,
        CancellationToken cancellationToken)
    {
        query ??= new ReceivableInvoiceQuery();
        var homes = await accessScope.GetScopedCareHomeIdsAsync(tenantId, cancellationToken);
        if (homes.Count == 0)
        {
            return [];
        }

        var invoices = dbContext.Invoices.AsNoTracking()
            .Where(x => x.TenantId == tenantId && homes.Contains(x.CareHomeId));

        if (query.CompanyId.HasValue)
        {
            invoices = invoices.Where(x => x.CompanyId == query.CompanyId);
        }

        if (query.CareHomeId.HasValue)
        {
            invoices = invoices.Where(x => x.CareHomeId == query.CareHomeId);
        }

        if (query.FundingAuthorityId.HasValue)
        {
            invoices = invoices.Where(x => x.FundingAuthorityId == query.FundingAuthorityId);
        }

        if (!string.IsNullOrWhiteSpace(query.DocumentStatus))
        {
            invoices = invoices.Where(x => x.Status == query.DocumentStatus.Trim());
        }
        else
        {
            invoices = invoices.Where(x => x.Status != InvoiceStatuses.Void);
        }

        if (query.InvoiceDateFrom.HasValue)
        {
            invoices = invoices.Where(x => x.InvoiceDate >= query.InvoiceDateFrom);
        }

        if (query.InvoiceDateTo.HasValue)
        {
            invoices = invoices.Where(x => x.InvoiceDate <= query.InvoiceDateTo);
        }

        if (query.DueDateFrom.HasValue)
        {
            invoices = invoices.Where(x => x.DueDate >= query.DueDateFrom);
        }

        if (query.DueDateTo.HasValue)
        {
            invoices = invoices.Where(x => x.DueDate <= query.DueDateTo);
        }

        if (!string.IsNullOrWhiteSpace(query.InvoiceNumber))
        {
            var term = query.InvoiceNumber.Trim();
            invoices = invoices.Where(x => x.InvoiceNumber.Contains(term));
        }

        var creditTotals = dbContext.CreditNotes.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Status != CreditNoteStatuses.Void)
            .GroupBy(c => c.InvoiceId)
            .Select(g => new { InvoiceId = g.Key, Credited = g.Sum(c => -c.TotalAmount) });

        var sourceRows = await (
            from i in invoices
            join c in creditTotals on i.Id equals c.InvoiceId into credits
            from c in credits.DefaultIfEmpty()
            select new ReceivableSourceRow
            {
                InvoiceId = i.Id,
                PublicId = i.PublicId,
                InvoiceNumber = i.InvoiceNumber,
                CareHomeId = i.CareHomeId,
                CareHomeName = i.SnapshotCareHomeName,
                CareHomeCode = i.SnapshotCareHomeCode,
                CompanyId = i.CompanyId,
                CompanyName = i.SnapshotCompanyName,
                FundingAuthorityId = i.FundingAuthorityId,
                FunderName = i.SnapshotFundingAuthorityName,
                FunderCode = i.SnapshotFundingAuthorityCode,
                ResidentName = i.Lines
                    .OrderBy(l => l.Id)
                    .Select(l => l.SnapshotClientName)
                    .FirstOrDefault(),
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                DocumentStatus = i.Status,
                GrossAmount = i.TotalAmount,
                CreditedAmount = c == null ? 0m : c.Credited,
                StoredPaymentStatus = i.PaymentStatus
            }).ToListAsync(cancellationToken);

        var invoiceIds = sourceRows.Select(r => r.InvoiceId).ToList();
        var allocationTotals = await allocatedPayments.GetActiveAllocatedAmountsForInvoicesAsync(
            tenantId,
            invoiceIds,
            cancellationToken);

        return sourceRows.Select(r => MapRow(r, asOf, allocationTotals.GetValueOrDefault(r.InvoiceId))).ToList();
    }

    private static ReceivableInvoiceDto MapRow(ReceivableSourceRow row, DateOnly asOf, decimal allocatedPayments = 0m)
    {
        var amounts = ReceivableBalance.Calculate(
            row.GrossAmount,
            row.CreditedAmount,
            row.StoredPaymentStatus,
            allocatedPayments);

        var daysOverdue = ReceivableAgeing.DaysOverdue(row.DueDate, asOf);
        var paymentStatus = ReceivableCollectionStatuses.Resolve(amounts, row.DueDate, asOf);
        var bucket = amounts.OutstandingAmount > 0
            ? ReceivableAgeing.ResolveBucket(row.DueDate, asOf)
            : ReceivableAgeingBucket.Current;

        return new ReceivableInvoiceDto
        {
            InvoiceId = row.InvoiceId,
            PublicId = row.PublicId,
            InvoiceNumber = row.InvoiceNumber,
            CareHomeId = row.CareHomeId,
            CareHomeName = row.CareHomeName,
            CareHomeCode = row.CareHomeCode,
            CompanyId = row.CompanyId,
            CompanyName = row.CompanyName,
            FundingAuthorityId = row.FundingAuthorityId,
            FunderName = row.FunderName,
            FunderCode = row.FunderCode,
            ResidentName = row.ResidentName,
            InvoiceDate = row.InvoiceDate,
            DueDate = row.DueDate,
            DocumentStatus = row.DocumentStatus,
            OriginalAmount = amounts.OriginalAmount,
            CreditedAmount = amounts.CreditedAmount,
            PaidAmount = amounts.PaidAmount,
            OutstandingAmount = amounts.OutstandingAmount,
            DaysOverdue = daysOverdue,
            PaymentStatus = paymentStatus,
            AgeingBucket = bucket
        };
    }

    private static IEnumerable<ReceivableInvoiceDto> ApplyReceivableFilters(
        IEnumerable<ReceivableInvoiceDto> rows,
        ReceivableInvoiceQuery query,
        DateOnly asOf)
    {
        foreach (var row in rows)
        {
            if (query.OpenReceivablesOnly && row.OutstandingAmount <= 0)
            {
                continue;
            }

            if (query.OverdueOnly == true
                && row.PaymentStatus != ReceivableCollectionStatuses.Overdue
                && !(row.OutstandingAmount > 0 && row.DueDate < asOf))
            {
                continue;
            }

            if (query.AgeingBucket.HasValue && row.AgeingBucket != query.AgeingBucket)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(query.PaymentStatus)
                && !string.Equals(row.PaymentStatus, query.PaymentStatus.Trim(), StringComparison.Ordinal))
            {
                continue;
            }

            yield return row;
        }
    }

    private static ReceivablesSummaryDto BuildSummary(List<ReceivableInvoiceDto> rows, DateOnly asOf)
    {
        var open = rows.Where(x => x.OutstandingAmount > 0).ToList();
        var weekEnd = asOf.AddDays(7);
        return new ReceivablesSummaryDto
        {
            TotalInvoiced = Money.Round(rows.Sum(x => x.OriginalAmount)),
            TotalOutstanding = Money.Round(open.Sum(x => x.OutstandingAmount)),
            TotalOverdue = Money.Round(open.Where(x => x.DaysOverdue > 0).Sum(x => x.OutstandingAmount)),
            DueThisWeek = Money.Round(open
                .Where(x => x.DueDate >= asOf && x.DueDate <= weekEnd)
                .Sum(x => x.OutstandingAmount)),
            Days90Plus = Money.Round(open
                .Where(x => x.AgeingBucket == ReceivableAgeingBucket.Days90Plus)
                .Sum(x => x.OutstandingAmount)),
            Ageing = SumAgeing(open),
            OpenInvoiceCount = open.Count
        };
    }

    private static ReceivablesAgeingDto SumAgeing(IEnumerable<ReceivableInvoiceDto> openRows)
    {
        var ageing = new ReceivablesAgeingDto();
        foreach (var row in openRows.Where(x => x.OutstandingAmount > 0))
        {
            switch (row.AgeingBucket)
            {
                case ReceivableAgeingBucket.Current:
                    ageing.Current += row.OutstandingAmount;
                    break;
                case ReceivableAgeingBucket.Days1To30:
                    ageing.Days1To30 += row.OutstandingAmount;
                    break;
                case ReceivableAgeingBucket.Days31To60:
                    ageing.Days31To60 += row.OutstandingAmount;
                    break;
                case ReceivableAgeingBucket.Days61To90:
                    ageing.Days61To90 += row.OutstandingAmount;
                    break;
                case ReceivableAgeingBucket.Days90Plus:
                    ageing.Days90Plus += row.OutstandingAmount;
                    break;
            }
        }

        ageing.Current = Money.Round(ageing.Current);
        ageing.Days1To30 = Money.Round(ageing.Days1To30);
        ageing.Days31To60 = Money.Round(ageing.Days31To60);
        ageing.Days61To90 = Money.Round(ageing.Days61To90);
        ageing.Days90Plus = Money.Round(ageing.Days90Plus);
        return ageing;
    }
}
