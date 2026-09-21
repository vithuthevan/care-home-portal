using CareHome.Api.Receivables.Domain;
using Xunit;

namespace CareHome.Api.Tests;

public class ReceivableAgeingTests
{
    private static readonly DateOnly AsOf = new(2026, 3, 1);

    [Theory]
    [InlineData("2026-03-02", 0, ReceivableAgeingBucket.Current)]
    [InlineData("2026-03-01", 0, ReceivableAgeingBucket.Current)]
    [InlineData("2026-02-28", 1, ReceivableAgeingBucket.Days1To30)]
    [InlineData("2026-01-30", 30, ReceivableAgeingBucket.Days1To30)]
    [InlineData("2026-01-29", 31, ReceivableAgeingBucket.Days31To60)]
    [InlineData("2025-12-31", 60, ReceivableAgeingBucket.Days31To60)]
    [InlineData("2025-12-30", 61, ReceivableAgeingBucket.Days61To90)]
    [InlineData("2025-12-01", 90, ReceivableAgeingBucket.Days61To90)]
    [InlineData("2025-11-30", 91, ReceivableAgeingBucket.Days90Plus)]
    public void Ageing_buckets_use_explicit_as_of_date(string due, int expectedDays, ReceivableAgeingBucket bucket)
    {
        var dueDate = DateOnly.Parse(due);
        Assert.Equal(expectedDays, ReceivableAgeing.DaysOverdue(dueDate, AsOf));
        Assert.Equal(bucket, ReceivableAgeing.ResolveBucket(dueDate, AsOf));
    }
}

public class ReceivableBalanceTests
{
    [Fact]
    public void Invoice_only_outstanding_equals_gross()
    {
        var amounts = ReceivableBalance.Calculate(10_000m, 0m, "NotPaid");
        Assert.Equal(10_000m, amounts.OutstandingAmount);
        Assert.Equal(0m, amounts.PaidAmount);
        Assert.Equal(ReceivableCollectionStatuses.Unpaid,
            ReceivableCollectionStatuses.Resolve(amounts, new DateOnly(2026, 4, 1), new DateOnly(2026, 3, 1)));
    }

    [Fact]
    public void Invoice_with_credit_reduces_outstanding()
    {
        var amounts = ReceivableBalance.Calculate(10_000m, 2_000m, "NotPaid");
        Assert.Equal(2_000m, amounts.CreditedAmount);
        Assert.Equal(8_000m, amounts.OutstandingAmount);
        Assert.Equal(0m, amounts.PaidAmount);
    }

    [Fact]
    public void Multiple_credits_sum_in_calculator()
    {
        var amounts = ReceivableBalance.Calculate(10_000m, 2_000m + 1_500m, "NotPaid");
        Assert.Equal(3_500m, amounts.CreditedAmount);
        Assert.Equal(6_500m, amounts.OutstandingAmount);
    }

    [Fact]
    public void Legacy_paid_flag_treats_net_as_paid_without_allocations()
    {
        var amounts = ReceivableBalance.Calculate(10_000m, 2_000m, "Paid");
        Assert.Equal(8_000m, amounts.PaidAmount);
        Assert.Equal(0m, amounts.OutstandingAmount);
    }

    [Fact]
    public void Voided_invoice_excluded_at_query_layer_not_in_calculator()
    {
        var amounts = ReceivableBalance.Calculate(0m, 0m, "NotPaid");
        Assert.Equal(0m, amounts.OutstandingAmount);
    }

    [Fact]
    public void Outstanding_never_negative()
    {
        var amounts = ReceivableBalance.Calculate(100m, 150m, "NotPaid");
        Assert.Equal(0m, amounts.OutstandingAmount);
    }
}
