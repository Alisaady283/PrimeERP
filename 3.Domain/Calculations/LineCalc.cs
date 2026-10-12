using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Domain.Calculations
{
    /// <summary>مبالغ سطر واحد بعد الخصم</summary>
    public readonly record struct LineAmounts(
        decimal Gross, decimal Discount, decimal Taxable, decimal Vat, decimal Withholding, decimal Net);

    /// <summary>مصدر واحد لحساب مبالغ المستندات</summary>
    public static class LineCalc
    {
        public static LineAmounts ForLine(decimal qty, decimal unitPrice,
            decimal discountPercent, decimal vatPercent, decimal withholdingPercent)
        {
            var gross = Round(qty * unitPrice);
            var discount = Round(gross * discountPercent / 100m);
            var taxable = gross - discount;
            var vat = Round(taxable * vatPercent / 100m);
            var withholding = Round((taxable + vat) * withholdingPercent / 100m);   // الأساس: الصافي بعد الخصم والضريبة

            return new LineAmounts(gross, discount, taxable, vat, withholding, taxable + vat - withholding);
        }

        public static (decimal Collected, decimal Change) Tender(decimal paid, decimal due) =>
            paid <= 0 ? (due, 0) : (System.Math.Min(paid, due), System.Math.Max(paid - due, 0));

        public static decimal Drawer(decimal opening, decimal cashIn, decimal cashOut) => opening + cashIn - cashOut;

        public static decimal DrawerDifference(decimal counted, decimal opening, decimal cashIn, decimal cashOut) =>
            counted - Drawer(opening, cashIn, cashOut);

        public static LineAmounts Sum(IEnumerable<LineAmounts> lines) =>
            lines.Aggregate(new LineAmounts(), (a, b) => new LineAmounts(
                a.Gross + b.Gross, a.Discount + b.Discount, a.Taxable + b.Taxable,
                a.Vat + b.Vat, a.Withholding + b.Withholding, a.Net + b.Net));

        /// <summary>حال السطر من المسحوب منه إلى المرحلة التالية ثم الأخيرة إن وُجدت</summary>
        public static Enums.FlowState Progress(decimal qty, decimal done, decimal? closed = null) =>
            done <= 0 ? Enums.FlowState.Open
            : done < qty ? Enums.FlowState.Partial
            : closed is decimal last && last < qty ? Enums.FlowState.Awaiting
            : Enums.FlowState.Complete;

        private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
