using PrimeERP.Data.Core;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace PrimeERP.Data.Repositories.Base
{
    /// <summary>ما يتقاسمه مستودعات القوائم</summary>
    public interface ILookupRepository<T>
    {
        T GetById(int id, PrimeDbContext db = null);
        List<T> GetAll(bool includeInactive = false);
        int Insert(T row);
        void Update(T row);
        void Delete(int id);
    }

    /// <summary>قائمةٌ بجدولها</summary>
    public class LookupRepository<T> : LookupRepositoryBase<T>, ILookupRepository<T> where T : class
    {
        private readonly string _table;

        public LookupRepository(string table) => _table = table;

        protected override string TableName => _table;
    }

    public abstract class LookupRepositoryBase<T> : RepositoryBase<T> where T : class
    {
        public List<T> GetAll(bool includeInactive = false) =>
            Fetch(q => (includeInactive ? q : q.Where(x => EF.Property<bool>(x, "IsActive")))
                       .OrderBy(x => EF.Property<string>(x, "Name")));


        public int Insert(T row) => Add(row);

        public void Update(T row) => Write(db => { SetOf(db).Update(row); return 0; });

        public void Delete(int id) =>
            Write(db =>
            {
                var row = Rows(db).AsTracking().FirstOrDefault(x => EF.Property<int>(x, "Id") == id);
                if (row != null) db.Entry(row).Property("IsActive").CurrentValue = false;
                return 0;
            });

    }
}
