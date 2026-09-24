using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Services.Builder
{
    /// <summary>نِسَب عرض أعمدة الجدول</summary>
    public static class ColumnWidths
    {
        public static bool Proportional(IEnumerable<BuilderColumn> columns) =>
            columns.Any(column => column.WidthPercent > 0);

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
