using CareHome.Api.Funding;
using Xunit;

namespace CareHome.Api.Tests;

public class FundingContractOverlapTests
{
    [Fact]
    public void Adjacent_periods_do_not_overlap()
    {
        var janToMar = (Start: new DateOnly(2026, 1, 1), End: (DateOnly?)new DateOnly(2026, 3, 31));
        var aprOpen = (Start: new DateOnly(2026, 4, 1), End: (DateOnly?)null);

        Assert.False(FundingContractOverlap.PeriodsOverlap(janToMar.Start, janToMar.End, aprOpen.Start, aprOpen.End));
    }

    [Fact]
    public void Interior_overlap_is_rejected()
    {
        var janToMar = (Start: new DateOnly(2026, 1, 1), End: (DateOnly?)new DateOnly(2026, 3, 31));
        var midMarToApr = (Start: new DateOnly(2026, 3, 15), End: (DateOnly?)new DateOnly(2026, 4, 15));

        Assert.True(FundingContractOverlap.PeriodsOverlap(janToMar.Start, janToMar.End, midMarToApr.Start, midMarToApr.End));
    }

    [Fact]
    public void Two_open_ended_periods_overlap()
    {
        var janOpen = (Start: new DateOnly(2026, 1, 1), End: (DateOnly?)null);
        var aprOpen = (Start: new DateOnly(2026, 4, 1), End: (DateOnly?)null);

        Assert.True(FundingContractOverlap.PeriodsOverlap(janOpen.Start, janOpen.End, aprOpen.Start, aprOpen.End));
    }

    [Fact]
    public void Inclusive_same_day_touch_inside_range_overlaps()
    {
        var first = (Start: new DateOnly(2026, 1, 1), End: (DateOnly?)new DateOnly(2026, 12, 31));
        var second = (Start: new DateOnly(2026, 6, 1), End: (DateOnly?)null);

        Assert.True(FundingContractOverlap.PeriodsOverlap(first.Start, first.End, second.Start, second.End));
    }

    [Fact]
    public void Billing_user_message_uses_business_context_not_contract_ids()
    {
        var message = FundingContractOverlap.BillingUserMessage(
            "Jane Smith",
            "Hampshire County Council",
            "General care",
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31));

        Assert.Contains("Jane Smith", message);
        Assert.Contains("Hampshire County Council", message);
        Assert.Contains("General care", message);
        Assert.Contains("1 Aug 2026", message);
        Assert.DoesNotContain("Contract IDs", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("42", message);
    }

    [Fact]
    public void Conflict_message_does_not_name_internal_client_ids()
    {
        Assert.Contains("resident", FundingContractOverlap.ConflictMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("client", FundingContractOverlap.ConflictMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Id", FundingContractOverlap.ConflictMessage, StringComparison.Ordinal);
    }
}
