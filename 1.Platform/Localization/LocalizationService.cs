using System;
using System.Linq;
using System.Windows;

namespace PrimeERP.Platform.Localization
{
    public enum AppLanguage
    {
        Ar,
        En
    }

    /// <summary>يبدّل قاموس النصوص (Strings.ar/en.xaml) واتجاه الواجهة (RTL/LTR) معاً.</summary>
    public static class LocalizationService
    {
        public static AppLanguage CurrentLanguage { get; private set; } = AppLanguage.Ar;

        public static event EventHandler LanguageChanged;

        public static void Apply(AppLanguage language)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            var dicts = app.Resources.MergedDictionaries;

            var existing = dicts.FirstOrDefault(d =>
                d.Source != null &&
                (d.Source.OriginalString.EndsWith("Strings.ar.xaml") ||
                 d.Source.OriginalString.EndsWith("Strings.en.xaml")));

            if (existing != null)
                dicts.Remove(existing);

            var path = language == AppLanguage.Ar
                ? "5.Design/Strings/Strings.ar.xaml"
                : "5.Design/Strings/Strings.en.xaml";

            dicts.Add(new ResourceDictionary { Source = new Uri(path, UriKind.Relative) });

            CurrentLanguage = language;

            var flowDirection = language == AppLanguage.Ar
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;

            if (app.MainWindow != null)
                app.MainWindow.FlowDirection = flowDirection;

            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void Toggle() =>
            Apply(CurrentLanguage == AppLanguage.Ar ? AppLanguage.En : AppLanguage.Ar);

        /// <summary>يجلب نص "Str.X" مباشرة من القاموس الحالي — مفيد من الكود بدل XAML binding.</summary>
        public static string Get(string key)
        {
            if (System.Windows.Application.Current?.Resources.Contains(key) == true)
                return System.Windows.Application.Current.Resources[key] as string ?? key;
            return key;
        }
    }
}
