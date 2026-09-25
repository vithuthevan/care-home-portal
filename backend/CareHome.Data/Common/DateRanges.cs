namespace CareHome.Api.Common;

public static class DateRanges
{
    public static DateOnly OpenEnded => DateOnly.MaxValue;

    public static bool Overlaps(
        DateOnly startA,
        DateOnly? endA,
        DateOnly startB,
        DateOnly? endB)
    {
        var aEnd = endA ?? OpenEnded;
        var bEnd = endB ?? OpenEnded;
        return startA <= bEnd && startB <= aEnd;
    }

    public static (DateOnly Start, DateOnly End)? Intersect(
        DateOnly startA,
        DateOnly? endA,
        DateOnly startB,
        DateOnly? endB)
    {
        var start = startA > startB ? startA : startB;
        var aEnd = endA ?? OpenEnded;
        var bEnd = endB ?? OpenEnded;
        var end = aEnd < bEnd ? aEnd : bEnd;

        if (end < start)
        {
            return null;
        }

        return (start, end);
    }

    public static int InclusiveDays(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            return 0;
        }

        return end.DayNumber - start.DayNumber + 1;
    }

    /// <summary>
    /// Subtracts billed ranges from an eligible inclusive range.
    /// Remaining fragments are returned as separate periods (partial-period billing).
    /// </summary>
    public static List<(DateOnly Start, DateOnly End)> Subtract(
        DateOnly start,
        DateOnly end,
        IEnumerable<(DateOnly Start, DateOnly End)> exclusions)
    {
        var remaining = new List<(DateOnly Start, DateOnly End)> { (start, end) };

        foreach (var exclusion in exclusions.OrderBy(x => x.Start))
        {
            var next = new List<(DateOnly Start, DateOnly End)>();

            foreach (var range in remaining)
            {
                if (!Overlaps(range.Start, range.End, exclusion.Start, exclusion.End))
                {
                    next.Add(range);
                    continue;
                }

                if (exclusion.Start > range.Start)
                {
                    var leftEnd = exclusion.Start.AddDays(-1);
                    if (leftEnd >= range.Start)
                    {
                        next.Add((range.Start, leftEnd));
                    }
                }

                if (exclusion.End < range.End)
                {
                    var rightStart = exclusion.End.AddDays(1);
                    if (rightStart <= range.End)
                    {
                        next.Add((rightStart, range.End));
                    }
                }
            }

            remaining = next;
        }

        return remaining;
    }
}
