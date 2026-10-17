using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Domain.Calculations
{
    /// <summary>مقاييس العرض والطباعة</summary>
    public static class LayoutCalc
    {
        public static bool Proportional(IEnumerable<BuilderColumn> columns) =>
            columns.Any(column => column.WidthPercent > 0);

        /// <summary>نسبة كل عمودٍ من العرض</summary>
        public static Dictionary<int, double> Shares(IReadOnlyList<BuilderColumn> columns, int digits = 15)
        {
            var pixels = columns.Sum(column => column.Width);

            double Declared(BuilderColumn column) =>
                column.WidthPercent > 0 ? column.WidthPercent
                : pixels > 0            ? column.Width / pixels * 100
                                        : 0;

            var total = columns.Sum(Declared);

            return columns.ToDictionary(column => column.Id,
                column => Math.Round(total > 0 ? Declared(column) / total * 100 : 0, digits));
        }
    }
}
