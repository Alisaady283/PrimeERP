using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Views.Controls.Documents
{
    /// <summary>
    /// محرك حساب أعمدة السطر — دوال C# مسجّلة بمفتاح ثابت، عمداً بلا أي تقييم نصوص ديناميكي (Eval)
    /// لأنه خطر أمنياً وبطيء. كل صيغة تُحسب من القيم الخام مباشرة فلا تعتمد على ترتيب التنفيذ.
    /// </summary>
    public static class LineComputeEngine
    {
        private static readonly Dictionary<string, Func<DocumentLine, decimal>> _formulas = new();

        static LineComputeEngine()
        {
            RegisterFormula("LineSubTotal", line => line.Qty * line.Price);

            RegisterFormula("DiscountAmount", line => line.Qty * line.Price * line.DiscountPercent / 100m);

            RegisterFormula("TaxAmount", line =>
            {
                var subTotal = line.Qty * line.Price;
                var discount = subTotal * line.DiscountPercent / 100m;
                return (subTotal - discount) * line.TaxPercent / 100m;
            });

            RegisterFormula("LineTotal", line =>
            {
                var subTotal = line.Qty * line.Price;
                var discount = subTotal * line.DiscountPercent / 100m;
                var tax = (subTotal - discount) * line.TaxPercent / 100m;
                return subTotal - discount + tax;
            });

            RegisterFormula("StockTotal", line => line.Qty * line.Price);
        }

        public static void RegisterFormula(string key, Func<DocumentLine, decimal> formula) => _formulas[key] = formula;

        public static bool HasFormula(string key) => _formulas.ContainsKey(key);

        public static decimal Compute(string key, DocumentLine line) =>
            !string.IsNullOrEmpty(key) && _formulas.TryGetValue(key, out var formula) ? formula(line) : 0m;

        /// <summary>يعيد حساب كل الأعمدة المحسوبة في السطر (Type=Computed) ويكتب النتيجة في نفس مفتاح كل عمود.</summary>
        public static void Recalculate(DocumentLine line, List<LineColumn> columns)
        {
            foreach (var col in columns.Where(c => c.Type == LineColumnType.Computed && !string.IsNullOrEmpty(c.ComputeExpression)))
                line[col.Key] = Compute(col.ComputeExpression, line);
        }
    }
}
