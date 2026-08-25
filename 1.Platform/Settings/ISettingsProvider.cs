using System;
using System.Collections.Generic;

namespace PrimeERP.Platform.Settings
{
    /// <summary>عملية تقنية بحتة: قراءة/كتابة خام لجدول الإعدادات + Cache — بلا صلاحية، بلا audit، بلا رسائل
    /// مترجمة. تستهلكه الطبقات الدنيا (Platform/Data/Domain) مباشرة، وSettingsService (Application) فوقه
    /// يضيف طبقة الأعمال (صلاحية + معاملة + audit). راجع ARCHITECTURE.md — "مزوّد مقابل خدمة".</summary>
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
