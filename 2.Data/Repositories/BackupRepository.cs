using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع Backup</summary>
    public interface IBackupRepository
    {
        List<BackupHistoryRecord> GetAll(PrimeDbContext db = null);
        BackupHistoryRecord GetById(int id, PrimeDbContext db = null);
        int Insert(BackupHistoryRecord record);
        BackupHistoryRecord Snapshot(string folder, string note, BackupType type);
        string CopyInto(string folder);
        int Insert(PrimeDbContext db, BackupHistoryRecord record);
        void Delete(int id);

        BackupCapability Capability { get; }
        string FileExtension { get; }
        string FolderOf(string configured);

        void CopyTo(string targetPath);
        void RestoreFrom(string sourcePath);
        (bool Ok, string Error) Verify(string filePath);
    }

    /// <summary>طبقة وصول بيانات سجل النسخ</summary>
    public class BackupRepository : RepositoryBase<BackupHistoryRecord>, IBackupRepository
    {
        /// <summary>قدرة المحرّك على النسخ</summary>
        public BackupCapability Capability => DbConfig.Current.Provider switch
        {
            DatabaseProvider.SqlServer  => BackupCapability.SqlCommand,
            DatabaseProvider.PostgreSql => BackupCapability.ExternalTool,
            _                           => BackupCapability.FileCopy
        };

        public string FileExtension => DbConfig.Current.Provider switch
        {
            DatabaseProvider.SqlServer  => ".bak",
            DatabaseProvider.PostgreSql => ".sql",
            _                           => ".db"
        };

        /// <summary>المضبوط وإلا مجلّد البيانات</summary>
        public string FolderOf(string configured) =>
            string.IsNullOrWhiteSpace(configured) ? System.IO.Path.Combine(PrimeERP.Platform.AppInfo.DataFolder, "Backups") : configured;

        /// <summary>أمرُ المحرّك بمسار ملفٍ</summary>
        private static void Run(string sql, string path)
        {
            using var db = DbContextFactory.Open();
            var conn = db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();

            using var cmd = conn.CreateCommand();

            cmd.CommandText = sql;
            cmd.CommandTimeout = DbConfig.Current.CommandTimeout;

            var parameter = cmd.CreateParameter();
            parameter.ParameterName = "@path";
            parameter.Value = path;
            cmd.Parameters.Add(parameter);

            cmd.ExecuteNonQuery();
        }

        /// <summary>نسخة القاعدة إلى ملف</summary>
        public void CopyTo(string targetPath)
        {
            if (Capability == BackupCapability.ExternalTool) throw Unsupported();

            Run(Capability == BackupCapability.FileCopy
                    ? "VACUUM INTO @path"
                    : $"BACKUP DATABASE [{DbConfig.Current.Database}] TO DISK = @path WITH INIT",
                targetPath);
        }

        public void RestoreFrom(string sourcePath)
        {
            if (Capability == BackupCapability.ExternalTool) throw Unsupported();

            var history = GetAll();
            if (Capability == BackupCapability.FileCopy)
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                System.IO.File.Copy(sourcePath, DbConfig.Current.FilePath, overwrite: true);
            }
            else
                Run($"RESTORE DATABASE [{DbConfig.Current.Database}] FROM DISK = @path WITH REPLACE, RECOVERY", sourcePath);

            SchemaSync.Run();
            KeepHistory(history);
        }

        /// <summary>النسخ الباقية على القرص</summary>
        private void KeepHistory(List<BackupHistoryRecord> history)
        {
            var restored = GetAll().Select(r => r.FilePath).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var record in history.Where(r => !restored.Contains(r.FilePath) && System.IO.File.Exists(r.FilePath)))
            {
                record.Id = 0;
                Insert(record);
            }
        }

        private static NotSupportedException Unsupported() =>
            new("النسخ الاحتياطي لقاعدة PostgreSQL يحتاج أداة pg_dump خارجية — غير مدعوم من داخل التطبيق.");

        /// <summary>سلامة الملف وكونه قاعدة PrimeERP</summary>
        public (bool Ok, string Error) Verify(string filePath)
        {
            var kind = DbConfig.Current.Provider;

            try
            {
                if (kind == DatabaseProvider.SqlServer)
                {
                    Run("RESTORE VERIFYONLY FROM DISK = @path", filePath);
                    return (true, null);
                }

                if (kind != DatabaseProvider.Sqlite) return (true, null);

                using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={filePath};Mode=ReadOnly");
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA integrity_check;";
                    var result = cmd.ExecuteScalar()?.ToString();
                    if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase)) return (false, result);
                }

                using var tables = conn.CreateCommand();
                tables.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('AppSettings', 'Accounts')";
                return Convert.ToInt64(tables.ExecuteScalar()) == 2 ? (true, null) : (false, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        protected override string TableName => "BackupHistory";


        public override List<BackupHistoryRecord> GetAll(PrimeDbContext db = null) =>
            Fetch(q => q.OrderByDescending(b => b.CreatedAt), db);

        public int Insert(BackupHistoryRecord record) => Insert(null, record);

        /// <summary>نسخة القاعدة في مجلّد</summary>
        public string CopyInto(string folder)
        {
            System.IO.Directory.CreateDirectory(folder);

            var path = System.IO.Path.Combine(folder, $"PrimeERP_{DateTime.Now:yyyyMMdd_HHmmss}{FileExtension}");
            CopyTo(path);
            return path;
        }

        /// <summary>نسخة القاعدة وسجلّها</summary>
        public BackupHistoryRecord Snapshot(string folder, string note, BackupType type)
        {
            var path = CopyInto(folder);

            var record = new BackupHistoryRecord
            {
                FileName = System.IO.Path.GetFileName(path), FilePath = path, SizeBytes = new System.IO.FileInfo(path).Length, CreatedAt = DateTime.Now,
                Note = note, BackupType = type, DatabaseProvider = DbConfig.Current.Provider.ToString(), IsValid = true
            };
            record.Id = Insert(record);
            return record;
        }

        public int Insert(PrimeDbContext db, BackupHistoryRecord record) => Add(record, db);

        public void Delete(int id) =>
            Remove(b => b.Id == id);
    }
}
