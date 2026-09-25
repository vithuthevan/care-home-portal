using CareHome.Api.Common;
using CareHome.Api.Dtos.Common;
using Xunit;

namespace CareHome.Api.Tests;

public class DefaultInvoiceCategoriesTests
{
    [Theory]
    [InlineData("MISC", true)]
    [InlineData("GENERAL_CARE", true)]
    [InlineData("CUSTOM", false)]
    public void IsSystemDefaultCode_identifies_provisioned_categories(string code, bool expected)
    {
        Assert.Equal(expected, DefaultInvoiceCategories.IsSystemDefaultCode(code));
    }
}

public class MasterDataUsageDtoTests
{
    [Fact]
    public void TotalFinancialReferences_sums_known_fields()
    {
        var usage = new MasterDataUsageDto
        {
            FundingContractCount = 2,
            InvoiceCount = 3,
            InvoiceLineSnapshotCount = 1
        };

        Assert.Equal(6, usage.TotalFinancialReferences);
        Assert.True(usage.HasFinancialReferences);
    }

    [Fact]
    public void TotalFinancialReferences_includes_pinned_contracts()
    {
        var usage = new MasterDataUsageDto
        {
            PinnedContractCount = 2,
            InvoiceCount = 1
        };

        Assert.Equal(3, usage.TotalFinancialReferences);
    }
}
