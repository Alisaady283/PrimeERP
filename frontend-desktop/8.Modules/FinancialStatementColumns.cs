using System.Collections.Generic;
using PrimeERP.Application.Reporting;
using PrimeERP.UI.Components.Display;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Modules
{
    /// <summary>أعمدة القوائم المالية</summary>
    public static class FinancialStatementColumns
    {
        public static List<GridColumn> Build() => new()
        {
            new() { Header = LocalizationService.Get("Str.Description"), Binding = nameof(FinancialStatementFactory.Line.Statement), Width = 320, IsStarWidth = true },
            new() { Header = LocalizationService.Get("Str.Reports.Partial"), Binding = nameof(FinancialStatementFactory.Line.Partial), Width = 150, Align = ColumnAlign.Center, Format = "N2" },
            new() { Header = LocalizationService.Get("Str.Reports.Whole"), Binding = nameof(FinancialStatementFactory.Line.Total),   Width = 150, Align = ColumnAlign.Center, Format = "N2" },
        };
    }
}
