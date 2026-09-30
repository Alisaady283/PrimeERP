using PrimeERP.Domain.Calculations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.UI.Components.Documents
{
    /// <summary>محرك حساب أعمدة السطر</summary>
    public static class LineComputeEngine
    {
        private static readonly Dictionary<string, Func<DocumentLine, decimal>> _formulas = new();

        static LineComputeEngine()
        {
            RegisterFormula("LineSubTotal", line => Amounts(line).Gross);
            RegisterFormula("DiscountAmount", line => Amounts(line).Discount);
            RegisterFormula("VatAmount", line => Amounts(line).Vat);
            RegisterFormula("LineTotal", line => Amounts(line).Net);
            RegisterFormula("StockTotal", line => Amounts(line).Gross);
        }

        private static LineAmounts Amounts(DocumentLine line) =>
            LineCalc.ForLine(line.Qty, line.Price, line.DiscountPercent, line.VatPercent, 0);

        public static void RegisterFormula(string key, Func<DocumentLine, decimal> formula) => _formulas[key] = formula;

        public static bool HasFormula(string key) => _formulas.ContainsKey(key);

        public static decimal Compute(string key, DocumentLine line) =>
            !string.IsNullOrEmpty(key) && _formulas.TryGetValue(key, out var formula) ? formula(line) : 0m;

        public static void Recalculate(DocumentLine line, List<LineColumn> columns)
        {
            foreach (var col in columns.Where(c => c.Type == LineColumnType.Computed && !string.IsNullOrEmpty(c.ComputeExpression)))
                line[col.Key] = Compute(col.ComputeExpression, line);
        }
    }
}
