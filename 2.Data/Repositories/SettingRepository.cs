using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع AppSettings</summary>
    public class SettingRepository : RepositoryBase<AppSetting>, ISettingStore
    {
        protected override string TableName => "AppSettings";

        public List<AppSetting> GetAll() => Fetch(q => q);

        public AppSetting GetByKey(string key) => One(q => q.Where(s => s.Key == key));

        public List<AppSetting> GetByCategory(string category) =>
            Fetch(q => q.Where(s => s.Category == category));

        public void Upsert(AppSetting setting) => Write(ctx => Apply(ctx, setting));



        public void UpsertMany(IEnumerable<AppSetting> settings) =>
            Write(db =>
            {
                foreach (var setting in settings) Apply(db, setting);
                return 0;
            });

        public void InsertIfMissing(AppSetting setting) =>
            Write(db =>
            {
                if (SetOf(db).Any(s => s.Key == setting.Key)) return 0;
                SetOf(db).Add(setting);
                return 1;
            });


        /// <summary>القائم تُحدَّث قيمته وحدها</summary>
        private int Apply(PrimeDbContext db, AppSetting setting)
        {
            var row = SetOf(db).FirstOrDefault(s => s.Key == setting.Key);
            if (row == null)
            {
                setting.ModifiedAt = DateTime.Now;
                SetOf(db).Add(setting);
                return 1;
            }

            row.Value      = setting.Value;
            row.ModifiedAt = DateTime.Now;
            row.ModifiedBy = setting.ModifiedBy;
            return 0;
        }
    }
}
