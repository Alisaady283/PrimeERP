using System;
using System.Linq;
using System.Windows;

namespace PrimeERP.Services
{
    public enum AppThemeMode
    {
        Light,
        Dark
    }

    /// <summary>يبدّل بين الفاتح والداكن بإضافة/إزالة Semantic.Dark.xaml فوق Semantic.Light.xaml — كل عناصر الواجهة المرتبطة بـ DynamicResource تتحدّث تلقائياً. الآلية نفسها يستدعيها IIdentityService.ApplyMode — هذا الكلاس هو التنفيذ الفعلي، لا تكرار له.</summary>
    public static class ThemeService
    {
        private const string DarkDictPath = "Resources/Design/Semantic/Semantic.Dark.xaml";

        public static AppThemeMode CurrentMode { get; private set; } = AppThemeMode.Light;

        public static event EventHandler ThemeChanged;

        public static void Apply(AppThemeMode mode)
        {
            var app = Application.Current;
            if (app == null) return;

            var dicts = app.Resources.MergedDictionaries;
            var existing = dicts.FirstOrDefault(d =>
                d.Source != null && d.Source.OriginalString.EndsWith("Semantic.Dark.xaml"));

            if (mode == AppThemeMode.Dark)
            {
                if (existing == null)
                    dicts.Add(new ResourceDictionary { Source = new Uri(DarkDictPath, UriKind.Relative) });
            }
            else
            {
                if (existing != null)
                    dicts.Remove(existing);
            }

            CurrentMode = mode;
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void Toggle() =>
            Apply(CurrentMode == AppThemeMode.Light ? AppThemeMode.Dark : AppThemeMode.Light);
    }
}
