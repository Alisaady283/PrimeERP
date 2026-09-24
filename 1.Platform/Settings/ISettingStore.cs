using System.Collections.Generic;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Platform.Settings
{
    /// <summary>مخزن جدول AppSettings</summary>
    public interface ISettingStore
    {
        List<AppSetting> GetAll();
        AppSetting GetByKey(string key);
        List<AppSetting> GetByCategory(string category);

        void Upsert(AppSetting setting);
        void UpsertMany(IEnumerable<AppSetting> settings);
        void InsertIfMissing(AppSetting setting);

    }
}
