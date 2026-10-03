using System.Collections.Generic;
using PrimeERP.Composition.Definitions;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Composition.Print
{
    /// <summary>شكل الورق التجاري</summary>
    public static class TradePaper
    {
        public static List<PrintColumnDefinition> Columns() => new()
        {
            new() { Key = "ProductCode", Header = LocalizationService.Get("Str.Code"),        Width = 1.3, IsText = true },
            new() { Key = "ProductName", Header = LocalizationService.Get("Str.Line.ProductName"),    Width = 2.4, IsText = true },
            new() { Key = "Qty",         Header = LocalizationService.Get("Str.Qty"),       Width = 0.8 },
            new() { Key = "UnitPrice",   Header = LocalizationService.Get("Str.Line.Price"),        Width = 1.0 },
            new() { Key = "LineTotal",   Header = LocalizationService.Get("Str.Total"),     Width = 1.1 },
            new() { Key = "DiscountAmount", Header = LocalizationService.Get("Str.Line.Discount"),     Width = 1.0 },
            new() { Key = "VatAmount",   Header = LocalizationService.Get("Str.Print.VatShort"), Width = 1.2 },
            new() { Key = "WithholdingAmount", Header = LocalizationService.Get("Str.Print.WithholdingShort"), Width = 1.2 },
            new() { Key = "NetAmount",   Header = LocalizationService.Get("Str.Print.NetAmount"),  Width = 1.2 },
            new() { Key = "Notes",       Header = LocalizationService.Get("Str.Notes"),     Width = 1.6, IsText = true },
        };

        public static LineFieldDefinition NetColumn() => new()
        {
            Key = "NetAmount", Header = LocalizationService.Get("Str.Print.NetAmount"), Kind = FieldKind.ReadOnly, Width = 110
        };

        public static LineMathDefinition LineMath() => new()
        {
            QtyKey = "Qty", PriceKey = "UnitPrice",
            DiscountPercentKey = "DiscountPercent", VatPercentKey = "VatPercent",
            WithholdingPercentKey = "WithholdingPercent", NetKey = "NetAmount"
        };

        public static List<PrintTotalDefinition> Totals() => new()
        {
            new() { Key = "SubTotal",          Label = LocalizationService.Get("Str.Total") },
            new() { Key = "DiscountAmount",    Label = LocalizationService.Get("Str.Line.Discount"), HideWhenZero = true },
            new() { Key = "VatAmount",         Label = LocalizationService.Get("Str.Print.Vat"), HideWhenZero = true },
            new() { Key = "WithholdingAmount", Label = LocalizationService.Get("Str.Print.Withholding"), HideWhenZero = true },
            new() { Key = "NetTotal",          Label = LocalizationService.Get("Str.Print.NetDue"), IsBold = true },
        };
    }
}
