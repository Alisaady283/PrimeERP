using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Domain.Helpers
{
    /// <summary>مبالغ سطر واحد بعد الخصم والضريبتين.</summary>
    public readonly record struct LineAmounts(
        decimal Gross, decimal Discount, decimal Taxable, decimal Vat, decimal Withholding, decimal Net);

    /// <summary>
    /// مصدر واحد لحساب مبالغ المستندات التجارية — فواتير البيع والشراء ومرتجعاتها تستدعيه بدل أربع نسخ.
    /// الوعاء = الإجمالي ناقص الخصم. القيمة المضافة تُضاف للمستحق، وضريبة الخصم والإضافة تُحجَز منه
    /// لصالح مصلحة الضرائب فتُنقص الصافي بلا أن تمسّ الإيراد.
    /// </summary>
    public static class DocumentTotals
    {
        public static LineAmounts ForLine(decimal qty, decimal unitPrice,
            decimal discountPercent, decimal vatPercent, decimal withholdingPercent)
        {
            var gross = Round(qty * unitPrice);
            var discount = Round(gross * discountPercent / 100m);
            var taxable = gross - discount;
            var vat = Round(taxable * vatPercent / 100m);
            var withholding = Round(taxable * withholdingPercent / 100m);

            return new LineAmounts(gross, discount, taxable, vat, withholding, taxable + vat - withholding);
        }

        public static LineAmounts Sum(IEnumerable<LineAmounts> lines) =>
            lines.Aggregate(new LineAmounts(), (a, b) => new LineAmounts(
                a.Gross + b.Gross, a.Discount + b.Discount, a.Taxable + b.Taxable,
                a.Vat + b.Vat, a.Withholding + b.Withholding, a.Net + b.Net));

        private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
