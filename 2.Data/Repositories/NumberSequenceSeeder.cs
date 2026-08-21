using PrimeERP.Platform.Settings;

namespace PrimeERP.Data.Repositories
{
    /// <summary>
    /// يزرع تسلسلات الأرقام ذات بادئة مُعدَّة مسبقاً من الإعدادات (لا EnsureRow التلقائية التي كانت ستجعل
    /// البادئة اسم المفتاح نفسه، مثال "Customer-2026-00001" بدل "C-2026-00001"). لا يفعل شيئاً لو السلسلة
    /// موجودة بالفعل (idempotent عبر EnsureRow). يجب أن يعمل بعد SettingSeeder.Seed() (يقرأ قيم البادئة منه).
    /// </summary>
    public static class NumberSequenceSeeder
    {
        public static void Seed()
        {
            EnsureFromSetting("Customer", SettingKeys.Documents.CustomerPrefix, "C");
            EnsureFromSetting("Supplier", SettingKeys.Documents.SupplierPrefix, "S");
            EnsureFromSetting("Product",  SettingKeys.Documents.ProductPrefix,  "P");
        }

        private static void EnsureFromSetting(string key, string settingKey, string fallbackPrefix)
        {
            var setting = SettingRepository.GetByKey(settingKey);
            var prefix = setting != null && !string.IsNullOrWhiteSpace(setting.Value) ? setting.Value : fallbackPrefix;
            NumberSequenceRepository.EnsureRow(key, prefix);
        }
    }
}
