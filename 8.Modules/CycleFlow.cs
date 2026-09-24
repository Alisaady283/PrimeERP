using System.Collections.Generic;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Composition.Definitions;

namespace PrimeERP.Modules
{
    /// <summary>سلسلة السحب في الدورة الشاملة</summary>
    public static class CycleFlow
    {
        public static List<PullSource> IntoPurchaseOrder() => new()
        {
            ByParty("PurchaseRequest", "سحب من طلب شراء", Purchases),
        };

        public static List<PullSource> IntoSalesOrder() => new()
        {
            ByParty("Quotation", "سحب من عرض سعر", Sales),
        };

        public static List<PullSource> IntoPurchaseInvoice() => new()
        {
            Unmatched("GoodsReceipt", "سحب من إذن استلام", Purchases),
        };

        public static List<PullSource> IntoSalesInvoice() => new()
        {
            Unmatched("DeliveryNote", "سحب من إذن صرف", Sales),
        };

        public static List<PullSource> IntoPurchaseReturn() => new()
        {
            By("PurchaseInvoices", "سحب من فاتورة شراء", Purchases, "SupplierId"),
        };

        public static List<PullSource> IntoSalesReturn() => new()
        {
            By("SalesInvoices", "سحب من فاتورة بيع", Sales, "CustomerId"),
        };

        public static PullSource IntoStockVoucher(string sourceKind, string label) =>
            Unmatched(sourceKind, label, "Inventory.Create");

        private const string Sales     = "Sales.Create";
        private const string Purchases = "Purchases.Create";

        private static PullSource ByParty(string sourceKind, string label, string permissionKey) =>
            By(sourceKind, label, permissionKey, nameof(CreateCycleDocumentDto.PartyId));

        private static PullSource By(string sourceKind, string label, string permissionKey, string matchField) => new()
        {
            SourceKind = sourceKind, Label = label, PermissionKey = permissionKey,
            MatchFields = new List<string> { matchField },
        };

        private static PullSource Unmatched(string sourceKind, string label, string permissionKey) => new()
        {
            SourceKind = sourceKind, Label = label, PermissionKey = permissionKey,
            MatchFields = new List<string>(),
        };
    }
}
