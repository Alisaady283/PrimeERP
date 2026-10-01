using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Core
{
    /// <summary>جداول يبنيها المستخدم</summary>
    public static class BuiltTables
    {
        private static readonly Dictionary<string, List<BuilderColumn>> Declared = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>يتغيّر مع كل إعلان فيُعاد</summary>
        public static int Version { get; private set; }

        public static IReadOnlyDictionary<string, List<BuilderColumn>> All => Declared;

        public static void Declare(string table, IEnumerable<BuilderColumn> columns)
        {
            if (string.IsNullOrWhiteSpace(table)) return;

            var stored = columns.Where(c => c.Aggregate == BuilderAggregate.None).ToList();
            if (Declared.TryGetValue(table, out var current) && Same(current, stored)) return;

            Declared[table] = stored;
            Version++;
        }

        private static bool Same(List<BuilderColumn> a, List<BuilderColumn> b) =>
            a.Count == b.Count && a.Zip(b).All(p => p.First.Name == p.Second.Name && p.First.DataType == p.Second.DataType);

        public static Type ClrTypeOf(BuilderDataType type, bool required) => type switch
        {
            BuilderDataType.Number or BuilderDataType.Money => typeof(decimal),
            BuilderDataType.Date      => required ? typeof(DateTime) : typeof(DateTime?),
            BuilderDataType.Bool      => typeof(bool),
            BuilderDataType.Reference => required ? typeof(int) : typeof(int?),
            _                         => typeof(string),
        };

        /// <summary>يُعلن الجدول في النموذج</summary>
        public static void Shape(ModelBuilder model, string table, List<BuilderColumn> columns)
        {
            model.SharedTypeEntity<Dictionary<string, object>>(table, b =>
            {
                b.ToTable(table);
                b.IndexerProperty<int>("Id");
                b.HasKey("Id");

                foreach (var c in columns)
                    b.IndexerProperty(ClrTypeOf(c.DataType, c.IsRequired), c.Name);

                b.IndexerProperty<DateTime?>("CreatedAt");
                b.IndexerProperty<string>("CreatedBy");
                b.IndexerProperty<DateTime?>("UpdatedAt");
                b.IndexerProperty<string>("UpdatedBy");
                b.IndexerProperty<bool>("IsDeleted");
                b.IndexerProperty<DateTime?>("DeletedAt");
                b.IndexerProperty<string>("DeletedBy");

                if (columns.Any(c => c.Name == "HeaderId")) return;
            });
        }
    }

    /// <summary>مفتاحٌ يضمّ نسخة جداول البنّاء</summary>
    public class BuiltModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime) =>
            (context.GetType(), BuiltTables.Version, designTime);
    }

    public partial class PrimeDbContext
    {
        /// <summary>صفوف جدولٍ بناه المستخدم</summary>
        public IQueryable<Dictionary<string, object>> Rows(string table) =>
            Set<Dictionary<string, object>>(table);

        public DbSet<Dictionary<string, object>> BuiltSet(string table) =>
            Set<Dictionary<string, object>>(table);

        /// <summary>ينشئ جدول المستخدم بمولّد DDL</summary>
        public void CreateBuiltTable(string table, List<BuilderColumn> columns, string headerTable = null)
        {
            if (Database.GetService<IRelationalDatabaseCreator>().HasTables() && TableExists(table)) return;

            var operation = new CreateTableOperation { Name = table };
            operation.Columns.Add(Column("Id", typeof(int), nullable: false, identity: true));
            operation.PrimaryKey = new AddPrimaryKeyOperation { Table = table, Columns = new[] { "Id" } };

            if (headerTable != null)
            {
                operation.Columns.Add(Column("HeaderId", typeof(int), nullable: false));
                operation.Columns.Add(Column("LineNo", typeof(int), nullable: false));
                operation.ForeignKeys.Add(new AddForeignKeyOperation
                {
                    Table = table, Columns = new[] { "HeaderId" },
                    PrincipalTable = headerTable, PrincipalColumns = new[] { "Id" }
                });
            }

            foreach (var c in columns.Where(c => c.Aggregate == BuilderAggregate.None))
                operation.Columns.Add(Column(c.Name, BuiltTables.ClrTypeOf(c.DataType, c.IsRequired), !c.IsRequired,
                                             max: c.MaxLength > 0 ? c.MaxLength : null));

            foreach (var (name, clr) in new (string, Type)[]
                     {
                         ("CreatedAt", typeof(DateTime?)), ("CreatedBy", typeof(string)),
                         ("UpdatedAt", typeof(DateTime?)), ("UpdatedBy", typeof(string)),
                         ("IsDeleted", typeof(bool)), ("DeletedAt", typeof(DateTime?)), ("DeletedBy", typeof(string))
                     })
                operation.Columns.Add(Column(name, clr, clr != typeof(bool)));

            Run(operation);
        }

        private AddColumnOperation Column(string name, Type clr, bool nullable = true, bool identity = false,
                                          int? max = null) =>
            new()
            {
                Name = name, ClrType = clr, IsNullable = nullable, MaxLength = max,
                ColumnType = null,
                [Database.IsSqlite() ? "Sqlite:Autoincrement" : "SqlServer:Identity"] = identity ? (object)true : null
            };

        private void Run(MigrationOperation operation)
        {
            var commands = Database.GetService<IMigrationsSqlGenerator>().Generate(new[] { operation }, Model);
            Database.GetService<IMigrationCommandExecutor>()
                .ExecuteNonQuery(commands, Database.GetService<IRelationalConnection>());
        }

        /// <summary>وجود الجدول بقراءةٍ فارغة</summary>
        private bool TableExists(string table)
        {
            var connection = Database.GetDbConnection();
            var wasClosed = connection.State != System.Data.ConnectionState.Open;
            if (wasClosed) connection.Open();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {Database.GetService<ISqlGenerationHelper>().DelimitIdentifier(table)} WHERE 1 = 0";
                using var reader = command.ExecuteReader(System.Data.CommandBehavior.SchemaOnly);
                return true;
            }
            catch { return false; }
            finally { if (wasClosed) connection.Close(); }
        }
    }
}
