using System.Linq;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Data.Seeders
{
    /// <summary>زرع المفاتيح الافتراضية بلا استبدال</summary>
    public static class SettingSeeder
    {
        public static void Seed(ISettingStore settings)
        {
            settings.InsertMissing(SettingKeys.All().Select(def => new AppSetting
            {
                Key      = def.Key,
                Value    = def.DefaultValue,
                Category = def.Category,
                DataType = def.DataType,
                IsSystem = def.IsSystem
            }));
        }
    }
}
