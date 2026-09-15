using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Data.Seeders
{
    /// <summary>
    /// يزرع تسلسلات الأرقام ذات بادئة مُعدَّة مسبقاً من الإعدادات (لا EnsureRow التلقائية التي كانت ستجعل
    /// البادئة اسم المفتاح نفسه، مثال "Customer-2026-00001" بدل "C-2026-00001"). لا يفعل شيئاً لو السلسلة
    /// موجودة بالفعل (idempotent عبر EnsureRow). يجب أن يعمل بعد SettingSeeder.Seed() (يقرأ قيم البادئة منه).
    /// </summary>
    public static class NumberSequenceSeeder
    {
        public static void Seed(INumberSequenceRepository numberSequences)
        {
            EnsureFromSetting(numberSequences, "Customer", SettingKeys.Documents.CustomerPrefix, "C");
            EnsureFromSetting(numberSequences, "Supplier", SettingKeys.Documents.SupplierPrefix, "S");
            EnsureFromSetting(numberSequences, "Product",  SettingKeys.Documents.ProductPrefix,  "P");

            // سجلٌّ لا مستند: سريال متصل قصير بلا سنة.
            numberSequences.EnsureRow("Treasury",  "TR", padding: 4, resetYearly: false);
            numberSequences.EnsureRow("Warehouse", "WH", padding: 4, resetYearly: false);
        }

        private static void EnsureFromSetting(INumberSequenceRepository numberSequences, string key, string settingKey, string fallbackPrefix)
        {
            var setting = SettingRepository.GetByKey(settingKey);
            var prefix = setting != null && !string.IsNullOrWhiteSpace(setting.Value) ? setting.Value : fallbackPrefix;
            numberSequences.EnsureRow(key, prefix);
        }
    }
}
