using System.Collections.Generic;

namespace PrimeERP.UI.Components.Documents
{
    /// <summary>مجموعات إجماليات جاهزة لأنماط المستندات القياسية — تُستخدم مع DocumentFooter.TotalsSource.</summary>
    public static class FooterPresets
    {
        public static List<FooterTotal> Invoice(decimal subtotal, decimal discount, decimal tax, decimal net) => new()
        {
            new() { Key = "Subtotal", Label = "الإجمالي الفرعي", Value = subtotal },
            new() { Key = "Discount", Label = "الخصم",           Value = discount },
            new() { Key = "Tax",      Label = "الضريبة",         Value = tax },
            new() { Key = "Net",      Label = "الصافي",          Value = net, IsBold = true, IsLarge = true, Variant = "brand" },
        };

        public static List<FooterTotal> Journal(decimal debit, decimal credit)
        {
            var difference = debit - credit;
            return new()
            {
                new() { Key = "Debit",      Label = "إجمالي المدين", Value = debit },
                new() { Key = "Credit",     Label = "إجمالي الدائن", Value = credit },
                new() { Key = "Difference", Label = "الفرق",         Value = difference, IsBold = true, IsLarge = true,
                        Variant = difference == 0 ? "success" : "danger" },
            };
        }

        public static List<FooterTotal> Stock(decimal totalQty, decimal totalCost) => new()
        {
            new() { Key = "TotalQty",  Label = "إجمالي الكمية",   Value = totalQty },
            new() { Key = "TotalCost", Label = "إجمالي التكلفة", Value = totalCost, IsBold = true, IsLarge = true, Variant = "brand" },
        };
    }
}
