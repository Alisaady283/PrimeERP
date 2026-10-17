using System;
using System.Threading;

namespace PrimeERP.Platform.Localization
{
    /// <summary>اللغة الحالية ونصوصها من StringTable: مشتركةٌ في سطح المكتب، ولكل طلبٍ لغته على الخادم (Enter)</summary>
    public enum AppLanguage
    {
        Ar,
        En
    }

    public static class LocalizationService
    {
        private static AppLanguage _shared = AppLanguage.Ar;
        private static readonly AsyncLocal<AppLanguage?> Scoped = new();

        public static AppLanguage CurrentLanguage => Scoped.Value ?? _shared;

        public static event EventHandler LanguageChanged;

        public static string Pick(string arabic, string english, string fallback = null) =>
            CurrentLanguage == AppLanguage.En
                ? (!string.IsNullOrWhiteSpace(english) ? english : !string.IsNullOrWhiteSpace(fallback) ? fallback : arabic)
                : (!string.IsNullOrWhiteSpace(arabic) ? arabic : fallback);

        public static void Use(AppLanguage language)
        {
            _shared = language;
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        public static IDisposable Enter(AppLanguage language)
        {
            var previous = Scoped.Value;
            Scoped.Value = language;
            return new ScopeExit(() => Scoped.Value = previous);
        }

        public static string Get(string key, params object[] args) =>
            args == null || args.Length == 0 ? Get(key) : string.Format(Get(key), args);

        /// <summary>النص أو بديله</summary>
        public static string GetOr(string key, string fallback) =>
            key != null && StringTable.Of(CurrentLanguage).TryGetValue(key, out var text) ? text : fallback;

        public static string Get(string key) =>
            key != null && StringTable.Of(CurrentLanguage).TryGetValue(key, out var text) ? text : key;
    }
}
