using System.Collections.Generic;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Composition.Definitions;

namespace PrimeERP.Modules
{
    /// <summary>
    /// سلسلة السحب في الدورة الشاملة، مُعلَنةً في موضع واحد: من يسحب ممّن، وبأي حقل يُطابَق.
    ///
    ///   شراء:  طلب شراء → أمر شراء  → إذن استلام → فاتورة شراء → مرتجع شراء → إذن صرف مرتجع
    ///   بيع:   عرض سعر  → أمر توريد → إذن صرف    → فاتورة بيع  → مرتجع بيع  → إذن استلام مرتجع
    ///
    /// **كل مستند يسحب ممّا قبله مباشرةً وحده، لا ممّا قبل ذلك.** الحلقة السابقة سحبت ممّا قبلها
    /// بالفعل، فسحبُ الفاتورة من الأمر *وَ* من الإذن معاً يخصم الكمية مرّتين من نفس الأمر.
    ///
    /// المحرّك (PullService) لا يعرف نوع مستند بعينه، فإضافة حلقة جديدة سطرٌ هنا لا كودٌ هناك.
    /// </summary>
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

        /// <summary>من إذن الاستلام وحده — وهو سحب من أمر الشراء قبله.</summary>
        public static List<PullSource> IntoPurchaseInvoice() => new()
        {
            Unmatched("GoodsReceipt", "سحب من إذن استلام", Purchases),
        };

        /// <summary>من إذن الصرف وحده — وهو سحب من أمر التوريد قبله.</summary>
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

        /// <summary>أذون المخزن تُسحب من مستندات الدورة — مصدرها يُعلَن عند تسجيلها لأنها تُبنى بمصنع آخر.</summary>
        public static PullSource IntoStockVoucher(string sourceKind, string label) =>
            Unmatched(sourceKind, label, "Inventory.Create");

        private const string Sales     = "Sales.Create";
        private const string Purchases = "Purchases.Create";

        /// <summary>مصدرٌ يحمل PartyId مثل الهدف، فيُقصَر المعروض على طرف المستند الحالي.</summary>
        private static PullSource ByParty(string sourceKind, string label, string permissionKey) =>
            By(sourceKind, label, permissionKey, nameof(CreateCycleDocumentDto.PartyId));

        private static PullSource By(string sourceKind, string label, string permissionKey, string matchField) => new()
        {
            SourceKind = sourceKind, Label = label, PermissionKey = permissionKey,
            MatchFields = new List<string> { matchField },
        };

        /// <summary>بلا حقل مطابقة: المصدر والهدف يسمّيان الطرف باسمين مختلفين (PartyId مقابل
        /// CustomerId/SupplierId)، أو المصدر بلا طرف أصلاً (أذون المخزن) — فالقصر يُخفي كل المستندات.</summary>
        private static PullSource Unmatched(string sourceKind, string label, string permissionKey) => new()
        {
            SourceKind = sourceKind, Label = label, PermissionKey = permissionKey,
            MatchFields = new List<string>(),
        };
    }
}
