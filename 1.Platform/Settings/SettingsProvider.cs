using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PrimeERP.Platform.Permissions;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Platform.Settings
{
    public class SettingsProvider : ISettingsProvider
    {
        private readonly object _lock = new();
        private Dictionary<string, string> _cache;

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
            var existing = SettingRepository.GetByKey(key);
            SettingRepository.Upsert(BuildRecord(key, value, existing));
            Reload();
            Changed?.Invoke(key);
        }

        public void SetManyRaw(Dictionary<string, object> values)
        {
            var existingRows = values.Keys.ToDictionary(k => k, SettingRepository.GetByKey);

            Db.RunTransaction((conn, tx) =>
            {
                foreach (var (key, value) in values)
                    SettingRepository.Upsert(conn, tx, BuildRecord(key, value, existingRows[key]));
            });

            Reload();
            foreach (var key in values.Keys) Changed?.Invoke(key);
        }

        public Dictionary<string, string> GetSection(string category) =>
            SettingRepository.GetByCategory(category).ToDictionary(s => s.Key, s => s.Value);

        public void Reload() { lock (_lock) _cache = null; }

        private void EnsureCacheLoaded()
        {
            if (_cache != null) return;
            lock (_lock)
            {
                if (_cache != null) return;
                _cache = SettingRepository.GetAll().ToDictionary(s => s.Key, s => s.Value);
            }
        }

        public static SettingRecord BuildRecord<T>(string key, T value, SettingRecord existing) => new()
        {
            Key           = key,
            Value         = FormatValue(value),
            Category      = existing?.Category,
            DataType      = existing?.DataType ?? typeof(T).Name,
            DisplayNameAr = existing?.DisplayNameAr,
            DisplayNameEn = existing?.DisplayNameEn,
            IsSystem      = existing?.IsSystem ?? false,
            ModifiedBy    = AppSession.Username
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
