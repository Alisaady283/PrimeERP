using PrimeERP.Services.Settings;

namespace PrimeERP.Database
{
    /// <summary>يزرع كل مفتاح من SettingKeys.All() بقيمته الافتراضية إن لم يكن موجوداً — لا يستبدل قيمة قائمة.</summary>
    public static class SettingSeeder
    {
        public static void Seed()
        {
            foreach (var def in SettingKeys.All())
                SettingRepository.InsertIfMissing(new SettingRecord
                {
                    Key      = def.Key,
                    Value    = def.DefaultValue,
                    Category = def.Category,
                    DataType = def.DataType,
                    IsSystem = def.IsSystem
                });
        }
    }
}
