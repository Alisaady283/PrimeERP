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
    public interface IAssetDisposalRepository
    {
        void CreateTable();
        AssetDisposal GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        (List<AssetDisposal> Items, int Total) GetPaged(int page, int pageSize, string searchText = null,
            int? assetId = null, string sortColumn = "DisposalDate", bool sortDescending = true);

        int Insert(AssetDisposal disposal, DbConnection conn = null, DbTransaction tx = null);
        void Update(AssetDisposal disposal, DbConnection conn = null, DbTransaction tx = null);
        void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null);
        void SetJournalEntryId(DbConnection conn, DbTransaction tx, int id, int journalEntryId);
    }

    public class AssetDisposalRepository : RepositoryBase<AssetDisposal>, IAssetDisposalRepository
    {
        protected override string TableName => "AssetDisposals";

        public void CreateTable() =>
            SchemaBuilder.Table("AssetDisposals")
                .Id()
                .Int("AssetId", nullable: false)
                .DateCol("DisposalDate")
                .Int("TreasuryId", nullable: false)
                .Decimal("SalePrice")
                .Decimal("AssetValue")
                .Decimal("AccumulatedDepreciation")
                .Text("Notes")
                .Int("JournalEntryId")
                .Audit()
                .SoftDelete()
                .Concurrency()
                .Index("AssetId")
                .Create();

        protected override AssetDisposal Map(DataRow row) => new()
        {
            Id           = Convert.ToInt32(row["Id"]),
            AssetId      = Convert.ToInt32(row["AssetId"]),
            DisposalDate = row["DisposalDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["DisposalDate"]),
            TreasuryId   = Convert.ToInt32(row["TreasuryId"]),
            SalePrice    = Convert.ToDecimal(row["SalePrice"]),
            AssetValue   = Convert.ToDecimal(row["AssetValue"]),
            AccumulatedDepreciation = Convert.ToDecimal(row["AccumulatedDepreciation"]),
            Notes        = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            JournalEntryId = row["JournalEntryId"] == DBNull.Value ? null : Convert.ToInt32(row["JournalEntryId"]),
            CreatedAt    = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy    = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
            UpdatedAt    = row["UpdatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["UpdatedAt"]),
            UpdatedBy    = row["UpdatedBy"] == DBNull.Value ? null : row["UpdatedBy"].ToString(),
            IsDeleted    = Convert.ToBoolean(row["IsDeleted"]),
            RowVersion   = Convert.ToInt64(row["RowVersion"])
        };

        public override AssetDisposal GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM AssetDisposals WHERE Id = @id AND IsDeleted = @d", conn, tx, ("@id", id), ("@d", false));

        public (List<AssetDisposal> Items, int Total) GetPaged(int page, int pageSize, string searchText = null,
            int? assetId = null, string sortColumn = "DisposalDate", bool sortDescending = true)
        {
            var where = new WhereBuilder()
                .Eq("IsDeleted", false)
                .LikeAny(searchText, "Notes")
                .Eq("AssetId", assetId);

            return Page(where, page, pageSize, OrderBuilder.By(sortColumn, sortDescending, "Id"));
        }

        public int Insert(AssetDisposal d, DbConnection conn = null, DbTransaction tx = null) =>
            InsertGetId(@"INSERT INTO AssetDisposals (AssetId, DisposalDate, TreasuryId, SalePrice, AssetValue,
                                 AccumulatedDepreciation, Notes, CreatedBy)
                          VALUES (@asset, @date, @treasury, @price, @value, @accumulated, @notes, @by)",
                conn, tx,
                ("@asset", d.AssetId), ("@date", d.DisposalDate), ("@treasury", d.TreasuryId), ("@price", d.SalePrice),
                ("@value", d.AssetValue), ("@accumulated", d.AccumulatedDepreciation),
                ("@notes", d.Notes ?? ""), ("@by", d.CreatedBy));

        public void Update(AssetDisposal d, DbConnection conn = null, DbTransaction tx = null) =>
            Exec(@"UPDATE AssetDisposals SET AssetId = @asset, DisposalDate = @date, TreasuryId = @treasury,
                          SalePrice = @price, AssetValue = @value, AccumulatedDepreciation = @accumulated,
                          Notes = @notes, UpdatedAt = @now, UpdatedBy = @by
                   WHERE Id = @id",
                conn, tx,
                ("@asset", d.AssetId), ("@date", d.DisposalDate), ("@treasury", d.TreasuryId), ("@price", d.SalePrice),
                ("@value", d.AssetValue), ("@accumulated", d.AccumulatedDepreciation),
                ("@notes", d.Notes ?? ""), ("@now", DateTime.Now), ("@by", d.UpdatedBy), ("@id", d.Id));

        public void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null) =>
            SoftDelete(id, deletedBy, conn, tx);

        public void SetJournalEntryId(DbConnection conn, DbTransaction tx, int id, int journalEntryId) =>
            Exec("UPDATE AssetDisposals SET JournalEntryId = @j WHERE Id = @id", conn, tx,
                ("@j", journalEntryId), ("@id", id));
    }
}
