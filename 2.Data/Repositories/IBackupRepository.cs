using System;
using System.Data.Common;
using System.Collections.Generic;

namespace PrimeERP.Data.Repositories
{
    public interface IBackupRepository
    {
        void CreateTable();
        List<BackupHistoryRecord> GetAll(DbConnection conn = null, DbTransaction tx = null);
        List<BackupHistoryRecord> GetRecent(int count);
        BackupHistoryRecord GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        int Insert(BackupHistoryRecord record);
        int Insert(DbConnection conn, DbTransaction tx, BackupHistoryRecord record);
        void Delete(int id);
        void DeleteOlderThan(DateTime cutoff);
    }
}
