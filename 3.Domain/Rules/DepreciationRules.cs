namespace PrimeERP.Domain.Rules
{
    /// <summary>
    /// قواعد الإهلاك النقيّة — بلا قاعدة بيانات ولا قيود. تستوردها خدمةُ الاحتساب التي تُنتج القيد،
    /// وتقريرُ الأصول الذي يعرض القسط والقيمة المتبقية، فالرقمان واحدٌ لا حسابان قد يفترقان.
    /// </summary>
    public static class DepreciationRules
    {
        /// <summary>قسط القسط الثابت شهرياً: (التكلفة − المتبقية) ÷ (العمر × ١٢). عمرٌ صفر = أصلٌ لا يُهلَك.</summary>
        public static decimal PerMonth(decimal cost, decimal salvage, int usefulLifeYears) =>
            usefulLifeYears <= 0 ? 0 : (cost - salvage) / (usefulLifeYears * 12);

        /// <summary>القيمة الدفترية: التكلفة ناقص ما أُهلك فعلاً.</summary>
        public static decimal BookValue(decimal cost, decimal accumulated) => cost - accumulated;

        /// <summary>ما بقي قابلاً للإهلاك — لا يُهلَك الأصل تحت قيمته المتبقية.</summary>
        public static decimal Depreciable(decimal cost, decimal salvage, decimal accumulated) =>
            cost - salvage - accumulated;

        /// <summary>آخر يوم في شهر التاريخ — الإهلاك يُقيَّد في نهاية شهره لا في يوم الشراء.</summary>
        public static System.DateTime EndOfMonth(System.DateTime date) =>
            new(date.Year, date.Month, System.DateTime.DaysInMonth(date.Year, date.Month));

        /// <summary>
        /// أول شهرٍ لم يُهلَك بعد: شهر الشراء نفسه إن لم يسبقه إهلاك (شهر الاقتناء يُحتسَب كاملاً)،
        /// وإلا الشهر التالي لآخر إهلاك — فلا يُحتسَب شهرٌ مرّتين ولا يُتخطّى شهر.
        /// </summary>
        public static System.DateTime FirstUndepreciatedMonth(System.DateTime? lastDepreciation, System.DateTime? purchase) =>
            lastDepreciation != null
                ? EndOfMonth(lastDepreciation.Value.AddMonths(1))
                : EndOfMonth(purchase ?? System.DateTime.Today);

        /// <summary>
        /// جدول الاستحقاق شهراً شهراً: قسطٌ لكل شهرٍ من أول شهرٍ لم يُهلَك حتى شهر التاريخ، بتاريخ آخر
        /// يومٍ من كل شهر — فيظهر الإهلاك في كشف الحساب وقائمة الدخل في شهره هو. وآخر قسطٍ يُقصّ عند ما
        /// بقي قابلاً للإهلاك، فلا يتجاوز الأصلُ عمرَه ولا ينزل تحت قيمة خردته.
        /// </summary>
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
