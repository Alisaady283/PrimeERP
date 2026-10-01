using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Platform.Settings
{
    /// <summary>قراءة الإعدادات وكتابتها بذاكرة مؤقتة</summary>
    public class SettingsProvider : ISettingsProvider
    {
        private readonly ISettingStore _store;
        private readonly object _lock = new();
        private Dictionary<string, string> _cache;

        public SettingsProvider(ISettingStore store) => _store = store;

        public event Action<string> Changed;

        public T Get<T>(string key, T defaultValue = default)
        {
            EnsureCacheLoaded();
            if (!_cache.TryGetValue(key, out var raw) || string.IsNullOrEmpty(raw)) return defaultValue;

            try
            {
                if (typeof(T) == typeof(bool)) return (T)(object)(raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase));
                if (typeof(T).IsEnum) return (T)Enum.Parse(typeof(T), raw, ignoreCase: true);
                return (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);
            }
            catch { return defaultValue; }
        }

        public void SetRaw<T>(string key, T value)
        {
            _store.Upsert(BuildRecord(key, value));
            Reload();
            Changed?.Invoke(key);
        }

        public void SetManyRaw(Dictionary<string, object> values)
        {
            _store.UpsertMany(values.Select(v => BuildRecord(v.Key, v.Value)).ToList());

            Reload();
            foreach (var key in values.Keys) Changed?.Invoke(key);
        }

        public Dictionary<string, string> GetSection(string category) =>
            _store.GetByCategory(category).ToDictionary(s => s.Key, s => s.Value);

        public void Reload() { lock (_lock) _cache = null; }

        private void EnsureCacheLoaded()
        {
            if (_cache != null) return;
            lock (_lock)
            {
                if (_cache != null) return;
                _cache = _store.GetAll().ToDictionary(s => s.Key, s => s.Value);
            }
        }

        /// <summary>القائم تُكتب قيمته وحدها</summary>
        public static AppSetting BuildRecord<T>(string key, T value) => new()
        {
            Key        = key,
            Value      = FormatValue(value),
            DataType   = typeof(T).Name,
            ModifiedBy = AppSession.Username
        };

        public static string FormatValue<T>(T value) =>
            value switch
            {
                null => "",
                bool b => b ? "true" : "false",
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
            };
    }
}
