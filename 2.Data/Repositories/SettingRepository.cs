using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Data.Repositories
{
    /// <summary>الإعدادات في قاعدةٍ مفتوحة</summary>
    public interface ISettingRepository : ISettingStore
    {
        void UpsertMany(IEnumerable<AppSetting> settings, PrimeDbContext db);
        void RemoveByPrefix(PrimeDbContext db, string prefix);
    }

    /// <summary>مستودع AppSettings</summary>
    public class SettingRepository : RepositoryBase<AppSetting>, ISettingRepository
    {
        protected override string TableName => "AppSettings";

        public List<AppSetting> GetAll() => Fetch(q => q);

        public AppSetting GetByKey(string key) => One(q => q.Where(s => s.Key == key));

        public List<AppSetting> GetByCategory(string category) =>
            Fetch(q => q.Where(s => s.Category == category));

        public bool AnySystem(IEnumerable<string> keys)
        {
            var wanted = keys.ToList();
            return Any(q => q.Where(s => s.IsSystem && wanted.Contains(s.Key)));
        }

        public void Upsert(AppSetting setting) => Write(ctx => Apply(ctx, setting));



        public void UpsertMany(IEnumerable<AppSetting> settings) => UpsertMany(settings, null);

        public void UpsertMany(IEnumerable<AppSetting> settings, PrimeDbContext db) =>
            Write(ctx =>
            {
                foreach (var setting in settings) Apply(ctx, setting);
                return 0;
            }, db);

        public void RemoveByPrefix(PrimeDbContext db, string prefix) =>
            Remove(s => s.Key.StartsWith(prefix), db);

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
