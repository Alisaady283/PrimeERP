namespace PrimeERP.Domain.Rules
{
    /// <summary>قواعد الإهلاك النقيّة</summary>
    public static class DepreciationRules
    {
        public static decimal PerMonth(decimal cost, decimal salvage, int usefulLifeYears) =>
            usefulLifeYears <= 0 ? 0 : (cost - salvage) / (usefulLifeYears * 12);

        public static decimal BookValue(decimal cost, decimal accumulated) => cost - accumulated;

        public static decimal Depreciable(decimal cost, decimal salvage, decimal accumulated) =>
            cost - salvage - accumulated;

        public static System.DateTime EndOfMonth(System.DateTime date) =>
            new(date.Year, date.Month, System.DateTime.DaysInMonth(date.Year, date.Month));

        public static System.DateTime FirstUndepreciatedMonth(System.DateTime? lastDepreciation, System.DateTime? purchase) =>
            lastDepreciation != null
                ? EndOfMonth(lastDepreciation.Value.AddMonths(1))
                : EndOfMonth(purchase ?? System.DateTime.Today);

        public static System.Collections.Generic.IEnumerable<(System.DateTime Period, decimal Amount)> Schedule(
            decimal cost, decimal salvage, int usefulLifeYears, decimal accumulated,
            System.DateTime firstMonth, System.DateTime upTo)
        {
            var monthly = PerMonth(cost, salvage, usefulLifeYears);
            if (monthly <= 0) yield break;

            var running = accumulated;
            var period = EndOfMonth(firstMonth);
            var last = EndOfMonth(upTo);

            while (period <= last)
            {
                var amount = System.Math.Min(monthly, Depreciable(cost, salvage, running));
                if (amount <= 0) yield break;

                running += amount;
                yield return (period, amount);

                period = EndOfMonth(period.AddMonths(1));
            }
        }
    }
}
