using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json;

namespace PrimeERP.Platform.Localization
{
    /// <summary>نصوص كل لغة من ملفها Strings/{lang}.json المضمَّن — المصدر الواحد لسطح المكتب والخادم والويب</summary>
    public static class StringTable
    {
        private static readonly ConcurrentDictionary<AppLanguage, FrozenDictionary<string, string>> Tables = new();

        public static FrozenDictionary<string, string> Of(AppLanguage language) => Tables.GetOrAdd(language, Load);

        private static FrozenDictionary<string, string> Load(AppLanguage language)
        {
            using var stream = typeof(StringTable).Assembly.GetManifestResourceStream($"Strings.{language.ToString().ToLowerInvariant()}.json");
            return JsonSerializer.Deserialize<Dictionary<string, string>>(stream).ToFrozenDictionary();
        }
    }
}
