namespace CareHome.Api.Common;

public static class BillingPeriodSlices
{
    public static List<(DateOnly Start, DateOnly End, DateOnly? CycleStart)> For(
        bool funderCycle,
        string? frequency,
        int? intervalDays,
        DateOnly? cycleAnchor,
        DateOnly contractStart,
        DateOnly sliceStart,
        DateOnly sliceEnd)
    {
        if (!funderCycle || BillingCycleCalendar.IsAdHoc(frequency))
        {
            return [(sliceStart, sliceEnd, null)];
        }

        var anchor = cycleAnchor ?? contractStart;
        if (anchor < contractStart)
        {
            anchor = contractStart;
        }

        var slices = new List<(DateOnly Start, DateOnly End, DateOnly? CycleStart)>();
        foreach (var cycle in BillingCycleCalendar.CyclesThrough(anchor, sliceEnd, frequency, intervalDays))
        {
            var hit = DateRanges.Intersect(sliceStart, sliceEnd, cycle.Start, cycle.End);
            if (hit is not null)
            {
                slices.Add((hit.Value.Start, hit.Value.End, cycle.Start));
            }
        }

        return slices;
    }
}
