using PrimeERP.Platform.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Settings;
using Auditor = PrimeERP.Platform.Audit.AuditLogger;
using AuditAction = PrimeERP.Domain.Enums.AuditAction;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Platform.Settings
{
    /// <summary>
    /// المكان الوحيد لقراءة/تعديل إعدادات النظام — بديل Core/Settings/AppSettings.cs (المحذوف). يمر بـ
    /// SettingRepository فقط (لا SQL مباشر)، يتحقق من الصلاحية قبل أي كتابة، ويسجّل كل كتابة في AuditLogger.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        public static readonly SettingsService Instance = new();

        private readonly IPermissionService _permissions = PermissionService.Instance;
        private readonly object _lock = new();
        private Dictionary<string, string> _cache;

        public event Action<string> SettingChanged;

        public T Get<T>(string key, T defaultValue = default)
        {
            EnsureCacheLoaded();

            if (!_cache.TryGetValue(key, out var raw) || string.IsNullOrEmpty(raw))
                return defaultValue;

            try
            {
                if (typeof(T) == typeof(bool))
                    return (T)(object)(raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase));
                if (typeof(T).IsEnum)
                    return (T)Enum.Parse(typeof(T), raw, ignoreCase: true);

                return (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue;
            }
        }

        public Result Set<T>(string key, T value)
        {
            if (!_permissions.Can(PermissionKeys.Settings.Edit))
                return Result.Fail(LocalizationService.Get("Str.Settings.PermissionDenied"), ErrorCode.Unauthorized);

            var existing = SettingRepository.GetByKey(key);
            if (existing != null && existing.IsSystem && !_permissions.Can(PermissionKeys.Settings.System))
                return Result.Fail(LocalizationService.Get("Str.Settings.SystemPermissionDenied"), ErrorCode.Unauthorized);

            var stringValue = FormatValue(value);

            SettingRepository.Upsert(new SettingRecord
            {
                Key           = key,
                Value         = stringValue,
                Category      = existing?.Category,
                DataType      = existing?.DataType ?? typeof(T).Name,
                DisplayNameAr = existing?.DisplayNameAr,
                DisplayNameEn = existing?.DisplayNameEn,
                IsSystem      = existing?.IsSystem ?? false,
                ModifiedBy    = AppSession.Username
            });

            Auditor.Log("AppSettings", 0, AuditAction.Update, newValue: new { key, value = stringValue }, details: key);

            Reload();
            SettingChanged?.Invoke(key);
            return Result.Ok();
        }

        public Result SetMany(Dictionary<string, object> values)
        {
            if (values == null || values.Count == 0) return Result.Ok();

            if (!_permissions.Can(PermissionKeys.Settings.Edit))
                return Result.Fail(LocalizationService.Get("Str.Settings.PermissionDenied"), ErrorCode.Unauthorized);

            var existingRows = values.Keys.ToDictionary(k => k, k => SettingRepository.GetByKey(k));
            var touchesSystemKey = existingRows.Values.Any(r => r?.IsSystem == true);
            if (touchesSystemKey && !_permissions.Can(PermissionKeys.Settings.System))
                return Result.Fail(LocalizationService.Get("Str.Settings.SystemPermissionDenied"), ErrorCode.Unauthorized);

            Db.RunTransaction((conn, tx) =>
            {
                foreach (var (key, value) in values)
                {
                    var existing = existingRows[key];
                    SettingRepository.Upsert(conn, tx, new SettingRecord
                    {
                        Key           = key,
                        Value         = FormatValue(value),
                        Category      = existing?.Category,
                        DataType      = existing?.DataType ?? value?.GetType().Name,
                        DisplayNameAr = existing?.DisplayNameAr,
                        DisplayNameEn = existing?.DisplayNameEn,
                        IsSystem      = existing?.IsSystem ?? false,
                        ModifiedBy    = AppSession.Username
                    });
                }
            });

            Auditor.Log("AppSettings", 0, AuditAction.Update, newValue: values, details: $"تعديل {values.Count} إعداد دفعة واحدة");

            Reload();
            foreach (var key in values.Keys)
                SettingChanged?.Invoke(key);

            return Result.Ok();
        }

        public Dictionary<string, string> GetSection(string category) =>
            SettingRepository.GetByCategory(category).ToDictionary(s => s.Key, s => s.Value);

        public void Reload()
        {
            lock (_lock) _cache = null;
        }

        private void EnsureCacheLoaded()
        {
            if (_cache != null) return;

            lock (_lock)
            {
                if (_cache != null) return;
                _cache = SettingRepository.GetAll().ToDictionary(s => s.Key, s => s.Value);
            }
        }

        private static string FormatValue<T>(T value) =>
            value switch
            {
                null => "",
                bool b => b ? "true" : "false",
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
            };
    }
}
