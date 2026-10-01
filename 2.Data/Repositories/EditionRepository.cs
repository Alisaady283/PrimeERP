using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    /// <summary>نسخةٌ أخرى: قاعدتها وملفاتها</summary>
    public interface IEditionRepository
    {
        DbConnection Open(string databasePath);
        void CopyProgram(string source, string target, double share, Action<int> report);
        void PointAtDatabase(string settingsFile, string databasePath);
    }

    public class EditionRepository : IEditionRepository
    {
        /// <summary>ما ليس من البرنامج</summary>
        private static readonly string[] NotProgram = { ".pdb", ".db", ".bak", ".log" };

        public DbConnection Open(string databasePath)
        {
            var connection = new Microsoft.Data.Sqlite.SqliteConnection(
                new DbConfig { Provider = DatabaseProvider.Sqlite, FilePath = databasePath }.ConnectionString());

            connection.Open();
            return connection;
        }

        /// <summary>ملفات البرنامج بنسبة تقدّمها</summary>
        public void CopyProgram(string source, string target, double share, Action<int> report)
        {
            Directory.CreateDirectory(target);

            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));

            var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
            var reported = -1;

            for (var i = 0; i < files.Length; i++)
            {
                if (!NotProgram.Contains(Path.GetExtension(files[i]), StringComparer.OrdinalIgnoreCase))
                    File.Copy(files[i], Path.Combine(target, Path.GetRelativePath(source, files[i])), overwrite: true);

                var percent = (int)((i + 1) * share / files.Length);
                if (percent == reported) continue;

                reported = percent;
                report(percent);
            }
        }

        public void PointAtDatabase(string settingsFile, string databasePath)
        {
            var settings = File.Exists(settingsFile)
                ? JsonNode.Parse(File.ReadAllText(settingsFile))
                : new JsonObject();

            settings["Database"] ??= new JsonObject();
            settings["Database"]["FilePath"] = databasePath;

            File.WriteAllText(settingsFile, settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
