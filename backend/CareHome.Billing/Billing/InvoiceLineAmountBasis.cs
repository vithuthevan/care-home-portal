namespace CareHome.Billing.Billing;

/// <summary>Read-only explanation text stored on invoice lines at generation time.</summary>
public static class InvoiceLineAmountBasis
{
    public static string Format(int eligibleDays, string rateFrequency, decimal rateAmount, bool isMiscCharge)
    {
        if (isMiscCharge || string.Equals(rateFrequency.Trim(), "AdHoc", StringComparison.OrdinalIgnoreCase))
        {
            return eligibleDays == 1
                ? "Fixed miscellaneous charge (1 day) as entered at billing."
                : $"Fixed charge for {eligibleDays} day(s) as entered at billing.";
        }

        var freq = string.IsNullOrWhiteSpace(rateFrequency) ? "rate" : rateFrequency.Trim();
        return $"{eligibleDays} eligible day(s) at {rateAmount:0.00} {freq} (pro-rated by billing).";
    }
}
