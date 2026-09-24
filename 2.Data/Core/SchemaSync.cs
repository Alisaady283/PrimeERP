using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;

namespace PrimeERP.Data.Core
{
    /// <summary>إلحاق ناقص النموذج بالقاعدة</summary>
    public static class SchemaSync
    {
        public static void Run()
        {
            using var db = DbContextFactory.Open();

            var creator = db.Database.GetService<IRelationalDatabaseCreator>();
            if (!creator.Exists()) creator.Create();

            var model = db.GetService<IDesignTimeModel>().Model;
            var blueprint = db.GetService<IMigrationsModelDiffer>()
                              .GetDifferences(null, model.GetRelationalModel())
                              .ToList();

            var connection = db.Database.GetDbConnection();
            var wasClosed = connection.State != ConnectionState.Open;
            if (wasClosed) connection.Open();

            List<MigrationOperation> operations;
            try { operations = Missing(db, blueprint); }
            finally { if (wasClosed) connection.Close(); }

            if (operations.Count == 0) return;

            var commands = db.GetService<IMigrationsSqlGenerator>().Generate(operations, model);
            db.GetService<IMigrationCommandExecutor>()
              .ExecuteNonQuery(commands, db.GetService<IRelationalConnection>());
        }

        private static List<MigrationOperation> Missing(PrimeDbContext db, List<MigrationOperation> blueprint)
        {
            var operations = new List<MigrationOperation>();
            var created = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var table in blueprint.OfType<CreateTableOperation>())
            {
                var live = Columns(db, table.Name);
                if (live == null) { operations.Add(table); created.Add(table.Name); continue; }

                foreach (var column in table.Columns.Where(c => !live.Contains(c.Name)))
                    operations.Add(Added(column));
            }

            operations.AddRange(blueprint.OfType<CreateIndexOperation>().Where(i => created.Contains(i.Table)));
            return operations;
        }

        /// <summary>عمودٌ يُضاف لجدولٍ فيه صفوف</summary>
        private static AddColumnOperation Added(AddColumnOperation column) => new()
        {
            Table = column.Table, Name = column.Name, ClrType = column.ClrType,
            ColumnType = column.ColumnType, MaxLength = column.MaxLength,
            Precision = column.Precision, Scale = column.Scale,
            DefaultValue = column.DefaultValue, DefaultValueSql = column.DefaultValueSql,
            IsNullable = column.IsNullable || column.DefaultValue == null && column.DefaultValueSql == null,
        };

        /// <summary>أعمدة الجدول من وصف القارئ</summary>
        private static HashSet<string> Columns(PrimeDbContext db, string table)
        {
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT * FROM {Quote(db, table)} WHERE 1 = 0";

            try
            {
                using var reader = command.ExecuteReader(CommandBehavior.SchemaOnly);
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++) names.Add(reader.GetName(i));
                return names;
            }
            catch { return null; }
        }

        private static string Quote(PrimeDbContext db, string identifier) =>
            db.GetService<ISqlGenerationHelper>().DelimitIdentifier(identifier);
    }
}
