using System.Collections.Generic;

namespace PrimeERP.UI.Components.Documents
{
    /// <summary>تعريفات أعمدة جاهزة لأنماط المستندات</summary>
    public static class LineColumnPresets
    {
        public static List<LineColumn> Journal() => new()
        {
            new() { Key = "LineNo",  Header = "#",         Type = LineColumnType.RowNumber, Width = 40,  IsReadOnly = true },
            new() { Key = "ItemCode",Header = "كود الحساب", Type = LineColumnType.Picker, PickerType = LinePickerType.Account, Width = 110,
                    FillsFrom = new() { ["ItemName"] = "Name" } },
            new() { Key = "ItemName",Header = "اسم الحساب", Type = LineColumnType.Text, Width = 200, IsStarWidth = true, IsReadOnly = true },
            new() { Key = "Debit",   Header = "مدين",       Type = LineColumnType.Money, Width = 120, Footer = LineColumnFooter.Sum },
            new() { Key = "Credit",  Header = "دائن",       Type = LineColumnType.Money, Width = 120, Footer = LineColumnFooter.Sum },
            new() { Key = "Notes",   Header = "بيان",       Type = LineColumnType.Text, Width = 160 },
        };

        public static List<LineColumn> SalesInvoice() => BuildInvoiceColumns(LinePickerType.Product);

        public static List<LineColumn> PurchaseInvoice() => BuildInvoiceColumns(LinePickerType.Product);

        public static List<LineColumn> StockVoucher() => new()
        {
            new() { Key = "LineNo",  Header = "#",     Type = LineColumnType.RowNumber, Width = 40, IsReadOnly = true },
            new() { Key = "ItemCode",Header = "كود الصنف", Type = LineColumnType.Picker, PickerType = LinePickerType.Product, Width = 110,
                    FillsFrom = new() { ["ItemName"] = "Name" } },
            new() { Key = "ItemName",Header = "اسم الصنف", Type = LineColumnType.Text, Width = 200, IsStarWidth = true, IsReadOnly = true },
            new() { Key = "Qty",     Header = "الكمية", Type = LineColumnType.Decimal, Width = 90, IsRequired = true },
            new() { Key = "Price",   Header = "التكلفة", Type = LineColumnType.Money, Width = 100 },
            new() { Key = "LineTotal", Header = "الإجمالي", Type = LineColumnType.Computed, ComputeExpression = "StockTotal",
                    Width = 110, IsReadOnly = true, Footer = LineColumnFooter.Sum },
            new() { Key = "Notes",   Header = "بيان",  Type = LineColumnType.Text, Width = 140 },
        };

        private static List<LineColumn> BuildInvoiceColumns(LinePickerType pickerType) => new()
        {
            new() { Key = "LineNo",  Header = "#",      Type = LineColumnType.RowNumber, Width = 40, IsReadOnly = true },
            new() { Key = "ItemCode",Header = "كود الصنف",  Type = LineColumnType.Picker, PickerType = pickerType, Width = 110,
                    FillsFrom = new() { ["ItemName"] = "Name", ["Price"] = "SalePrice", ["UnitName"] = "UnitName", ["VatPercent"] = "TaxRate" } },
            new() { Key = "ItemName",Header = "اسم الصنف",  Type = LineColumnType.Text, Width = 180, IsStarWidth = true, IsReadOnly = true },
            new() { Key = "Qty",     Header = "الكمية", Type = LineColumnType.Decimal, Width = 80, IsRequired = true },
            new() { Key = "UnitName",Header = "الوحدة", Type = LineColumnType.Text, Width = 70, IsReadOnly = true },
            new() { Key = "Price",   Header = "السعر",  Type = LineColumnType.Money, Width = 100 },
            new() { Key = "DiscountPercent", Header = "خصم%",    Type = LineColumnType.Percent, Width = 70 },
            new() { Key = "DiscountAmount",  Header = "الخصم",   Type = LineColumnType.Computed, ComputeExpression = "DiscountAmount", Width = 90, IsReadOnly = true },
            new() { Key = "VatPercent",      Header = "ضريبة%",  Type = LineColumnType.Percent, Width = 70 },
            new() { Key = "VatAmount",       Header = "الضريبة", Type = LineColumnType.Computed, ComputeExpression = "VatAmount", Width = 90, IsReadOnly = true },
            new() { Key = "LineTotal",       Header = "الإجمالي",Type = LineColumnType.Computed, ComputeExpression = "LineTotal", Width = 110,
                    IsReadOnly = true, Footer = LineColumnFooter.Sum },
        };
    }
}
