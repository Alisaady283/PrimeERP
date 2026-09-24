using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع أي جدول بناه المستخدم</summary>
    public class DynamicRepository
    {
        private readonly string _table;
        private readonly List<BuilderColumn> _columns;

        public DynamicRepository(string table, List<BuilderColumn> columns)
        {
            _table = table;
            _columns = columns;
            BuiltTables.Declare(table, columns);
        }

        private IEnumerable<BuilderColumn> Stored => _columns.Where(c => c.Aggregate == BuilderAggregate.None);
        private IEnumerable<BuilderColumn> Computed => _columns.Where(c => c.Aggregate != BuilderAggregate.None);

        private List<IDictionary<string, object>> Read(
            Func<IQueryable<Dictionary<string, object>>, IQueryable<Dictionary<string, object>>> shape)
        {
            using var db = DbContextFactory.Open();
            var rows = shape(db.Rows(_table).AsNoTracking()).AsEnumerable()
                .Select(r => (IDictionary<string, object>)new Dictionary<string, object>(r))
                .ToList();

            Compute(db, rows);
            return rows;
        }

        /// <summary>الأعمدة التجميعية تُحسب من جدول</summary>
        private void Compute(PrimeDbContext db, List<IDictionary<string, object>> rows)
        {
            foreach (var c in Computed)
            {
                if (string.IsNullOrWhiteSpace(c.AggFrom) || string.IsNullOrWhiteSpace(c.AggMatch)) continue;

                var lines = db.Rows(c.AggFrom).AsNoTracking().AsEnumerable().ToList();
                foreach (var row in rows)
                {
                    var own = lines.Where(l => Equals(Number(l[c.AggMatch]), Number(row["Id"])));
                    row[c.Name] = c.Aggregate switch
                    {
                        BuilderAggregate.Count => own.Count(),
                        BuilderAggregate.Sum   => own.Sum(l => Number(l[c.AggColumn])),
                        BuilderAggregate.Avg   => own.Any() ? own.Average(l => Number(l[c.AggColumn])) : 0m,
                        BuilderAggregate.Min   => own.Any() ? own.Min(l => Number(l[c.AggColumn])) : 0m,
                        BuilderAggregate.Max   => own.Any() ? own.Max(l => Number(l[c.AggColumn])) : 0m,
                        _                      => 0m,
                    };
                }
            }
        }

        private static decimal Number(object value) => value == null ? 0m : Convert.ToDecimal(value);

        public (List<IDictionary<string, object>> Items, int Total) GetPaged(int page, int pageSize, string searchText,
            string sortColumn, bool sortDescending)
        {
            var searchable = Stored.Where(c => c.DataType == BuilderDataType.Text).Select(c => c.Name).ToList();
            var column = Stored.Any(c => c.Name == sortColumn) ? sortColumn : DefaultSort();

            IQueryable<Dictionary<string, object>> Shape(IQueryable<Dictionary<string, object>> rows)
            {
                var q = rows.Where(r => !(bool)r["IsDeleted"]);
                if (!string.IsNullOrWhiteSpace(searchText) && searchable.Count > 0)
                    q = q.Where(r => searchable.Any(name => EF.Functions.Like((string)r[name], $"%{searchText}%")));
                return q;
            }

            using var db = DbContextFactory.Open();
            var total = Shape(db.Rows(_table).AsNoTracking()).Count();

            var items = Read(q =>
            {
                var shaped = Shape(q);
                var ordered = sortDescending
                    ? shaped.OrderByDescending(r => r[column])
                    : shaped.OrderBy(r => r[column]);
                return ordered.ThenByDescending(r => r["Id"]).Skip(Math.Max(0, page - 1) * pageSize).Take(pageSize);
            });

            return (items, total);
        }

        private string DefaultSort()
        {
            var date = Stored.FirstOrDefault(c => c.DataType == BuilderDataType.Date);
            return date?.Name ?? Stored.FirstOrDefault()?.Name ?? "Id";
        }

        public IDictionary<string, object> GetById(int id) =>
            Read(q => q.Where(r => (int)r["Id"] == id).Take(1)).FirstOrDefault();

        public int Insert(IDictionary<string, object> values, string user)
        {
            using var db = DbContextFactory.Open();
            var row = Blank();
            foreach (var c in Stored) row[c.Name] = values.TryGetValue(c.Name, out var v) ? v : null;
            row["CreatedAt"] = DateTime.Now;
            row["CreatedBy"] = user ?? "";

            db.BuiltSet(_table).Add(row);
            db.SaveChanges();
            return Convert.ToInt32(row["Id"]);
        }

        public void Update(int id, IDictionary<string, object> values)
        {
            using var db = DbContextFactory.Open();
            var row = db.BuiltSet(_table).FirstOrDefault(r => (int)r["Id"] == id);
            if (row == null) return;

            foreach (var c in Stored)
                if (values.TryGetValue(c.Name, out var v)) row[c.Name] = v;

            row["UpdatedAt"] = DateTime.Now;
            db.SaveChanges();
        }

        public void Delete(int id)
        {
            using var db = DbContextFactory.Open();
            var row = db.BuiltSet(_table).FirstOrDefault(r => (int)r["Id"] == id);
            if (row == null) return;

            row["IsDeleted"] = true;
            row["DeletedAt"] = DateTime.Now;
            db.SaveChanges();
        }

        public bool Exists(string column, object value, int exceptId)
        {
            using var db = DbContextFactory.Open();
            return db.Rows(_table).AsNoTracking()
                .Any(r => r[column].Equals(value) && (int)r["Id"] != exceptId && !(bool)r["IsDeleted"]);
        }

        private Dictionary<string, object> Blank()
        {
            var row = new Dictionary<string, object>();
            foreach (var c in Stored) row[c.Name] = null;
            row["IsDeleted"] = false;
            return row;
        }
    }
}
