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
            RegisterFormula("LineSubTotal", line => line.Qty * line.Price);

            RegisterFormula("DiscountAmount", line => line.Qty * line.Price * line.DiscountPercent / 100m);

            RegisterFormula("VatAmount", line =>
            {
                var subTotal = line.Qty * line.Price;
                var discount = subTotal * line.DiscountPercent / 100m;
                return (subTotal - discount) * line.VatPercent / 100m;
            });

            RegisterFormula("LineTotal", line =>
            {
                var subTotal = line.Qty * line.Price;
                var discount = subTotal * line.DiscountPercent / 100m;
                var tax = (subTotal - discount) * line.VatPercent / 100m;
                return subTotal - discount + tax;
            });

            RegisterFormula("StockTotal", line => line.Qty * line.Price);
        }

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
