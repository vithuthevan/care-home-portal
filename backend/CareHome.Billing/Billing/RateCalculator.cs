namespace CareHome.Api.Billing
{
    /// <summary>
    /// PROVISIONAL BUSINESS ASSUMPTIONS live here so they can be changed in one place.
    /// See docs/BILLING_ENGINE.md and docs/OPEN_BUSINESS_DECISIONS.md.
    /// </summary>
    public class RateCalculator
    {
        public decimal Calculate(string frequency, decimal rateAmount, DateOnly periodStart, DateOnly periodEnd)
        {
            var days = Common.DateRanges.InclusiveDays(periodStart, periodEnd);
            if (days <= 0)
            {
                return Common.Money.Zero;
            }

            return frequency switch
            {
                RateFrequencies.Daily => Common.Money.Round(rateAmount * days),
                RateFrequencies.Weekly => Common.Money.Round((rateAmount / 7m) * days),
                RateFrequencies.Monthly => CalculateMonthly(rateAmount, periodStart, periodEnd),
                _ => throw new InvalidOperationException($"Unsupported rate frequency '{frequency}'.")
            };
        }

        private static decimal CalculateMonthly(decimal monthlyAmount, DateOnly periodStart, DateOnly periodEnd)
        {
            decimal total = 0m;
            var cursor = new DateOnly(periodStart.Year, periodStart.Month, 1);

            while (cursor <= periodEnd)
            {
                var monthStart = cursor;
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var sliceStart = periodStart > monthStart ? periodStart : monthStart;
                var sliceEnd = periodEnd < monthEnd ? periodEnd : monthEnd;

                if (sliceEnd >= sliceStart)
                {
                    var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
                    var eligible = Common.DateRanges.InclusiveDays(sliceStart, sliceEnd);
                    total += (monthlyAmount / daysInMonth) * eligible;
                }

                cursor = monthStart.AddMonths(1);
            }

            return Common.Money.Round(total);
        }
    }
}

