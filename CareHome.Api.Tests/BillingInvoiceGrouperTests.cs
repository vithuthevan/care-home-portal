using CareHome.Api.Common;
using CareHome.Api.Dtos.Billing;
using CareHome.Billing.Billing;
using Xunit;

namespace CareHome.Api.Tests;

public class BillingInvoiceGrouperTests
{
    [Fact]
    public void Per_resident_grouping_splits_clients()
    {
        var lines = new List<BillingPreviewLineDto>
        {
            Line(1, "Alice", InvoiceGroupingModes.PerResident, 100m),
            Line(2, "Bob", InvoiceGroupingModes.PerResident, 200m),
        };

        var groups = BillingInvoiceGrouper.BuildGroupPreviews(lines, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 7));
        Assert.Equal(2, groups.Count);
        Assert.Equal(100m, groups.Single(g => g.ClientName == "Alice").SubtotalAmount);
        Assert.Equal(200m, groups.Single(g => g.ClientName == "Bob").SubtotalAmount);
    }

    [Fact]
    public void Per_funder_grouping_combines_residents()
    {
        var lines = new List<BillingPreviewLineDto>
        {
            Line(1, "Alice", InvoiceGroupingModes.PerFunder, 100m),
            Line(2, "Bob", InvoiceGroupingModes.PerFunder, 200m),
        };

        var groups = BillingInvoiceGrouper.BuildGroupPreviews(lines, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 7));
        Assert.Single(groups);
        Assert.Equal(300m, groups[0].SubtotalAmount);
        Assert.Equal(2, groups[0].LineCount);
    }

    private static BillingPreviewLineDto Line(int clientId, string name, string grouping, decimal amount) =>
        new()
        {
            ClientId = clientId,
            ClientName = name,
            CareHomeId = 1,
            CareHomeName = "Home",
            CompanyId = 1,
            CompanyName = "Co",
            FundingAuthorityId = 1,
            FundingAuthorityName = "Funder",
            InvoiceCategoryId = 1,
            InvoiceCategoryName = "Care",
            GroupingMode = grouping,
            ServiceFrom = new DateOnly(2026, 5, 1),
            ServiceTo = new DateOnly(2026, 5, 7),
            Amount = amount,
            NominalCode = "4000"
        };
}
