using System.Collections.Generic;
using PrimeERP.Platform.Localization;

namespace PrimeERP.UI.Components.Documents
{
    /// <summary>تعريفات أعمدة جاهزة لأنماط المستندات</summary>
    public static class LineColumnPresets
    {
        public static List<LineColumn> Journal() => new()
        {
            new() { Key = "LineNo",  Header = "#",         Type = LineColumnType.RowNumber, Width = 40,  IsReadOnly = true },
            new() { Key = "ItemCode",Header = LocalizationService.Get("Str.Field.AccountCode"), Type = LineColumnType.Picker, PickerType = LinePickerType.Account, Width = 110,
                    FillsFrom = new() { ["ItemName"] = "Name" } },
            new() { Key = "ItemName",Header = LocalizationService.Get("Str.Field.AccountName"), Type = LineColumnType.Text, Width = 200, IsStarWidth = true, IsReadOnly = true },
            new() { Key = "Debit",   Header = LocalizationService.Get("Str.Debit"), Type = LineColumnType.Money, Width = 120, Footer = LineColumnFooter.Sum },
            new() { Key = "Credit",  Header = LocalizationService.Get("Str.Credit"), Type = LineColumnType.Money, Width = 120, Footer = LineColumnFooter.Sum },
            new() { Key = "Notes",   Header = LocalizationService.Get("Str.Line.Note"), Type = LineColumnType.Text, Width = 160 },
        };

        public static List<LineColumn> SalesInvoice() => BuildInvoiceColumns(LinePickerType.Product);

        public static List<LineColumn> PurchaseInvoice() => BuildInvoiceColumns(LinePickerType.Product);

        public static List<LineColumn> StockVoucher() => new()
        {
            new() { Key = "LineNo",  Header = "#",     Type = LineColumnType.RowNumber, Width = 40, IsReadOnly = true },
            new() { Key = "ItemCode",Header = LocalizationService.Get("Str.Line.ProductCode"), Type = LineColumnType.Picker, PickerType = LinePickerType.Product, Width = 110,
                    FillsFrom = new() { ["ItemName"] = "Name" } },
            new() { Key = "ItemName",Header = LocalizationService.Get("Str.Line.ProductName"), Type = LineColumnType.Text, Width = 200, IsStarWidth = true, IsReadOnly = true },
            new() { Key = "Qty",     Header = LocalizationService.Get("Str.Qty"), Type = LineColumnType.Decimal, Width = 90, IsRequired = true },
            new() { Key = "Price",   Header = LocalizationService.Get("Str.Line.Cost"), Type = LineColumnType.Money, Width = 100 },
            new() { Key = "LineTotal", Header = LocalizationService.Get("Str.Total"), Type = LineColumnType.Computed, ComputeExpression = "StockTotal",
                    Width = 110, IsReadOnly = true, Footer = LineColumnFooter.Sum },
            new() { Key = "Notes",   Header = LocalizationService.Get("Str.Line.Note"), Type = LineColumnType.Text, Width = 140 },
        };

        private static List<LineColumn> BuildInvoiceColumns(LinePickerType pickerType) => new()
        {
            new() { Key = "LineNo",  Header = "#",      Type = LineColumnType.RowNumber, Width = 40, IsReadOnly = true },
            new() { Key = "ItemCode",Header = LocalizationService.Get("Str.Line.ProductCode"),  Type = LineColumnType.Picker, PickerType = pickerType, Width = 110,
                    FillsFrom = new() { ["ItemName"] = "Name", ["Price"] = "SalePrice", ["UnitName"] = "UnitName", ["VatPercent"] = "TaxRate" } },
            new() { Key = "ItemName",Header = LocalizationService.Get("Str.Line.ProductName"),  Type = LineColumnType.Text, Width = 180, IsStarWidth = true, IsReadOnly = true },
            new() { Key = "Qty",     Header = LocalizationService.Get("Str.Qty"), Type = LineColumnType.Decimal, Width = 80, IsRequired = true },
            new() { Key = "UnitName",Header = LocalizationService.Get("Str.Line.Unit"), Type = LineColumnType.Text, Width = 70, IsReadOnly = true },
            new() { Key = "Price",   Header = LocalizationService.Get("Str.Line.Price"),  Type = LineColumnType.Money, Width = 100 },
            new() { Key = "DiscountPercent", Header = LocalizationService.Get("Str.Line.DiscountPercent"),    Type = LineColumnType.Percent, Width = 70 },
            new() { Key = "DiscountAmount",  Header = LocalizationService.Get("Str.Line.Discount"),   Type = LineColumnType.Computed, ComputeExpression = "DiscountAmount", Width = 90, IsReadOnly = true },
            new() { Key = "VatPercent",      Header = LocalizationService.Get("Str.Line.VatPercent"),  Type = LineColumnType.Percent, Width = 70 },
            new() { Key = "VatAmount",       Header = LocalizationService.Get("Str.TaxAmount"), Type = LineColumnType.Computed, ComputeExpression = "VatAmount", Width = 90, IsReadOnly = true },
            new() { Key = "LineTotal",       Header = LocalizationService.Get("Str.Total"),Type = LineColumnType.Computed, ComputeExpression = "LineTotal", Width = 110,
                    IsReadOnly = true, Footer = LineColumnFooter.Sum },
        };
    }
}
