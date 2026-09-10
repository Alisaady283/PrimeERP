using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Services.Builder
{
    /// <summary>
    /// نِسَب عرض أعمدة الجدول. نسبةٌ واحدة مُدخَلة تجعل الجدول كلَّه نسبيّاً، فيبقى تناسبه واحداً على
    /// أي عرض شاشة وفي أي نسخة. والعمود بلا نسبة يأخذ حصّته من عرض جدوله فلا ينهار، والمجموع يُطبَّع
    /// على مئة مهما أُدخِل. موضعٌ واحد تقرؤه شاشة الأعمدة والمُحمِّل — فالمعروض هو المطبَّق.
    /// </summary>
    public static class ColumnWidths
    {
        /// <summary>هل للجدول نسبٌ أصلاً — واحدةٌ تكفي.</summary>
        public static bool Proportional(IEnumerable<BuilderColumn> columns) =>
            columns.Any(column => column.WidthPercent > 0);

        /// <summary>حصّة كل عمود من مئة، بمعرّفه.</summary>
        public static Dictionary<int, double> Shares(IReadOnlyList<BuilderColumn> columns)
        {
            var pixels = columns.Sum(column => column.Width);

            double Declared(BuilderColumn column) =>
                column.WidthPercent > 0 ? column.WidthPercent
                : pixels > 0            ? column.Width / pixels * 100
                                        : 0;

            var total = columns.Sum(Declared);

            return columns.ToDictionary(column => column.Id,
                column => total > 0 ? Declared(column) / total * 100 : 0);
        }
    }
}
