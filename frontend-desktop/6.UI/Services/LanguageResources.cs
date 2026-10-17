using System.Windows;
using PrimeERP.Platform.Localization;

namespace PrimeERP.UI.Services
{
    /// <summary>اللغة في واجهة WPF: نصوص StringTable قاموسَ موارد لـDynamicResource، واتجاه النوافذ</summary>
    public static class LanguageResources
    {
        private static ResourceDictionary _strings;

        public static FlowDirection Flow => LocalizationService.CurrentLanguage == AppLanguage.Ar ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        public static void Apply(AppLanguage language)
        {
            var app = System.Windows.Application.Current;
            if (app == null) { LocalizationService.Use(language); return; }
            if (!app.Dispatcher.CheckAccess()) { app.Dispatcher.Invoke(() => Apply(language)); return; }

            var strings = new ResourceDictionary();
            foreach (var (key, text) in StringTable.Of(language)) strings[key] = text;

            var dicts = app.Resources.MergedDictionaries;
            if (_strings != null) dicts.Remove(_strings);
            dicts.Add(strings);
            _strings = strings;

            if (app.MainWindow != null)
                app.MainWindow.FlowDirection = language == AppLanguage.Ar ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            LocalizationService.Use(language);
        }

        public static void Toggle() =>
            Apply(LocalizationService.CurrentLanguage == AppLanguage.Ar ? AppLanguage.En : AppLanguage.Ar);
    }
}
