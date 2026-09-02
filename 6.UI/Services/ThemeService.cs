using System;

namespace PrimeERP.UI.Services
{
    public enum AppThemeMode
    {
        Light,
        Dark
    }

    /// <summary>حالة الوضع فقط — بناء شجرة الموارد كله في IdentityService، إذ يجب أن يُدرَج قاموس الوضع
    /// الداكن قبل L3 لا فوقه (وإلا بقيت رموز C.* على قيم الفاتح).</summary>
    public static class ThemeService
    {
        public static AppThemeMode CurrentMode { get; private set; } = AppThemeMode.Light;

        public static event EventHandler ThemeChanged;

        public static void SetMode(AppThemeMode mode)
        {
            CurrentMode = mode;
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        public static PrimeERP.Platform.Design.ThemeMode NextMode =>
            CurrentMode == AppThemeMode.Light
                ? PrimeERP.Platform.Design.ThemeMode.Dark
                : PrimeERP.Platform.Design.ThemeMode.Light;
    }
}
