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

    /// <summary>تسميات المحور وسلاسله؛ وKeys معرّف كل تسمية وDimension ما تُصفّى به اللوحة عند النقر عليها (فارغان لما لا يُصفّي)</summary>
    public class ChartData
    {
        public List<string> Labels { get; init; } = new();
        public List<ChartSeries> Series { get; init; } = new();
        public List<string> Keys { get; set; }
        public string Dimension { get; set; }
    }

    /// <summary>
    /// تصفية اللوحة كـBI: Dimension ما نُقر عليه (Month · Party · Product · Category) وKey معرّفه (yyyy-MM أو المعرّف).
    /// الشهر يضيّق الفترة في كل لوحة، والطرف والصنف والتصنيف تُعاد بها لوحتا البيع والشراء كلها.
    /// </summary>
    public record AnalyticsFilter(string Dimension, string Key)
    {
        public const string Month = "Month", Party = "Party", Product = "Product", Category = "Category",
                            Account = "Account", Warehouse = "Warehouse", Treasury = "Treasury", Department = "Department";
    }

    public record ChartSeries(string Name, List<decimal> Values, bool Comparison = false);
}
