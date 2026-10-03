using System.Collections.Generic;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Composition.Definitions;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Modules
{
    /// <summary>سلسلة السحب في الدورة الشاملة</summary>
    public static class CycleFlow
    {
        public static List<PullSource> IntoPurchaseOrder() => new()
        {
            ByParty("PurchaseRequest", Purchases),
        };

        public static List<PullSource> IntoSalesOrder() => new()
        {
            ByParty("Quotation", Sales),
        };

        public static List<PullSource> IntoPurchaseInvoice() => new()
        {
            Unmatched("GoodsReceipt", Purchases),
        };

        public static List<PullSource> IntoSalesInvoice() => new()
        {
            Unmatched("DeliveryNote", Sales),
        };

        public static List<PullSource> IntoPurchaseReturn() => new()
        {
            By("PurchaseInvoices", Purchases, "SupplierId"),
        };

        public static List<PullSource> IntoSalesReturn() => new()
        {
            By("SalesInvoices", Sales, "CustomerId"),
        };

        public static PullSource IntoStockVoucher(string sourceKind) =>
            Unmatched(sourceKind, "Inventory.Create");

        private const string Sales     = "Sales.Create";
        private const string Purchases = "Purchases.Create";

        private static PullSource ByParty(string sourceKind, string permissionKey) =>
            By(sourceKind, permissionKey, nameof(CreateCycleDocumentDto.PartyId));

        private static PullSource By(string sourceKind, string permissionKey, string matchField) => new()
        {
            SourceKind = sourceKind, Label = LabelOf(sourceKind), PermissionKey = permissionKey,
            MatchFields = new List<string> { matchField },
        };

        private static PullSource Unmatched(string sourceKind, string permissionKey) => new()
        {
            SourceKind = sourceKind, Label = LabelOf(sourceKind), PermissionKey = permissionKey,
            MatchFields = new List<string>(),
        };

        /// <summary>عنوان السحب من مصدره</summary>
        private static string LabelOf(string sourceKind) => LocalizationService.Get($"Str.Pull.From.{sourceKind}");
    }
}
