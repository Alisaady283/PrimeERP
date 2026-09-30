using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Data.Repositories.Base
{
    /// <summary>أساس حقيقي بالتوريث لكل Repository</summary>
    public abstract class RepositoryBase<T> where T : class
    {
        protected abstract string TableName { get; }

        protected virtual string LineTable => null;
        protected virtual string LineForeignKey => "DocumentId";

        // ── LINQ

        /// <summary>مجموعة الكيان</summary>
        protected DbSet<T> SetOf(PrimeDbContext db) =>
            db.Model.FindEntityType(TableName) != null ? db.Set<T>(TableName) : db.Set<T>();

        protected IQueryable<T> Rows(PrimeDbContext db) => SetOf(db);

        /// <summary>المُعار لا يُهدَم</summary>
        protected static TResult Scope<TResult>(PrimeDbContext db, Func<PrimeDbContext, TResult> work)
        {
            if (db != null) return work(db);

            using var owned = DbContextFactory.Open();
            return work(owned);
        }

        protected List<T> Fetch(Func<IQueryable<T>, IQueryable<T>> shape, PrimeDbContext db = null) =>
            Scope(db, ctx => shape(Rows(ctx).AsNoTracking()).ToList());

        protected T One(Func<IQueryable<T>, IQueryable<T>> shape, PrimeDbContext db = null) =>
            Fetch(q => shape(q).Take(1), db).FirstOrDefault();

        protected int Count(Func<IQueryable<T>, IQueryable<T>> shape, PrimeDbContext db = null) =>
            Scope(db, ctx => shape(Rows(ctx).AsNoTracking()).Count());

        protected bool Any(Func<IQueryable<T>, IQueryable<T>> shape, PrimeDbContext db = null) =>
            Scope(db, ctx => shape(Rows(ctx).AsNoTracking()).Any());

        /// <summary>نفس البنية لكيانٍ آخر</summary>
        protected static DbSet<TOther> SetOf<TOther>(PrimeDbContext db, string table) where TOther : class =>
            db.Model.FindEntityType(table) != null ? db.Set<TOther>(table) : db.Set<TOther>();

        protected static IQueryable<TOther> RowsOf<TOther>(PrimeDbContext db, string table) where TOther : class =>
            SetOf<TOther>(db, table);

        protected static List<TOther> FetchOf<TOther>(string table, Func<IQueryable<TOther>, IQueryable<TOther>> shape,
            PrimeDbContext db = null) where TOther : class =>
            Scope(db, ctx => shape(RowsOf<TOther>(ctx, table).AsNoTracking()).ToList());

        /// <summary>كتابةٌ واحدة</summary>
        protected int Write(Func<PrimeDbContext, int> work, PrimeDbContext db = null) =>
            Scope(db, ctx =>
            {
                var result = work(ctx);
                ctx.SaveChanges();
                ctx.ChangeTracker.Clear();
                return result;
            });

        protected int Add(T entity, PrimeDbContext db = null) =>
            Scope(db, ctx =>
            {
                SetOf(ctx).Add(entity);
                ctx.SaveChanges();
                ctx.ChangeTracker.Clear();
                return (int)(typeof(T).GetProperty("Id")?.GetValue(entity) ?? 0);
            });

        /// <summary>أسماء الفئات بضمّةٍ واحدة</summary>
        protected static List<T> WithCategoryNames(List<T> rows,
            params (Func<T, int?> Id, Action<T, string> Apply)[] links) =>
            WithNames<Category>("Categories", rows, links);

        /// <summary>أسماء جدولٍ آخر بضمّةٍ واحدة</summary>
        protected static List<T> WithNames<TOther>(string table, List<T> rows,
            params (Func<T, int?> Id, Action<T, string> Apply)[] links) where TOther : class
        {
            var names = NamesIn<TOther>(table, links.SelectMany(l => rows.Select(l.Id))
                                                    .Where(id => id != null).Select(id => id.Value));
            if (names.Count == 0) return rows;

            foreach (var row in rows)
                foreach (var (id, apply) in links)
                    if (id(row) is int key && names.TryGetValue(key, out var name)) apply(row, name);

            return rows;
        }

        /// <summary>كود المرجع واسمه بضمّة</summary>
        protected static List<T> WithCodeNames<TOther>(string table, List<T> rows, Func<T, int> id,
            Action<T, string, string> apply) where TOther : class
        {
            var wanted = rows.Select(id).Distinct().ToList();
            if (wanted.Count == 0) return rows;

            var found = Scope(null, db => RowsOf<TOther>(db, table).AsNoTracking()
                .Where(o => wanted.Contains(EF.Property<int>(o, "Id")))
                .Select(o => new { Id = EF.Property<int>(o, "Id"), Code = EF.Property<string>(o, "Code"), Name = EF.Property<string>(o, "Name") })
                .ToDictionary(o => o.Id));

            foreach (var row in rows)
                if (found.TryGetValue(id(row), out var other)) apply(row, other.Code, other.Name);

            return rows;
        }

        /// <summary>أسماء صفوف جدولٍ بمعرّفاتها</summary>
        protected static Dictionary<int, string> NamesIn<TOther>(string table, IEnumerable<int> ids,
            PrimeDbContext db = null) where TOther : class
        {
            var wanted = ids.Distinct().ToList();
            if (wanted.Count == 0) return new Dictionary<int, string>();

            return Scope(db, ctx => RowsOf<TOther>(ctx, table).AsNoTracking()
                .Where(o => wanted.Contains(EF.Property<int>(o, "Id")))
                .Select(o => new { Id = EF.Property<int>(o, "Id"), Name = EF.Property<string>(o, "Name") })
                .ToDictionary(o => o.Id, o => o.Name));
        }

        public Dictionary<int, string> NamesOf(IEnumerable<int> ids, PrimeDbContext db = null) =>
            NamesIn<T>(TableName, ids, db);

        public List<T> GetByIds(IEnumerable<int> ids, PrimeDbContext db = null)
        {
            var wanted = ids.Distinct().ToList();
            return wanted.Count == 0 ? new List<T>() : Fetch(q => q.Where(e => wanted.Contains(EF.Property<int>(e, "Id"))), db);
        }

        /// <summary>صفوفٌ بأكوادها في ضمّةٍ واحدة</summary>
        public Dictionary<string, T> ByCodes(IEnumerable<string> codes, PrimeDbContext db = null)
        {
            var wanted = codes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
            return wanted.Count == 0
                ? new Dictionary<string, T>()
                : Scope(db, ctx => Rows(ctx).AsNoTracking()
                    .Where(e => wanted.Contains(EF.Property<string>(e, "Code")))
                    .Select(e => new { Code = EF.Property<string>(e, "Code"), Row = e })
                    .ToDictionary(x => x.Code, x => x.Row));
        }

        /// <summary>ترتيبٌ بعمودٍ واحد</summary>
        protected static Func<IQueryable<T>, IOrderedQueryable<T>> By<TKey>(Expression<Func<T, TKey>> key, bool descending) =>
            q => descending ? q.OrderByDescending(key) : q.OrderBy(key);

        /// <summary>ترتيب مستند</summary>
        protected static Func<IQueryable<T>, IOrderedQueryable<T>> DocumentOrder<TKey>(
            Expression<Func<T, TKey>> head, bool descending, Expression<Func<T, string>> number) =>
            q => By(head, descending)(q).ThenByDescending(number)
                                        .ThenByDescending(e => EF.Property<DateTime>(e, "CreatedAt"));

        /// <summary>صفحةٌ من جدول</summary>
        protected (List<T> Items, int Total) Page(int page, int pageSize,
            Func<IQueryable<T>, IQueryable<T>> filter, Func<IQueryable<T>, IOrderedQueryable<T>> order,
            PrimeDbContext db = null) =>
            (Fetch(q => order(filter(q)).ThenByDescending(e => EF.Property<int>(e, "Id"))
                                        .Skip(Math.Max(0, page - 1) * pageSize).Take(pageSize), db),
             Count(filter, db));

        protected void Modify(T entity, PrimeDbContext db = null) =>
            Write(db =>
            {
                SetOf(db).Update(entity);
                return 0;
            }, db);

        /// <summary>تعديل صفٍّ قائم</summary>
        protected int Edit(Expression<Func<T, bool>> match, Action<T> apply,
            PrimeDbContext db = null) =>
            Write(db =>
            {
                var row = SetOf(db).AsTracking().FirstOrDefault(match);
                if (row == null) return 0;
                apply(row);
                return 1;
            }, db);

        /// <summary>تعديلٌ بجملةٍ واحدة</summary>
        protected int Set(Expression<Func<T, bool>> match, Action<UpdateSettersBuilder<T>> setters, PrimeDbContext db = null) =>
            SetIn(TableName, match, setters, db);

        protected static int SetIn<TOther>(string table, Expression<Func<TOther, bool>> match,
            Action<UpdateSettersBuilder<TOther>> setters, PrimeDbContext db = null) where TOther : class =>
            Scope(db, ctx =>
            {
                var entity = ctx.Model.FindEntityType(table) ?? ctx.Model.FindEntityType(typeof(TOther));
                var stamped = entity?.FindProperty("UpdatedAt")?.ClrType == typeof(DateTime);
                return RowsOf<TOther>(ctx, table).Where(match).ExecuteUpdate(s =>
                {
                    setters(s);
                    if (stamped) s.SetProperty(e => EF.Property<DateTime>(e, "UpdatedAt"), DateTime.Now);
                });
            });

        /// <summary>حذفٌ بجملةٍ واحدة</summary>
        protected int Remove(Expression<Func<T, bool>> match, PrimeDbContext db = null) =>
            RemoveIn(TableName, match, db);

        protected static int RemoveIn<TOther>(string table, Expression<Func<TOther, bool>> match,
            PrimeDbContext db = null) where TOther : class =>
            Scope(db, ctx => RowsOf<TOther>(ctx, table).Where(match).ExecuteDelete());

        protected void SoftDelete(int id, string deletedBy, PrimeDbContext db = null) =>
            SoftDeleteIn<T>(TableName, id, deletedBy, db);

        protected static void SoftDeleteIn<TOther>(string table, int id, string deletedBy, PrimeDbContext db = null)
            where TOther : class =>
            SetIn<TOther>(table, e => EF.Property<int>(e, "Id") == id, s => s
                .SetProperty(e => EF.Property<bool>(e, "IsDeleted"), true)
                .SetProperty(e => EF.Property<DateTime?>(e, "DeletedAt"), DateTime.Now)
                .SetProperty(e => EF.Property<string>(e, "DeletedBy"), deletedBy ?? ""), db);


        public virtual List<T> GetAll(PrimeDbContext db = null) =>
            Fetch(q => q, db);

        public virtual T GetById(int id, PrimeDbContext db = null) =>
            One(q => q.Where(e => EF.Property<int>(e, "Id") == id), db);
    }
}
