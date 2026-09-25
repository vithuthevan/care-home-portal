using CareHome.Api.Common;
using Xunit;

namespace CareHome.Api.Tests;

public class BillingCycleCalendarTests
{
    [Fact]
    public void Weekly_cycles_do_not_overlap()
    {
        var anchor = new DateOnly(2026, 7, 13);
        var cycles = BillingCycleCalendar.CyclesThrough(anchor, new DateOnly(2026, 8, 9), "Weekly", null);
        Assert.Equal(4, cycles.Count);
        for (var i = 1; i < cycles.Count; i++)
        {
            Assert.True(cycles[i].Start > cycles[i - 1].End);
        }
    }

    [Fact]
    public void Funder_cycle_slices_respect_contract_start()
    {
        var slices = BillingPeriodSlices.For(
            funderCycle: true,
            frequency: "Weekly",
            intervalDays: null,
            cycleAnchor: new DateOnly(2026, 7, 13),
            contractStart: new DateOnly(2026, 7, 20),
            sliceStart: new DateOnly(2026, 7, 13),
            sliceEnd: new DateOnly(2026, 8, 9));

        Assert.NotEmpty(slices);
        Assert.All(slices, s => Assert.True(s.Start >= new DateOnly(2026, 7, 20)));
    }

    [Fact]
    public void Manual_mode_returns_single_slice()
    {
        var slices = BillingPeriodSlices.For(
            funderCycle: false,
            frequency: "Weekly",
            intervalDays: null,
            cycleAnchor: new DateOnly(2026, 7, 13),
            contractStart: new DateOnly(2026, 1, 1),
            sliceStart: new DateOnly(2026, 5, 1),
            sliceEnd: new DateOnly(2026, 5, 7));

        Assert.Single(slices);
        Assert.Equal(new DateOnly(2026, 5, 1), slices[0].Start);
        Assert.Equal(new DateOnly(2026, 5, 7), slices[0].End);
    }
}
