using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using PrimeERP.Data.Query;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using System.Dynamic;

namespace PrimeERP.Data.Repositories
{
    /// <summary>
    /// مستودع أي جدول بناه المستخدم — نفس شكل مستودعات النظام (WarehouseRepository مثالاً)، والفرق أن
    /// الجدول وأعمدته من الوصف لا مكتوبة، والصفّ قاموسٌ لا كيان. العمود المحسوب استعلامٌ فرعي يُقرأ
    /// ولا يُكتب، فرصيدُ الطرف يُحسب عند القراءة كما يفعل النظام اليوم.
    /// </summary>
    public class DynamicRepository : RepositoryBase<IDictionary<string, object>>
    {
        private readonly string _table;
        private readonly List<BuilderColumn> _columns;

        public DynamicRepository(string table, List<BuilderColumn> columns)
        {
            _table = table;
            _columns = columns;
        }

        protected override string TableName => _table;

        private IEnumerable<BuilderColumn> Stored => _columns.Where(c => c.Aggregate == BuilderAggregate.None);
        private IEnumerable<BuilderColumn> Computed => _columns.Where(c => c.Aggregate != BuilderAggregate.None);

        /// <summary>الأعمدة المخزَّنة كما هي، والمحسوبة استعلاماً فرعياً بجانبها.</summary>
        private string Select()
        {
            var parts = new List<string> { "t.*" };

            foreach (var c in Computed)
                parts.Add($"(SELECT COALESCE({c.Aggregate.ToString().ToUpperInvariant()}({c.AggColumn}), 0) " +
                          $"FROM {c.AggFrom} WHERE {c.AggMatch} = t.Id) AS {c.Name}");

            return string.Join(", ", parts);
        }

        public (List<IDictionary<string, object>> Items, int Total) GetPaged(int page, int pageSize, string searchText,
            string sortColumn, bool sortDescending)
        {
            var searchable = Stored.Where(c => c.DataType == BuilderDataType.Text).Select(c => "t." + c.Name).ToArray();
            var where = new WhereBuilder().Eq("t.IsDeleted", false).LikeAny(searchText, searchable);

            var total = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM {_table} t {where.Sql}", where.Parameters));

            var column = Stored.Any(c => c.Name == sortColumn) ? "t." + sortColumn : DefaultSort();
            var sql = $@"SELECT {Select()} FROM {_table} t {where.Sql}
                         {OrderBuilder.By(column, sortDescending, "t.Id")}
                         {Core.DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";

            return (Query(sql, null, null, where.Parameters), total);
        }

        /// <summary>يتبع قاعدة الترتيب: المؤرَّخ بتاريخه، وغيره بأول عمود نصّي.</summary>
        private string DefaultSort()
        {
            var date = Stored.FirstOrDefault(c => c.DataType == BuilderDataType.Date);
            return "t." + (date?.Name ?? Stored.FirstOrDefault()?.Name ?? "Id");
        }

        public IDictionary<string, object> GetById(int id) =>
            QueryOne($"SELECT {Select()} FROM {_table} t WHERE t.Id = @id", null, null, ("@id", id));

        public int Insert(IDictionary<string, object> values, string user)
        {
            var names = Stored.Select(c => c.Name).ToList();
            var sql = $"INSERT INTO {_table} ({string.Join(", ", names)}, CreatedAt, CreatedBy) " +
                      $"VALUES ({string.Join(", ", names.Select(n => "@" + n))}, @now, @by)";

            return InsertGetId(sql, null, null, Params(values, names)
                .Append(("@now", (object)DateTime.Now)).Append(("@by", (object)(user ?? ""))).ToArray());
        }

        public void Update(int id, IDictionary<string, object> values)
        {
            var names = Stored.Select(c => c.Name).ToList();
            var sql = $"UPDATE {_table} SET {string.Join(", ", names.Select(n => $"{n} = @{n}"))}, UpdatedAt = @now WHERE Id = @id";

            Exec(sql, null, null, Params(values, names)
                .Append(("@now", (object)DateTime.Now)).Append(("@id", (object)id)).ToArray());
        }

        // الحذف ناعم كما في كل جداول النظام — البيانات تبقى والسجل يُخفى.
        public void Delete(int id) =>
            Exec($"UPDATE {_table} SET IsDeleted = @d, DeletedAt = @now WHERE Id = @id",
                null, null, ("@d", true), ("@now", DateTime.Now), ("@id", id));

        public bool Exists(string column, object value, int exceptId) =>
            Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM {_table} WHERE {column} = @v AND Id <> @id AND IsDeleted = @d",
                ("@v", value ?? DBNull.Value), ("@id", exceptId), ("@d", false))) > 0;

        private static IEnumerable<(string, object)> Params(IDictionary<string, object> values, List<string> names) =>
            names.Select(n => ("@" + n, values.TryGetValue(n, out var v) && v != null ? v : DBNull.Value));

        /// <summary>
        /// الصفّ ExpandoObject لا Dictionary: WPF يربط الأعمدة بأسماء الخصائص، والقاموس العادي بلا خصائص
        /// فتظهر الخلايا فارغة. وExpandoObject قاموسٌ أيضاً، فيقرؤه بقية الكود كما هو.
        /// </summary>
        protected override IDictionary<string, object> Map(DataRow row)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (DataColumn column in row.Table.Columns)
                expando[column.ColumnName] = row[column] == DBNull.Value ? null : row[column];

            return expando;
        }
    }
}
