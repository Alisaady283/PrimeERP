using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;

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
    public class BackupRepository : RepositoryBase<BackupHistoryRecord>, IBackupRepository
    {
        protected override string TableName => "BackupHistory";

        public void CreateTable() =>
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

        protected override BackupHistoryRecord Map(DataRow row) => new()
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

        public override List<BackupHistoryRecord> GetAll(DbConnection conn = null, DbTransaction tx = null) =>
            Query("SELECT * FROM BackupHistory ORDER BY CreatedAt DESC", conn, tx);

        public List<BackupHistoryRecord> GetRecent(int count) =>
            Query($"SELECT * FROM BackupHistory ORDER BY CreatedAt DESC {DbFactory.Current.LimitClause(0, count)}");

        public int Insert(BackupHistoryRecord record) => Insert(null, null, record);

        public int Insert(DbConnection conn, DbTransaction tx, BackupHistoryRecord record) =>
            InsertGetId(
                @"INSERT INTO BackupHistory (FileName, FilePath, SizeBytes, CreatedAt, CreatedBy, Note, BackupType, DatabaseProvider, IsValid, ValidationMessage)
                  VALUES (@fn, @fp, @sz, @ca, @cb, @note, @bt, @dp, @iv, @vm)",
                conn, tx,
                ("@fn", record.FileName), ("@fp", record.FilePath), ("@sz", record.SizeBytes),
                ("@ca", record.CreatedAt), ("@cb", record.CreatedBy), ("@note", record.Note ?? ""),
                ("@bt", (int)record.BackupType), ("@dp", record.DatabaseProvider),
                ("@iv", record.IsValid), ("@vm", record.ValidationMessage));

        public void Delete(int id) => Exec("DELETE FROM BackupHistory WHERE Id = @id", null, null, ("@id", id));

        public void DeleteOlderThan(DateTime cutoff) =>
            Exec("DELETE FROM BackupHistory WHERE CreatedAt < @cutoff", null, null, ("@cutoff", cutoff));
    }
}
