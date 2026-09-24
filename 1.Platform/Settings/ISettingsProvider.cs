using System;
using System.Collections.Generic;

namespace PrimeERP.Platform.Settings
{
    /// <summary>عقد قراءة وكتابة الإعدادات</summary>
    public interface ISettingsProvider
    {
        T Get<T>(string key, T defaultValue = default);
        void SetRaw<T>(string key, T value);
        void SetManyRaw(Dictionary<string, object> values);
        Dictionary<string, string> GetSection(string category);
        void Reload();
        event Action<string> Changed;
    }
}
