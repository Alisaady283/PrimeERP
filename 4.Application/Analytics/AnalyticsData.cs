using System.Collections.Generic;

namespace PrimeERP.Application.Analytics
{
    /// <summary>مخرَج لوحة التحليل: بطاقاتٌ ورسومٌ بمفاتيحها</summary>
    public class AnalyticsData
    {
        public Dictionary<string, Kpi> Cards { get; init; } = new();
        public Dictionary<string, ChartData> Charts { get; init; } = new();
    }

    /// <summary>قيمة مؤشر وتغيّره عن فترة المقارنة</summary>
    public record Kpi(decimal Value, decimal? Change = null, string Note = null);

    /// <summary>تسميات المحور وسلاسله</summary>
    public class ChartData
    {
        public List<string> Labels { get; init; } = new();
        public List<ChartSeries> Series { get; init; } = new();
    }

    public record ChartSeries(string Name, List<decimal> Values, bool Comparison = false);
}
