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

            numberSequences.EnsureRow("Treasury",  "TR", padding: 4, resetYearly: false);
            numberSequences.EnsureRow("Warehouse", "WH", padding: 4, resetYearly: false);
        }

        private static void EnsureFromSetting(INumberSequenceRepository numberSequences, ISettingStore settings,
            string key, string settingKey, string fallbackPrefix)
        {
            var setting = settings.GetByKey(settingKey);
            var prefix = setting != null && !string.IsNullOrWhiteSpace(setting.Value) ? setting.Value : fallbackPrefix;
            numberSequences.EnsureRow(key, prefix);
        }
    }
}
