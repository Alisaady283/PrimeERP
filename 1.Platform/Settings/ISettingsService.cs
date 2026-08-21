using System;
using System.Collections.Generic;
using PrimeERP.Domain.Results;

namespace PrimeERP.Platform.Settings
{
    public interface ISettingsService
    {
        T Get<T>(string key, T defaultValue = default);
        Result Set<T>(string key, T value);

        /// <summary>يحفظ عدة إعدادات معاً داخل معاملة واحدة.</summary>
        Result SetMany(Dictionary<string, object> values);

        Dictionary<string, string> GetSection(string category);

        /// <summary>يُبطل الـ Cache — يُعاد التحميل من قاعدة البيانات عند أول قراءة تالية.</summary>
        void Reload();

        event Action<string> SettingChanged;
    }
}
