using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Data.Seeders
{
    /// <summary>يزرع تسلسلات الأرقام ذات بادئة</summary>
    public static class NumberSequenceSeeder
    {
        public static void Seed(INumberSequenceRepository numberSequences, ISettingStore settings)
        {
            EnsureFromSetting(numberSequences, settings, "Customer", SettingKeys.Documents.CustomerPrefix, "C");
            EnsureFromSetting(numberSequences, settings, "Supplier", SettingKeys.Documents.SupplierPrefix, "S");
            EnsureFromSetting(numberSequences, settings, "Product",  SettingKeys.Documents.ProductPrefix,  "P");
            EnsureFromSetting(numberSequences, settings, "SalesInvoice",    SettingKeys.Documents.SalesInvoicePrefix,    "INV");
            EnsureFromSetting(numberSequences, settings, "PurchaseInvoice", SettingKeys.Documents.PurchaseInvoicePrefix, "PI");

            foreach (var (key, prefix) in Prefixes) numberSequences.EnsureRow(key, prefix);
        }

        private static readonly (string Key, string Prefix)[] Prefixes =
        {
            ("Treasury", "TR"), ("Warehouse", "WH"), ("Asset", "AS"), ("Employee", "EM"), ("Payroll", "PAY"),
            ("SalesReturn", "SR"), ("PurchaseReturn", "PR"), ("Quotation", "QT"), ("SalesOrder", "SO"),
            ("PurchaseRequest", "RQ"), ("PurchaseOrder", "PO"), ("GoodsReceipt", "GR"), ("GoodsIssue", "GI"),
            ("DeliveryNote", "DN"), ("SalesReceipt", "SN"), ("StockIn", "IN"), ("StockOut", "OUT"),
            ("StockTransfer", "TRF"), ("StockMovement", "MV"), ("ReceiptVoucher", "RV"), ("PaymentVoucher", "PV"),
        };

        private static void EnsureFromSetting(INumberSequenceRepository numberSequences, ISettingStore settings,
            string key, string settingKey, string fallbackPrefix)
        {
            var setting = settings.GetByKey(settingKey);
            var prefix = setting != null && !string.IsNullOrWhiteSpace(setting.Value) ? setting.Value : fallbackPrefix;
            numberSequences.EnsureRow(key, prefix);
        }
    }
}
