using CareHome.Api.Payments.Domain;
using CareHome.Api.Receivables.Domain;
using Xunit;

namespace CareHome.Api.Tests;

public class PaymentAllocationCapacityTests
{
    [Fact]
    public void Partial_allocation_reduces_remaining_collectible()
    {
        var remaining = InvoiceAllocationCapacity.RemainingCollectible(10_000m, 0m, "NotPaid", 4_000m);
        Assert.Equal(6_000m, remaining);
    }

    [Fact]
    public void Legacy_paid_blocks_allocation_when_no_real_allocations()
    {
        var remaining = InvoiceAllocationCapacity.RemainingCollectible(10_000m, 2_000m, "Paid", 0m);
        Assert.Equal(0m, remaining);
    }

    [Fact]
    public void Real_allocations_ignore_legacy_paid_double_count()
    {
        var amounts = ReceivableBalance.Calculate(10_000m, 0m, "Paid", 1_000m);
        Assert.Equal(1_000m, amounts.PaidAmount);
        Assert.Equal(9_000m, amounts.OutstandingAmount);
        Assert.False(LegacyInvoicePaymentCompatibility.Applies("Paid", 1_000m));
    }
}

public class ReceivablePartialPaymentTests
{
    [Fact]
    public void Partial_payment_yields_partially_paid_collection_status()
    {
        var amounts = ReceivableBalance.Calculate(10_000m, 0m, "NotPaid", 4_000m);
        var status = ReceivableCollectionStatuses.Resolve(
            amounts,
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 3, 1));
        Assert.Equal(ReceivableCollectionStatuses.PartiallyPaid, status);
        Assert.Equal(6_000m, amounts.OutstandingAmount);
    }
}
