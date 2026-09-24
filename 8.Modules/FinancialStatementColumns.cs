using System.Collections.Generic;
using PrimeERP.Application.Reporting;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.Modules
{
    /// <summary>أعمدة القوائم المالية</summary>
    public static class FinancialStatementColumns
    {
        public static List<GridColumn> Build() => new()
        {
            new() { Header = "البيان", Binding = nameof(FinancialStatementFactory.Line.Statement), Width = 320, IsStarWidth = true },
            new() { Header = "جزئي",   Binding = nameof(FinancialStatementFactory.Line.Partial), Width = 150, Align = ColumnAlign.Center, Format = "N2" },
            new() { Header = "كلي",    Binding = nameof(FinancialStatementFactory.Line.Total),   Width = 150, Align = ColumnAlign.Center, Format = "N2" },
        };
    }
}
