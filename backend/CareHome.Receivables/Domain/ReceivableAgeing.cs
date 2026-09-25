namespace CareHome.Api.Receivables.Domain;

public enum ReceivableAgeingBucket
{
    Current,
    Days1To30,
    Days31To60,
    Days61To90,
    Days90Plus
}

/// <summary>
/// Deterministic ageing from invoice due date and an explicit as-of date.
/// </summary>
public static class ReceivableAgeing
{
    public static int DaysOverdue(DateOnly dueDate, DateOnly asOfDate)
    {
        if (dueDate >= asOfDate)
        {
            return 0;
        }

        return asOfDate.DayNumber - dueDate.DayNumber;
    }

    public static ReceivableAgeingBucket ResolveBucket(DateOnly dueDate, DateOnly asOfDate)
    {
        var overdueDays = DaysOverdue(dueDate, asOfDate);
        if (overdueDays <= 0)
        {
            return ReceivableAgeingBucket.Current;
        }

        if (overdueDays <= 30)
        {
            return ReceivableAgeingBucket.Days1To30;
        }

        if (overdueDays <= 60)
        {
            return ReceivableAgeingBucket.Days31To60;
        }

        if (overdueDays <= 90)
        {
            return ReceivableAgeingBucket.Days61To90;
        }

        return ReceivableAgeingBucket.Days90Plus;
    }
}
