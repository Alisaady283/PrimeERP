using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Core;
using PrimeERP.Data.Query;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IAssetRevaluationRepository
    {
        void CreateTable();
        AssetRevaluation GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        (List<AssetRevaluation> Items, int Total) GetPaged(int page, int pageSize, string searchText = null,
            int? assetId = null, string sortColumn = "RevaluationDate", bool sortDescending = true);

        int Insert(AssetRevaluation revaluation, DbConnection conn = null, DbTransaction tx = null);
        void Update(AssetRevaluation revaluation, DbConnection conn = null, DbTransaction tx = null);
        void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null);
        void SetJournalEntryId(DbConnection conn, DbTransaction tx, int id, int journalEntryId);
    }

    public class AssetRevaluationRepository : RepositoryBase<AssetRevaluation>, IAssetRevaluationRepository
    {
        protected override string TableName => "AssetRevaluations";

        public void CreateTable() =>
            SchemaBuilder.Table("AssetRevaluations")
                .Id()
                .Int("AssetId", nullable: false)
                .DateCol("RevaluationDate")
                .Decimal("OldValue")
                .Decimal("NewValue")
                .Text("Notes")
                .Int("JournalEntryId")
                .Audit()
                .SoftDelete()
                .Concurrency()
                .Index("AssetId")
                .Create();

        protected override AssetRevaluation Map(DataRow row) => new()
        {
            Id              = Convert.ToInt32(row["Id"]),
            AssetId         = Convert.ToInt32(row["AssetId"]),
            RevaluationDate = row["RevaluationDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["RevaluationDate"]),
            OldValue        = Convert.ToDecimal(row["OldValue"]),
            NewValue        = Convert.ToDecimal(row["NewValue"]),
            Notes           = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            JournalEntryId  = row["JournalEntryId"] == DBNull.Value ? null : Convert.ToInt32(row["JournalEntryId"]),
            CreatedAt       = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy       = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
            UpdatedAt       = row["UpdatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["UpdatedAt"]),
            UpdatedBy       = row["UpdatedBy"] == DBNull.Value ? null : row["UpdatedBy"].ToString(),
            IsDeleted       = Convert.ToBoolean(row["IsDeleted"]),
            RowVersion      = Convert.ToInt64(row["RowVersion"])
        };

        public override AssetRevaluation GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM AssetRevaluations WHERE Id = @id AND IsDeleted = @d", conn, tx, ("@id", id), ("@d", false));

        public (List<AssetRevaluation> Items, int Total) GetPaged(int page, int pageSize, string searchText = null,
            int? assetId = null, string sortColumn = "RevaluationDate", bool sortDescending = true)
        {
            var where = new WhereBuilder()
                .Eq("IsDeleted", false)
                .LikeAny(searchText, "Notes")
                .Eq("AssetId", assetId);

            return Page(where, page, pageSize, OrderBuilder.By(sortColumn, sortDescending, "Id"));
        }

        public int Insert(AssetRevaluation r, DbConnection conn = null, DbTransaction tx = null) =>
            InsertGetId(@"INSERT INTO AssetRevaluations (AssetId, RevaluationDate, OldValue, NewValue, Notes, CreatedBy)
                          VALUES (@asset, @date, @old, @new, @notes, @by)",
                conn, tx,
                ("@asset", r.AssetId), ("@date", r.RevaluationDate), ("@old", r.OldValue), ("@new", r.NewValue),
                ("@notes", r.Notes ?? ""), ("@by", r.CreatedBy));

        public void Update(AssetRevaluation r, DbConnection conn = null, DbTransaction tx = null) =>
            Exec(@"UPDATE AssetRevaluations SET AssetId = @asset, RevaluationDate = @date, OldValue = @old,
                          NewValue = @new, Notes = @notes, UpdatedAt = @now, UpdatedBy = @by
                   WHERE Id = @id",
                conn, tx,
                ("@asset", r.AssetId), ("@date", r.RevaluationDate), ("@old", r.OldValue), ("@new", r.NewValue),
                ("@notes", r.Notes ?? ""), ("@now", DateTime.Now), ("@by", r.UpdatedBy), ("@id", r.Id));

        public void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null) =>
            SoftDelete(id, deletedBy, conn, tx);

        public void SetJournalEntryId(DbConnection conn, DbTransaction tx, int id, int journalEntryId) =>
            Exec("UPDATE AssetRevaluations SET JournalEntryId = @j WHERE Id = @id", conn, tx,
                ("@j", journalEntryId), ("@id", id));
    }
}
