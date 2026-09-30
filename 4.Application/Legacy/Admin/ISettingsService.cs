using System;
using System.Collections.Generic;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Legacy.Admin
{
    /// <summary>عقد الإعدادات</summary>
    public interface ISettingsService
    {
        T Get<T>(string key, T defaultValue = default);
        Result Set<T>(string key, T value);

        Result SetMany(Dictionary<string, object> values);

        Dictionary<string, string> GetSection(string category);

        void Reload();

        event Action<string> SettingChanged;
    }
}
