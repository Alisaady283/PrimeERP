using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Admin
{
    /// <summary>طبقة الأعمال فوق ISettingsProvider</summary>
    public class SettingsService : ServiceBase, ISettingsService
    {
        protected override string PermissionPrefix => "Settings";
        protected override string StringPrefix => "Str.Settings";
        protected override string EntityName => "AppSettings";

        private readonly ISettingsProvider _provider;
        private readonly ISettingStore _settings;

        public SettingsService(IPermissionService permissions, ISettingsProvider provider,
                                ILocalizationService localization, IAuditLogger audit, ISettingStore settings)
            : base(permissions, provider, localization, audit)
        {
            _provider = provider;
            _settings = settings;
            _provider.Changed += key => SettingChanged?.Invoke(key);
        }

        public event Action<string> SettingChanged;

        public T Get<T>(string key, T defaultValue = default) => _provider.Get(key, defaultValue);

        public Result Set<T>(string key, T value)
        {
            var denied = CheckWriteAllowed(_settings.GetByKey(key));
            if (denied != null) return denied;

            _provider.SetRaw(key, value);
            Audit.Log(EntityName, 0, AuditAction.Update, newValue: new { key, value = SettingsProvider.FormatValue(value) }, details: key);
            return Result.Ok();
        }

        public Result SetMany(Dictionary<string, object> values)
        {
            if (values == null || values.Count == 0) return Result.Ok();

            var existingRows = values.Keys.ToDictionary(k => k, k => _settings.GetByKey(k));
            var denied = CheckWriteAllowed(existingRows.Values.FirstOrDefault(r => r?.IsSystem == true));
            if (denied != null) return denied;

            _provider.SetManyRaw(values);
            Audit.Log(EntityName, 0, AuditAction.Update, newValue: values, details: Msg("BatchSavedLog", values.Count));
            return Result.Ok();
        }

        public Dictionary<string, string> GetSection(string category) => _provider.GetSection(category);

        public void Reload() => _provider.Reload();

        private Result CheckWriteAllowed(AppSetting existing)
        {
            if (!Can("Edit")) return Fail("PermissionDenied", ErrorCode.Unauthorized);
            if (existing != null && existing.IsSystem && !Can("System")) return Fail("SystemPermissionDenied", ErrorCode.Unauthorized);
            return null;
        }
    }
}
