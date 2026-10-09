using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>تبويبات صفحة الإعدادات</summary>
    public static class SettingsTabs
    {
        public static readonly (string Category, string TitleKey)[] All =
        {
            ("Company", "Str.Settings.Company"), ("Financial", "Str.Settings.Financial"), ("Accounts", "Str.Settings.Accounts"),
            ("Documents", "Str.Settings.Documents"), ("UI", "Str.Settings.UI"), ("Print", "Str.Settings.Print"), ("Backup", "Str.Settings.Backup"), ("Security", "Str.Settings.Security"),
            ("HR", "Str.Settings.HR"), ("System", "Str.Settings.System"),
        };

        /// <summary>مفتاحه في بيان النسخة</summary>
        public static string Key(string category) => $"Settings.{category}";

        /// <summary>بيانٌ بلا تبويبٍ يعرضها كلها</summary>
        public static (string Category, string TitleKey)[] Visible(IReadOnlyCollection<string> manifest) =>
            manifest == null || !All.Any(t => manifest.Contains(Key(t.Category)))
                ? All
                : All.Where(t => manifest.Contains(Key(t.Category))).ToArray();
    }
}
