namespace CareHome.Api.Common;

public static class BillingCycleCalendar
{
    public static bool IsAdHoc(string? frequency) =>
        string.Equals(frequency, "AdHoc", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<(DateOnly Start, DateOnly End)> CyclesThrough(
        DateOnly anchor,
        DateOnly through,
        string? frequency,
        int? intervalDays)
    {
        if (through < anchor || IsAdHoc(frequency))
        {
            return [];
        }

        if (string.Equals(frequency, "Monthly", StringComparison.OrdinalIgnoreCase))
        {
            return MonthlyCycles(anchor, through);
        }

        var length = string.Equals(frequency, "Weekly", StringComparison.OrdinalIgnoreCase)
            ? 7
            : string.Equals(frequency, "Daily", StringComparison.OrdinalIgnoreCase)
                ? 1
                : intervalDays.GetValueOrDefault();

        if (length < 1)
        {
            return [];
        }

        return FixedCycles(anchor, through, length);
    }

    /// <summary>
    /// Start of the upcoming cycle: the cycle that begins on or after <paramref name="asOf"/>.
    /// When <paramref name="asOf"/> falls inside a cycle, this is the following cycle.
    /// </summary>
    public static DateOnly? NextCycleStart(
        DateOnly anchor,
        DateOnly asOf,
        string? frequency,
        int? intervalDays)
    {
        if (IsAdHoc(frequency) || asOf < anchor)
        {
            return IsAdHoc(frequency) ? null : anchor;
        }

        var horizon = asOf.AddYears(2);
        foreach (var cycle in CyclesThrough(anchor, horizon, frequency, intervalDays))
        {
            if (cycle.Start >= asOf)
            {
                return cycle.Start;
            }
        }

        return null;
    }

    public static (DateOnly Start, DateOnly End)? CycleContaining(
        DateOnly anchor,
        DateOnly date,
        string? frequency,
        int? intervalDays)
    {
        foreach (var cycle in CyclesThrough(anchor, date, frequency, intervalDays))
        {
            if (cycle.Start <= date && cycle.End >= date)
            {
                return cycle;
            }
        }

        return null;
    }

    private static List<(DateOnly Start, DateOnly End)> FixedCycles(
        DateOnly anchor,
        DateOnly through,
        int length)
    {
        var cycles = new List<(DateOnly Start, DateOnly End)>();
        var start = anchor;
        for (var i = 0; i < 520 && start <= through; i++)
        {
            var end = start.AddDays(length - 1);
            cycles.Add((start, end));
            start = end.AddDays(1);
        }

        return cycles;
    }

    private static List<(DateOnly Start, DateOnly End)> MonthlyCycles(DateOnly anchor, DateOnly through)
    {
        var cycles = new List<(DateOnly Start, DateOnly End)>();
        var start = anchor;
        for (var i = 0; i < 240 && start <= through; i++)
        {
            var next = start.AddMonths(1);
            var end = next.AddDays(-1);
            cycles.Add((start, end));
            start = next;
        }

        return cycles;
    }
}
