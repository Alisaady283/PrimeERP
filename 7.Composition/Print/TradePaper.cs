using System.Collections.Generic;
using PrimeERP.Composition.Definitions;

namespace PrimeERP.Composition.Print
{
    /// <summary>شكل الورق التجاري</summary>
    public static class TradePaper
    {
        public static List<PrintColumnDefinition> Columns() => new()
        {
            new() { Key = "ProductCode", Header = "الكود",        Width = 1.3, IsText = true },
            new() { Key = "ProductName", Header = "اسم الصنف",    Width = 2.4, IsText = true },
            new() { Key = "Qty",         Header = "الكمية",       Width = 0.8 },
            new() { Key = "UnitPrice",   Header = "السعر",        Width = 1.0 },
            new() { Key = "LineTotal",   Header = "الإجمالي",     Width = 1.1 },
            new() { Key = "DiscountAmount", Header = "الخصم",     Width = 1.0 },
            new() { Key = "VatAmount",   Header = "ض. القيمة المضافة", Width = 1.2 },
            new() { Key = "WithholdingAmount", Header = "ض. الخصم والإضافة", Width = 1.2 },
            new() { Key = "NetAmount",   Header = "صافي المبلغ",  Width = 1.2 },
            new() { Key = "Notes",       Header = "ملاحظات",     Width = 1.6, IsText = true },
        };

        public static LineFieldDefinition NetColumn() => new()
        {
            Key = "NetAmount", Header = "صافي المبلغ", Kind = FieldKind.ReadOnly, Width = 110
        };

        public static LineMathDefinition LineMath() => new()
        {
            QtyKey = "Qty", PriceKey = "UnitPrice",
            DiscountPercentKey = "DiscountPercent", VatPercentKey = "VatPercent",
            WithholdingPercentKey = "WithholdingPercent", NetKey = "NetAmount"
        };

        public static List<PrintTotalDefinition> Totals() => new()
        {
            new() { Key = "SubTotal",          Label = "الإجمالي" },
            new() { Key = "DiscountAmount",    Label = "الخصم", HideWhenZero = true },
            new() { Key = "VatAmount",         Label = "ضريبة القيمة المضافة", HideWhenZero = true },
            new() { Key = "WithholdingAmount", Label = "ضريبة الخصم والإضافة", HideWhenZero = true },
            new() { Key = "NetTotal",          Label = "الصافي المستحق", IsBold = true },
        };
    }
}
