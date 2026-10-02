namespace PrimeERP.Composition.Definitions
{
    /// <summary>تبويبات صفحة الإعدادات</summary>
    public static class SettingsTabs
    {
        public static readonly (string Category, string TitleKey)[] All =
        {
            ("Company", "Str.Settings.Company"), ("Financial", "Str.Settings.Financial"), ("Accounts", "Str.Settings.Accounts"),
            ("Documents", "Str.Settings.Documents"), ("UI", "Str.Settings.UI"), ("Print", "الطباعة"), ("Backup", "Str.Settings.Backup"), ("Security", "Str.Settings.Security"),
        };
    }
}
