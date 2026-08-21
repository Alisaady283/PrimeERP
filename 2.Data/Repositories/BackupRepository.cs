using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Data.Core;
using PrimeERP.Data.Schema;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Data.Repositories
{
    public class BackupHistoryRecord
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string Note { get; set; }
        public BackupType BackupType { get; set; }
        public string DatabaseProvider { get; set; }
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; }
    }

    /// <summary>طبقة وصول بيانات سجل النسخ الاحتياطية — SQL خام ↔ BackupHistoryRecord فقط. بلا تحقق ملفات ولا نسخ فعلي ولا حساب retention (كلها في BackupService).</summary>
    public static class BackupRepository
    {
        public static void CreateTable()
        {
            SchemaBuilder.Table("BackupHistory")
                .Id()
                .Text("FileName", 260, required: true)
                .Text("FilePath", 500, required: true)
                .Int("SizeBytes", nullable: false, defaultValue: 0)
                .DateCol("CreatedAt", nullable: false)
                .Text("CreatedBy", 100)
                .Text("Note", 400)
                .Int("BackupType", nullable: false, defaultValue: 1)
                .Text("DatabaseProvider", 30)
                .Bool("IsValid", defaultValue: true)
                .Text("ValidationMessage", 500)
                .Create();
        }

        private static BackupHistoryRecord Map(DataRow row) => new()
        {
            Id                = Convert.ToInt32(row["Id"]),
            FileName          = row["FileName"].ToString(),
            FilePath          = row["FilePath"].ToString(),
            SizeBytes         = Convert.ToInt64(row["SizeBytes"]),
            CreatedAt         = Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy         = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
            Note              = row["Note"] == DBNull.Value ? null : row["Note"].ToString(),
            BackupType        = (BackupType)Convert.ToInt32(row["BackupType"]),
            DatabaseProvider  = row["DatabaseProvider"] == DBNull.Value ? null : row["DatabaseProvider"].ToString(),
            IsValid           = Convert.ToBoolean(row["IsValid"]),
            ValidationMessage = row["ValidationMessage"] == DBNull.Value ? null : row["ValidationMessage"].ToString()
        };

        public static List<BackupHistoryRecord> GetAll() =>
            Db.Query("SELECT * FROM BackupHistory ORDER BY CreatedAt DESC").AsEnumerable().Select(Map).ToList();

        public static List<BackupHistoryRecord> GetRecent(int count) =>
            Db.Query($"SELECT * FROM BackupHistory ORDER BY CreatedAt DESC {DbFactory.Current.LimitClause(0, count)}")
              .AsEnumerable().Select(Map).ToList();

        public static BackupHistoryRecord GetById(int id) =>
            Db.Query("SELECT * FROM BackupHistory WHERE Id = @id", Db.Params(("@id", id)))
              .AsEnumerable().Select(Map).FirstOrDefault();

        public static int Insert(BackupHistoryRecord record) =>
            Db.InsertAndGetId(
                @"INSERT INTO BackupHistory (FileName, FilePath, SizeBytes, CreatedAt, CreatedBy, Note, BackupType, DatabaseProvider, IsValid, ValidationMessage)
                  VALUES (@fn, @fp, @sz, @ca, @cb, @note, @bt, @dp, @iv, @vm)",
                Db.Params(
                    ("@fn", record.FileName), ("@fp", record.FilePath), ("@sz", record.SizeBytes),
                    ("@ca", record.CreatedAt), ("@cb", record.CreatedBy), ("@note", record.Note ?? ""),
                    ("@bt", (int)record.BackupType), ("@dp", record.DatabaseProvider),
                    ("@iv", record.IsValid), ("@vm", record.ValidationMessage)));

        public static int Insert(DbConnection conn, DbTransaction tx, BackupHistoryRecord record) =>
            Db.InsertAndGetId(conn, tx,
                @"INSERT INTO BackupHistory (FileName, FilePath, SizeBytes, CreatedAt, CreatedBy, Note, BackupType, DatabaseProvider, IsValid, ValidationMessage)
                  VALUES (@fn, @fp, @sz, @ca, @cb, @note, @bt, @dp, @iv, @vm)",
                Db.Params(
                    ("@fn", record.FileName), ("@fp", record.FilePath), ("@sz", record.SizeBytes),
                    ("@ca", record.CreatedAt), ("@cb", record.CreatedBy), ("@note", record.Note ?? ""),
                    ("@bt", (int)record.BackupType), ("@dp", record.DatabaseProvider),
                    ("@iv", record.IsValid), ("@vm", record.ValidationMessage)));

        public static void Delete(int id) =>
            Db.Execute("DELETE FROM BackupHistory WHERE Id = @id", Db.Params(("@id", id)));

        public static void DeleteOlderThan(DateTime cutoff) =>
            Db.Execute("DELETE FROM BackupHistory WHERE CreatedAt < @cutoff", Db.Params(("@cutoff", cutoff)));
    }
}
