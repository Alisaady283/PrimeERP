using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Analytics;
using PrimeERP.Platform.Localization;

namespace PrimeERP.UI.Components.Charts
{
    /// <summary>سطر تلميح: Series لنقطة اللون، وTrend لتلوين الفرق (+1 صاعد · −1 هابط)</summary>
    public sealed record InsightRow(string Label, string Value, bool Strong = false, int? Series = null, int Trend = 0);

    public sealed record Insight(string Title, IReadOnlyList<InsightRow> Rows);

    /// <summary>
    /// محتوى تلميح الرسوم — نظير frontend-web/src/lib/chartInsights.ts بالقواعد والنصوص نفسها:
    /// القيمة · الحصّة · الترتيب · الفرق عن السلسلة الأخرى أو السابق أو المتوسط. التنسيق من الرسم نفسه.
    /// </summary>
    public static class ChartInsights
    {
        /// <summary>مدّة بقاء الشريحة بعيدةً بعد النقر — كالويب</summary>
        public static readonly TimeSpan PullHold = TimeSpan.FromMilliseconds(1000);

        public static string Percent(double value) => $"{value:0.0}%";

        /// <summary>ترتيب القيمة بين قيمها، من الأكبر</summary>
        public static int RankOf(IReadOnlyList<decimal> values, decimal value) => values.OrderByDescending(v => v).ToList().IndexOf(value) + 1;

        /// <summary>الشرائح من الأكبر إلى الأصغر، إلا ترتيب الشدّة فله معناه</summary>
        public static List<int> DonutOrder(ChartData data, bool byValue)
        {
            var order = Enumerable.Range(0, data.Labels.Count).ToList();
            var values = data.Series.FirstOrDefault()?.Values ?? new List<decimal>();
            return byValue ? order.OrderByDescending(i => values.ElementAtOrDefault(i)).ToList() : order;
        }

        private static InsightRow Delta(string label, decimal diff, decimal baseline, Func<decimal, string> format) =>
            new(label, $"{(diff >= 0 ? "+" : "−")}{format(Math.Abs(diff))}{(baseline != 0 ? $" ({Percent((double)(diff / Math.Abs(baseline) * 100))})" : "")}", Trend: diff >= 0 ? 1 : -1);

        /// <summary>العمود: القيمة · الحصّة · الترتيب · الفرق عن السلسلة الأخرى</summary>
        public static Insight Column(ChartData data, int seriesIndex, int index, Func<decimal, string> format)
        {
            var s = data.Series[seriesIndex];
            var v = s.Values.ElementAtOrDefault(index);
            var total = s.Values.Sum();
            var other = data.Series.Where((_, i) => i != seriesIndex).FirstOrDefault();
            var rows = new List<InsightRow>
            {
                new(s.Name, format(v), Strong: true, Series: seriesIndex),
                new(LocalizationService.Get("Str.Chart.Share"), Percent(total == 0 ? 0 : (double)(v / total * 100))),
                new(LocalizationService.Get("Str.Chart.Rank"), $"{RankOf(s.Values, v)} / {s.Values.Count}")
            };
            if (other != null)
            {
                var otherValue = other.Values.ElementAtOrDefault(index);
                rows.Add(Delta($"{LocalizationService.Get("Str.Chart.Vs")} {other.Name}", v - otherValue, otherValue, format));
            }
            return new Insight(data.Labels.ElementAtOrDefault(index) ?? "", rows);
        }

        /// <summary>النقطة على الخطّ: كل سلسلةٍ بقيمتها والفرق عن السابق</summary>
        public static Insight Point(ChartData data, int index, Func<decimal, string> format) =>
            new(data.Labels.ElementAtOrDefault(index) ?? "", data.Series.SelectMany((s, k) =>
            {
                var v = s.Values.ElementAtOrDefault(index);
                var rows = new List<InsightRow> { new(s.Name, format(v), Strong: true, Series: k) };
                if (index > 0)
                {
                    var previous = s.Values.ElementAtOrDefault(index - 1);
                    rows.Add(Delta(LocalizationService.Get("Str.Chart.VsPrevious"), v - previous, previous, format));
                }
                return rows;
            }).ToList());

        /// <summary>الشريحة أو الصفّ: القيمة · الحصّة · الترتيب · الفرق عن المتوسط</summary>
        public static Insight Slice(string name, decimal value, IReadOnlyList<decimal> values, Func<decimal, string> format)
        {
            var total = values.Sum();
            var average = values.Count == 0 ? 0 : total / values.Count;
            return new Insight(name, new List<InsightRow>
            {
                new(LocalizationService.Get("Str.Dashboard.Value"), format(value), Strong: true),
                new(LocalizationService.Get("Str.Chart.Share"), Percent(total == 0 ? 0 : (double)(value / total * 100))),
                new(LocalizationService.Get("Str.Chart.Rank"), $"{RankOf(values, value)} / {values.Count}"),
                Delta(LocalizationService.Get("Str.Chart.VsAverage"), value - average, average, format)
            });
        }
    }
}
