using CareHome.Api.Common;
using CareHome.Api.Dtos.Billing;

namespace CareHome.Billing.Billing;

public static class BillingInvoiceGrouper
{
    internal sealed record InvoiceGroupKey(
        int? CompanyId,
        int CareHomeId,
        int FundingAuthorityId,
        int InvoiceCategoryId,
        int ResidentId,
        DateOnly? CycleStart);

    internal static IEnumerable<IGrouping<InvoiceGroupKey, BillingPreviewLineDto>> GroupLines(
        IReadOnlyList<BillingPreviewLineDto> lines) =>
        lines.GroupBy(x => new InvoiceGroupKey(
            x.CompanyId,
            x.CareHomeId,
            x.FundingAuthorityId,
            x.InvoiceCategoryId,
            InvoiceGroupingModes.IsPerResident(x.GroupingMode) ? x.ClientId : 0,
            x.CycleStart));

    public static List<BillingInvoiceGroupPreviewDto> BuildGroupPreviews(
        IReadOnlyList<BillingPreviewLineDto> lines,
        DateOnly requestPeriodStart,
        DateOnly requestPeriodEnd)
    {
        var groups = new List<BillingInvoiceGroupPreviewDto>();
        foreach (var group in GroupLines(lines))
        {
            var first = group.First();
            var splitInvoice = first.CycleStart.HasValue
                || InvoiceGroupingModes.IsPerResident(first.GroupingMode);
            var periodStart = splitInvoice ? group.Min(x => x.ServiceFrom) : requestPeriodStart;
            var periodEnd = splitInvoice ? group.Max(x => x.ServiceTo) : requestPeriodEnd;

            groups.Add(new BillingInvoiceGroupPreviewDto
            {
                CompanyId = first.CompanyId,
                CompanyName = first.CompanyName,
                CareHomeId = first.CareHomeId,
                CareHomeName = first.CareHomeName,
                FundingAuthorityId = first.FundingAuthorityId,
                FundingAuthorityName = first.FundingAuthorityName,
                InvoiceCategoryId = first.InvoiceCategoryId,
                InvoiceCategoryName = first.InvoiceCategoryName,
                ClientId = InvoiceGroupingModes.IsPerResident(first.GroupingMode) ? first.ClientId : null,
                ClientName = InvoiceGroupingModes.IsPerResident(first.GroupingMode) ? first.ClientName : null,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                LineCount = group.Count(),
                SubtotalAmount = Money.Round(group.Sum(x => x.Amount))
            });
        }

        return groups;
    }
}
