using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
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

        private static readonly ConcurrentDictionary<string, bool> SoftDeletable = new();

        /// <summary>يستبعد المحذوف منطقياً حيث يعلن</summary>
        protected IQueryable<T> Live(IQueryable<T> rows) =>
            SoftDeletable.GetOrAdd(TableName, table =>
            {
                using var db = DbContextFactory.Open();
                var entity = db.Model.FindEntityType(table) ?? db.Model.FindEntityType(typeof(T));
                return entity?.FindProperty("IsDeleted") != null;
            })
                ? rows.Where(e => !EF.Property<bool>(e, "IsDeleted"))
                : rows;

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
                return result;
            });

        protected int Add(T entity, PrimeDbContext db = null) =>
            Scope(db, ctx =>
            {
                SetOf(ctx).Add(entity);
                Stamp(ctx, entity);
                ctx.SaveChanges();
                return (int)(typeof(T).GetProperty("Id")?.GetValue(entity) ?? 0);
            });

        /// <summary>أسماء الفئات بضمّةٍ واحدة</summary>
        protected static List<T> WithCategoryNames(List<T> rows,
            params (Func<T, int?> Id, Action<T, string> Apply)[] links)
        {
            var ids = links.SelectMany(l => rows.Select(l.Id))
                           .Where(id => id != null).Select(id => id.Value).Distinct().ToList();
            if (ids.Count == 0) return rows;

            var names = FetchOf<Category>("Categories", q => q.Where(c => ids.Contains(c.Id)))
                            .ToDictionary(c => c.Id, c => c.Name);

            foreach (var row in rows)
                foreach (var (id, apply) in links)
                    if (id(row) is int key && names.TryGetValue(key, out var name)) apply(row, name);

            return rows;
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
                if (entity is BaseModel row) row.UpdatedAt = DateTime.Now;
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
                if (row is BaseModel stamped) stamped.UpdatedAt = DateTime.Now;
                return 1;
            }, db);

        protected void SoftDelete(int id, string deletedBy, PrimeDbContext db = null) =>
            Write(db =>
            {
                if (SetOf(db).FirstOrDefault(e => EF.Property<int>(e, "Id") == id) is not BaseModel row) return 0;
                row.IsDeleted = true;
                row.DeletedAt = DateTime.Now;
                row.DeletedBy = deletedBy ?? "";
                return 0;
            }, db);

        /// <summary>وقت الإنشاء يُختم من النموذج</summary>
        private static void Stamp(PrimeDbContext db, T entity)
        {
            var entry = db.Entry(entity);

            foreach (var name in new[] { "CreatedAt", "UpdatedAt" })
                if (entry.Metadata.FindProperty(name) != null && Equals(entry.Property(name).CurrentValue, default(DateTime)))
                    entry.Property(name).CurrentValue = DateTime.Now;

            if (entry.Metadata.FindProperty("CreatedBy") != null && entry.Property("CreatedBy").CurrentValue == null)
                entry.Property("CreatedBy").CurrentValue = AppSession.Username ?? "";
        }

        public virtual List<T> GetAll(PrimeDbContext db = null) =>
            Fetch(q => q, db);

        public virtual T GetById(int id, PrimeDbContext db = null) =>
            One(q => Live(q).Where(e => EF.Property<int>(e, "Id") == id), db);
    }
}
