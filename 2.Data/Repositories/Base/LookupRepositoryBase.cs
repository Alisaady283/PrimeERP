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
        Dictionary<int, string> NamesOf(IEnumerable<int> ids, PrimeDbContext db = null);
        List<T> GetAll(bool includeInactive = false);
        int Insert(T row, PrimeDbContext db = null);
        void Update(T row, PrimeDbContext db = null);
        void Delete(int id, PrimeDbContext db = null);
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


        public int Insert(T row, PrimeDbContext db = null) => Add(row, db);

        public void Update(T row, PrimeDbContext db = null) => Modify(row, db);

        public void Delete(int id, PrimeDbContext db = null) =>
            Set(x => EF.Property<int>(x, "Id") == id, s => s.SetProperty(x => EF.Property<bool>(x, "IsActive"), false), db);

    }
}
