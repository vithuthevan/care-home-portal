namespace CareHome.Api.Common;

/// <summary>
/// Central monetary rounding. PROVISIONAL: MidpointRounding.AwayFromZero to 2 decimal places.
/// </summary>
public static class Money
{
    public const int Scale = 2;

    public static decimal Round(decimal value)
    {
        return Math.Round(value, Scale, MidpointRounding.AwayFromZero);
    }

    public static decimal Zero => 0.00m;
}
