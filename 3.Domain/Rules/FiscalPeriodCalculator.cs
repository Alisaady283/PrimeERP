using System;
using System.Collections.Generic;

namespace PrimeERP.Domain.Rules
{
    /// <summary>
    /// حسابات تواريخ السنة/الفترات المالية — دوال نقية بلا وصول لقاعدة بيانات أو إعدادات أو خدمة، بنفس روح
    /// AccountingRules. يستخدمها FiscalPeriodService.CreateYear (منطق الأعمال، خلف تحقق صلاحية) و
    /// Database/FiscalYearSeeder (يعمل عند إقلاع التطبيق قبل أي تسجيل دخول) معاً — كانت هذه الحسابات مكرَّرة
    /// حرفياً في الاثنين قبل استخلاصها هنا.
    /// </summary>
    public static class FiscalPeriodCalculator
    {
        public static DateTime EndOfYear(DateTime start) => start.AddYears(1).AddDays(-1);

        /// <summary>يقسّم [start, end] إلى periodsCount فترة متساوية بالأشهر (12/periodsCount شهراً لكل فترة) — يتعامل مع فروق أطوال الشهور تلقائياً لأنه يبني كل حد عبر AddMonths على start لا بعدّ أيام ثابت.</summary>
        public static List<(int PeriodNo, DateTime Start, DateTime End)> SplitPeriods(DateTime start, DateTime end, int periodsCount)
        {
            int monthsPerPeriod = 12 / periodsCount;
            var result = new List<(int, DateTime, DateTime)>();

            for (int i = 0; i < periodsCount; i++)
            {
                var periodStart = start.AddMonths(i * monthsPerPeriod);
                var periodEnd = i == periodsCount - 1 ? end : start.AddMonths((i + 1) * monthsPerPeriod).AddDays(-1);
                result.Add((i + 1, periodStart, periodEnd));
            }

            return result;
        }

        public static string DefaultYearName(DateTime start, DateTime end) =>
            start.Year == end.Year ? start.Year.ToString() : $"{start.Year}/{end.Year}";

        /// <summary>أول يوم من السنة المالية التي تحوي referenceDate، بافتراض أن السنة تبدأ شهر startMonth (1-12) من كل عام.</summary>
        public static DateTime YearStartContaining(DateTime referenceDate, int startMonth)
        {
            var yearNum = referenceDate.Month >= startMonth ? referenceDate.Year : referenceDate.Year - 1;
            return new DateTime(yearNum, startMonth, 1);
        }
    }
}
