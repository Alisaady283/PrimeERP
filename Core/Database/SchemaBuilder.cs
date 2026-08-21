using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PrimeERP.Core.Database
{
    /// <summary>يبني جداول بنفس الكود على أي محرك — يترجم كل عمود عبر IDbProvider الحالي.</summary>
    public class SchemaBuilder
    {
        private readonly string _tableName;
        private readonly IDbProvider _provider;
        private readonly List<string> _columns = new();
        private readonly List<string> _constraints = new();
        private readonly List<(string Column, bool Unique)> _indexes = new();

        private SchemaBuilder(string tableName)
        {
            _tableName = tableName;
            _provider  = DbFactory.Current;
        }

        public static SchemaBuilder Table(string name) => new SchemaBuilder(name);

        public SchemaBuilder Id(string name = "Id")
        {
            _columns.Add($"{_provider.QuoteIdentifier(name)} {_provider.AutoIncrementPk}");
            return this;
        }

        public SchemaBuilder Text(string name, int? length = null, bool required = false, bool unique = false)
        {
            var col = $"{_provider.QuoteIdentifier(name)} {_provider.TextType(length)}";
            if (required) col += " NOT NULL";
            _columns.Add(col);
            if (unique) _indexes.Add((name, true));
            return this;
        }

        public SchemaBuilder Int(string name, bool nullable = true, int? defaultValue = null)
        {
            var col = $"{_provider.QuoteIdentifier(name)} {_provider.IntType}";
            if (!nullable) col += " NOT NULL";
            if (defaultValue.HasValue) col += $" DEFAULT {defaultValue.Value}";
            _columns.Add(col);
            return this;
        }

        public SchemaBuilder Decimal(string name, int precision = 18, int scale = 4, decimal defaultValue = 0)
        {
            var def = defaultValue.ToString(CultureInfo.InvariantCulture);
            _columns.Add($"{_provider.QuoteIdentifier(name)} {_provider.DecimalType(precision, scale)} NOT NULL DEFAULT {def}");
            return this;
        }

        public SchemaBuilder Bool(string name, bool defaultValue = false)
        {
            var def = defaultValue ? _provider.BoolTrue : _provider.BoolFalse;
            _columns.Add($"{_provider.QuoteIdentifier(name)} {_provider.BoolType} NOT NULL DEFAULT {def}");
            return this;
        }

        public SchemaBuilder DateCol(string name, bool nullable = true)
        {
            var col = $"{_provider.QuoteIdentifier(name)} {_provider.DateType}";
            if (!nullable) col += " NOT NULL";
            _columns.Add(col);
            return this;
        }

        /// <summary>أعمدة تتبّع قياسية: CreatedAt/CreatedBy/UpdatedAt/UpdatedBy.</summary>
        public SchemaBuilder Audit()
        {
            // DEFAULT باستدعاء دالة (لا حرفي ثابت) يحتاج أقواساً خارجية إلزامياً في SQLite —
            // "DEFAULT datetime('now')" خطأ نحوي، الصحيح "DEFAULT (datetime('now'))". صالحة أيضاً لـ
            // SQL Server/PostgreSQL (GETDATE()/NOW() يقبلان أقواساً خارجية بلا مشكلة).
            _columns.Add($"{_provider.QuoteIdentifier("CreatedAt")} {_provider.DateType} DEFAULT ({_provider.CurrentTimestampFunction})");
            _columns.Add($"{_provider.QuoteIdentifier("CreatedBy")} {_provider.TextType(100)}");
            _columns.Add($"{_provider.QuoteIdentifier("UpdatedAt")} {_provider.DateType} DEFAULT ({_provider.CurrentTimestampFunction})");
            _columns.Add($"{_provider.QuoteIdentifier("UpdatedBy")} {_provider.TextType(100)}");
            return this;
        }

        /// <summary>حذف منطقي: IsDeleted/DeletedAt/DeletedBy.</summary>
        public SchemaBuilder SoftDelete()
        {
            _columns.Add($"{_provider.QuoteIdentifier("IsDeleted")} {_provider.BoolType} NOT NULL DEFAULT {_provider.BoolFalse}");
            _columns.Add($"{_provider.QuoteIdentifier("DeletedAt")} {_provider.DateType}");
            _columns.Add($"{_provider.QuoteIdentifier("DeletedBy")} {_provider.TextType(100)}");
            return this;
        }

        /// <summary>عمود التزامن المتفائل (Optimistic Concurrency) — rowversion تلقائي في SQL Server، عدّاد يدوي في SQLite/PostgreSQL.</summary>
        public SchemaBuilder Concurrency(string name = "RowVersion")
        {
            _columns.Add(_provider.RowVersionColumnDdl(name));
            return this;
        }

        public SchemaBuilder Index(string column, bool unique = false)
        {
            _indexes.Add((column, unique));
            return this;
        }

        public SchemaBuilder ForeignKey(string column, string refTable, string refColumn = "Id")
        {
            _constraints.Add(
                $"FOREIGN KEY ({_provider.QuoteIdentifier(column)}) " +
                $"REFERENCES {_provider.QuoteIdentifier(refTable)}({_provider.QuoteIdentifier(refColumn)})");
            return this;
        }

        public void Create()
        {
            var body = string.Join(",\n    ", _columns.Concat(_constraints));
            DbHelper.Execute(_provider.CreateTableIfNotExists(_tableName, body));

            foreach (var (column, unique) in _indexes)
            {
                var idxName = $"IX_{_tableName}_{column}";
                DbHelper.Execute(_provider.CreateIndexIfNotExists(idxName, _tableName, column, unique));
            }
        }
    }
}
