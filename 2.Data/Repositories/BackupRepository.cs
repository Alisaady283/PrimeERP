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
        List<BackupHistoryRecord> GetRecent(int count);
        BackupHistoryRecord GetById(int id, PrimeDbContext db = null);
        int Insert(BackupHistoryRecord record);
        int Insert(PrimeDbContext db, BackupHistoryRecord record);
        void Delete(int id);
        void DeleteOlderThan(DateTime cutoff);

        BackupCapability Capability { get; }
        string FileExtension { get; }

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

            if (Capability == BackupCapability.FileCopy)
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                System.IO.File.Copy(sourcePath, DbConfig.Current.FilePath, overwrite: true);
                return;
            }

            Run($"RESTORE DATABASE [{DbConfig.Current.Database}] FROM DISK = @path WITH REPLACE, RECOVERY", sourcePath);
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

        public List<BackupHistoryRecord> GetRecent(int count) =>
            Fetch(q => q.OrderByDescending(b => b.CreatedAt).Take(count));

        public int Insert(BackupHistoryRecord record) => Insert(null, record);

        public int Insert(PrimeDbContext db, BackupHistoryRecord record) => Add(record, db);

        public void Delete(int id) =>
            Write(db =>
            {
                var row = Rows(db).AsTracking().FirstOrDefault(b => b.Id == id);
                if (row != null) SetOf(db).Remove(row);
                return 0;
            });

        public void DeleteOlderThan(DateTime cutoff) =>
            Write(db =>
            {
                SetOf(db).RemoveRange(Rows(db).Where(b => b.CreatedAt < cutoff));
                return 0;
            });
    }
}
