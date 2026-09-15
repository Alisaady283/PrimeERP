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
    public interface IAssetDepreciationRepository
    {
        void CreateTable();
        AssetDepreciation GetById(int id, DbConnection conn = null, DbTransaction tx = null);

        (List<AssetDepreciation> Items, int Total) GetPaged(int page, int pageSize, string searchText = null,
            int? assetId = null, string sortColumn = "PeriodDate", bool sortDescending = true);

        /// <summary>أقساط أصلٍ بعينه — منها يُشتقّ مجمّعه وآخر شهرٍ أُهلك، فلا رقم محفوظ يتناقض معها.</summary>
        List<AssetDepreciation> OfAsset(int assetId, DbConnection conn = null, DbTransaction tx = null);

        /// <summary>معرِّفات القيود التي تملكها أقساطٌ قائمة — ما عداها قيدُ إهلاكٍ يتيم.</summary>
        List<int> LinkedEntryIds();

        int Insert(AssetDepreciation charge, DbConnection conn = null, DbTransaction tx = null);
        void Delete(int id, DbConnection conn = null, DbTransaction tx = null);
        void SetJournalEntryId(DbConnection conn, DbTransaction tx, int id, int journalEntryId);
    }

    public class AssetDepreciationRepository : RepositoryBase<AssetDepreciation>, IAssetDepreciationRepository
    {
        protected override string TableName => "AssetDepreciations";

        public void CreateTable() =>
            SchemaBuilder.Table("AssetDepreciations")
                .Id()
                .Int("AssetId", nullable: false)
                .DateCol("PeriodDate")
                .Decimal("Amount")
                .Int("JournalEntryId")
                .Text("Notes")
                .Audit()
                .Index("AssetId")
                .Create();

        protected override AssetDepreciation Map(DataRow row) => new()
        {
            Id             = Convert.ToInt32(row["Id"]),
            AssetId        = Convert.ToInt32(row["AssetId"]),
            PeriodDate     = row["PeriodDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["PeriodDate"]),
            Amount         = Convert.ToDecimal(row["Amount"]),
            JournalEntryId = row["JournalEntryId"] == DBNull.Value ? null : Convert.ToInt32(row["JournalEntryId"]),
            Notes          = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            CreatedAt      = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy      = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString()
        };

        public override AssetDepreciation GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM AssetDepreciations WHERE Id = @id", conn, tx, ("@id", id));

        public (List<AssetDepreciation> Items, int Total) GetPaged(int page, int pageSize, string searchText = null,
            int? assetId = null, string sortColumn = "PeriodDate", bool sortDescending = true)
        {
            var where = new WhereBuilder().LikeAny(searchText, "Notes").Eq("AssetId", assetId);

            return Page(where, page, pageSize, OrderBuilder.By(sortColumn, sortDescending, "Id"));
        }

        public List<AssetDepreciation> OfAsset(int assetId, DbConnection conn = null, DbTransaction tx = null) =>
            Query("SELECT * FROM AssetDepreciations WHERE AssetId = @asset ORDER BY PeriodDate", conn, tx, ("@asset", assetId));

        public List<int> LinkedEntryIds() =>
            QueryAs(row => Convert.ToInt32(row["JournalEntryId"]),
                "SELECT JournalEntryId FROM AssetDepreciations WHERE JournalEntryId IS NOT NULL");

        public int Insert(AssetDepreciation c, DbConnection conn = null, DbTransaction tx = null) =>
            InsertGetId(@"INSERT INTO AssetDepreciations (AssetId, PeriodDate, Amount, Notes, CreatedBy)
                          VALUES (@asset, @period, @amount, @notes, @by)",
                conn, tx,
                ("@asset", c.AssetId), ("@period", c.PeriodDate), ("@amount", c.Amount),
                ("@notes", c.Notes ?? ""), ("@by", c.CreatedBy));

        /// <summary>حذفٌ صلب: القسط يزول بقيده، فلا يبقى أثرٌ مخفيّ يُحتسَب في المجمّع.</summary>
        public void Delete(int id, DbConnection conn = null, DbTransaction tx = null) => HardDelete(id, conn, tx);

        public void SetJournalEntryId(DbConnection conn, DbTransaction tx, int id, int journalEntryId) =>
            Exec("UPDATE AssetDepreciations SET JournalEntryId = @j WHERE Id = @id", conn, tx,
                ("@j", journalEntryId), ("@id", id));
    }
}
