using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services
{
    /// <summary>طبقة الأعمال فوق ISettingsProvider: صلاحية قبل الكتابة، تدقيق بعدها، ورسائل مترجمة —
    /// القراءة والـ Cache وبناء السجل الخام كلها في SettingsProvider (Platform)، لا تتكرر هنا.</summary>
    public class SettingsService : ServiceBase, ISettingsService
    {
        protected override string PermissionPrefix => "Settings";
        protected override string StringPrefix => "Str.Settings";
        protected override string EntityName => "AppSettings";

        private readonly ISettingsProvider _provider;

        public SettingsService(IPermissionService permissions, ISettingsProvider provider,
                                ILocalizationService localization, IAuditLogger audit)
            : base(permissions, provider, localization, audit)
        {
            _provider = provider;
            _provider.Changed += key => SettingChanged?.Invoke(key);
        }

        public event Action<string> SettingChanged;

        public T Get<T>(string key, T defaultValue = default) => _provider.Get(key, defaultValue);

        public Result Set<T>(string key, T value)
        {
            var denied = CheckWriteAllowed(SettingRepository.GetByKey(key));
            if (denied != null) return denied;

            _provider.SetRaw(key, value);
            Audit.Log(EntityName, 0, AuditAction.Update, newValue: new { key, value = SettingsProvider.FormatValue(value) }, details: key);
            return Result.Ok();
        }

        public Result SetMany(Dictionary<string, object> values)
        {
            if (values == null || values.Count == 0) return Result.Ok();

            var existingRows = values.Keys.ToDictionary(k => k, SettingRepository.GetByKey);
            var denied = CheckWriteAllowed(existingRows.Values.FirstOrDefault(r => r?.IsSystem == true));
            if (denied != null) return denied;

            _provider.SetManyRaw(values);
            Audit.Log(EntityName, 0, AuditAction.Update, newValue: values, details: $"تعديل {values.Count} إعداد دفعة واحدة");
            return Result.Ok();
        }

        public Dictionary<string, string> GetSection(string category) => _provider.GetSection(category);

        public void Reload() => _provider.Reload();

        private Result CheckWriteAllowed(SettingRecord existing)
        {
            if (!Can("Edit")) return Fail("PermissionDenied");
            if (existing != null && existing.IsSystem && !Can("System")) return Fail("SystemPermissionDenied");
            return null;
        }
    }
}
