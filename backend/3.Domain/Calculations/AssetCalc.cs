namespace PrimeERP.Domain.Calculations
{
    /// <summary>قواعد الإهلاك النقيّة</summary>
    public static class AssetCalc
    {
        /// <summary>قيمة التقييم وإلا التكلفة</summary>
        public static decimal Basis(decimal revalued, decimal purchase) => revalued > 0 ? revalued : purchase;

        public static decimal BookValue(decimal cost, decimal accumulated) => cost - accumulated;

        /// <summary>القيمة الحالية للأصل</summary>
        public static decimal CurrentValue(decimal revalued, decimal purchase, decimal accumulated) =>
            BookValue(Basis(revalued, purchase), accumulated);

        public static decimal BookValue(PrimeERP.Domain.Entities.AssetDisposal d) => BookValue(d.AssetValue, d.AccumulatedDepreciation);

        public static decimal GainOrLoss(PrimeERP.Domain.Entities.AssetDisposal d) => d.SalePrice - BookValue(d);

        public static decimal Difference(PrimeERP.Domain.Entities.AssetRevaluation r) => r.NewValue - r.OldValue;

        public static Movement Roll(PrimeERP.Domain.Entities.Asset asset, System.Collections.Generic.IEnumerable<PrimeERP.Domain.Entities.AssetDepreciation> charges,
            System.Collections.Generic.IEnumerable<PrimeERP.Domain.Entities.AssetRevaluation> revaluations, PrimeERP.Domain.Entities.AssetDisposal disposal,
            System.DateTime from)
        {
            var differences = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(revaluations, Difference));
            var additions = System.Linq.Enumerable.Sum(System.Linq.Enumerable.Where(differences, d => d > 0));
            var decreases = -System.Linq.Enumerable.Sum(System.Linq.Enumerable.Where(differences, d => d < 0));
            var disposed = disposal != null && disposal.DisposalDate >= from;
            var reductions = decreases + (disposed ? asset.PurchaseCost + additions - decreases : 0);

            var start = System.Linq.Enumerable.Sum(System.Linq.Enumerable.Where(charges, c => c.PeriodDate < from), c => c.Amount);
            var charge = System.Linq.Enumerable.Sum(System.Linq.Enumerable.Where(charges, c => c.PeriodDate >= from), c => c.Amount);
            var end = disposed ? 0 : start + charge;

            return new Movement(additions, reductions, asset.UsefulLifeYears > 0 ? System.Math.Round(100m / asset.UsefulLifeYears, 2) : 0,
                start, charge, end, asset.PurchaseCost + additions - reductions - end);
        }

        public static System.DateTime EndOfMonth(System.DateTime date) =>
            new(date.Year, date.Month, System.DateTime.DaysInMonth(date.Year, date.Month));

        public static System.DateTime FirstUndepreciatedMonth(System.DateTime? lastDepreciation, System.DateTime? purchase) =>
            lastDepreciation != null
                ? EndOfMonth(lastDepreciation.Value.AddMonths(1))
                : EndOfMonth(purchase ?? System.DateTime.Today);

        public readonly record struct Terms(System.DateTime From, decimal Value, decimal Salvage, int LifeMonths);

        public static System.Collections.Generic.List<Terms> TermsOf(PrimeERP.Domain.Entities.Asset asset,
            System.Collections.Generic.IEnumerable<PrimeERP.Domain.Entities.AssetRevaluation> revaluations)
        {
            var terms = new System.Collections.Generic.List<Terms>
            {
                new(EndOfMonth(asset.PurchaseDate ?? System.DateTime.Today), asset.PurchaseCost, asset.SalvageValue, asset.UsefulLifeYears * 12)
            };

            foreach (var r in System.Linq.Enumerable.OrderBy(revaluations, r => r.RevaluationDate))
            {
                var previous = System.Linq.Enumerable.Last(terms);
                var from = EndOfMonth(r.RevaluationDate);
                var value = previous.Value + Difference(r);
                terms.Add(r.UsefulLifeYears > 0
                    ? new Terms(from, value, r.SalvageValue, r.UsefulLifeYears * 12)
                    : new Terms(from, value, previous.Salvage, System.Math.Max(0, previous.LifeMonths - MonthsBetween(previous.From, from))));
            }

            return terms;
        }

        public static decimal Monthly(System.Collections.Generic.IReadOnlyList<Terms> terms, decimal accumulated, System.DateTime period)
        {
            var month = EndOfMonth(period);
            var current = System.Linq.Enumerable.Last(System.Linq.Enumerable.DefaultIfEmpty(
                System.Linq.Enumerable.Where(terms, t => t.From <= month), System.Linq.Enumerable.First(terms)));

            var depreciable = current.Value - current.Salvage - accumulated;
            if (depreciable <= 0 || current.LifeMonths <= 0) return 0;

            var left = current.LifeMonths - MonthsBetween(current.From, month);
            return left <= 1 ? depreciable : depreciable / left;
        }

        public static System.Collections.Generic.IEnumerable<(System.DateTime Period, decimal Amount)> Schedule(
            System.Collections.Generic.IReadOnlyList<Terms> terms, decimal accumulated, System.DateTime firstMonth, System.DateTime upTo)
        {
            var running = accumulated;
            for (var period = EndOfMonth(firstMonth); period <= EndOfMonth(upTo); period = EndOfMonth(period.AddMonths(1)))
            {
                var amount = Monthly(terms, running, period);
                if (amount <= 0) continue;

                running += amount;
                yield return (period, amount);
            }
        }

        private static int MonthsBetween(System.DateTime from, System.DateTime to) =>
            (to.Year - from.Year) * 12 + to.Month - from.Month;
    }

    public readonly record struct Movement(decimal Additions, decimal Reductions, decimal Rate,
        decimal AccumulatedStart, decimal Charge, decimal AccumulatedEnd, decimal Net);
}
