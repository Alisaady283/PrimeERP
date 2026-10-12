using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Domain.Calculations
{
    /// <summary>مؤشرات التحليل المسمّاة</summary>
    public static class AnalyticsCalc
    {
        public static decimal? Growth(decimal current, decimal previous) =>
            previous == 0 ? null : Math.Round((current - previous) / Math.Abs(previous) * 100m, 1);

        public static decimal Share(decimal part, decimal whole) =>
            whole == 0 ? 0 : Math.Round(part / whole * 100m, 1);

        /// <summary>أيام الرصيد من تدفّق الفترة: DSO للعملاء، DPO للموردين، DIO للمخزون</summary>
        public static decimal DaysOf(decimal balance, decimal periodFlow, int periodDays) =>
            periodFlow <= 0 || periodDays <= 0 ? 0 : Math.Round(balance / (periodFlow / periodDays), 1);

        /// <summary>دورة التحويل النقدي</summary>
        public static decimal CashCycle(decimal dso, decimal dio, decimal dpo) => dso + dio - dpo;

        public static AgingBucket Aging(DateTime? dueDate, DateTime documentDate, DateTime asOf)
        {
            var overdue = (asOf.Date - (dueDate ?? documentDate).Date).Days;
            return overdue <= 0 ? AgingBucket.NotDue
                : overdue <= 30 ? AgingBucket.Days30
                : overdue <= 60 ? AgingBucket.Days60
                : overdue <= 90 ? AgingBucket.Days90
                : overdue <= 120 ? AgingBucket.Days120
                : overdue <= 150 ? AgingBucket.Days150
                : AgingBucket.Over150;
        }

        /// <summary>تصنيف ABC: أعلى القيم حتى ٨٠٪ من المجموع A، وحتى ٩٥٪ B، والباقي C</summary>
        public static Dictionary<TKey, AbcClass> Abc<TKey>(IEnumerable<(TKey Key, decimal Value)> items)
        {
            var ordered = items.Where(i => i.Value > 0).OrderByDescending(i => i.Value).ToList();
            var total = ordered.Sum(i => i.Value);
            var running = 0m;
            var result = new Dictionary<TKey, AbcClass>();
            foreach (var (key, value) in ordered)
            {
                var before = Share(running, total);
                running += value;
                result[key] = before < 80 ? AbcClass.A : before < 95 ? AbcClass.B : AbcClass.C;
            }
            return result;
        }

        /// <summary>دوران المخزون: تكلفة المنصرف على قيمة الرصيد</summary>
        public static decimal Turnover(decimal issuedCost, decimal stockValue) =>
            stockValue <= 0 ? 0 : Math.Round(issuedCost / stockValue, 2);

        public static MovementSpeed Speed(decimal turnover) =>
            turnover <= 0 ? MovementSpeed.Dead : turnover < 1 ? MovementSpeed.Slow : turnover < 4 ? MovementSpeed.Normal : MovementSpeed.Fast;

        /// <summary>الصنف راكدٌ إن لم يُصرف منذ حدّ الأيام وله رصيد</summary>
        public static bool IsDead(decimal stockQty, DateTime? lastIssue, DateTime asOf, int days = 90) =>
            stockQty > 0 && (lastIssue == null || (asOf.Date - lastIssue.Value.Date).Days >= days);

        /// <summary>الطرف نشطٌ إن تعامل خلال ٣٠ يوماً، ويفتر حتى ٩٠، ثم متوقّف</summary>
        public static PartyState Recency(DateTime lastDealing, DateTime asOf, int activeDays = 30, int lostDays = 90)
        {
            var days = (asOf.Date - lastDealing.Date).Days;
            return days <= activeDays ? PartyState.Active : days <= lostDays ? PartyState.Cooling : PartyState.Lost;
        }

        /// <summary>تاريخ احتساب الطرف متوقّفاً</summary>
        public static DateTime LostOn(DateTime lastDealing, int lostDays = 90) => lastDealing.Date.AddDays(lostDays + 1);

        /// <summary>الفترة السابقة بالطول نفسه</summary>
        public static (DateTime From, DateTime To) PreviousPeriod(DateTime from, DateTime to)
        {
            var days = (to.Date - from.Date).Days + 1;
            return (from.Date.AddDays(-days), from.Date.AddDays(-1));
        }

        public static (DateTime From, DateTime To) LastYear(DateTime from, DateTime to) => (from.Date.AddYears(-1), to.Date.AddYears(-1));

        public static List<(int Year, int Month)> Months(DateTime from, DateTime to)
        {
            var months = new List<(int, int)>();
            for (var m = new DateTime(from.Year, from.Month, 1); m <= to; m = m.AddMonths(1)) months.Add((m.Year, m.Month));
            return months;
        }

        public static int Days(DateTime from, DateTime to) => Math.Max((to.Date - from.Date).Days + 1, 1);
    }
}
