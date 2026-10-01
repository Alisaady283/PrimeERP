using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع AssetRevaluation</summary>
    public interface IAssetRevaluationRepository
    {
        AssetRevaluation GetById(int id, PrimeDbContext db = null);
        (List<AssetRevaluation> Items, int Total) GetPaged(int page, int pageSize, string searchText = null,
            int? assetId = null, string sortColumn = "RevaluationDate", bool sortDescending = true);

        int Insert(AssetRevaluation revaluation, PrimeDbContext db = null);
        void Update(AssetRevaluation revaluation, PrimeDbContext db = null);
        void Delete(int id, string deletedBy, PrimeDbContext db = null);
        void SetJournalEntryId(PrimeDbContext db, int id, int journalEntryId);
    }

    public class AssetRevaluationRepository : RepositoryBase<AssetRevaluation>, IAssetRevaluationRepository
    {
        protected override string TableName => "AssetRevaluations";

        /// <summary>كود الأصل واسمه عرضٌ فقط</summary>
        private static List<AssetRevaluation> WithAsset(List<AssetRevaluation> rows) =>
            WithCodeNames<Asset>("Assets", rows, x => x.AssetId, (x, code, name) => (x.AssetCode, x.AssetName) = (code, name));

        public override AssetRevaluation GetById(int id, PrimeDbContext db = null) =>
            WithAsset(Fetch(q => q.Where(x => x.Id == id), db)).FirstOrDefault();



        public (List<AssetRevaluation> Items, int Total) GetPaged(int page, int pageSize, string searchText = null,
            int? assetId = null, string sortColumn = "RevaluationDate", bool sortDescending = true)
        {
            IQueryable<AssetRevaluation> Shape(IQueryable<AssetRevaluation> rows)
            {
                var q = rows;
                if (!string.IsNullOrWhiteSpace(searchText))
                    q = q.Where(x => EF.Functions.Like(x.Notes, $"%{searchText}%"));
                if (assetId != null) q = q.Where(x => x.AssetId == assetId);
                return q;
            }

            var (items, total) = Page(page, pageSize, Shape, q => (By(x => x.RevaluationDate, sortDescending))(q).ThenByDescending(x => x.Id));
            return (WithAsset(items), total);
        }

        public int Insert(AssetRevaluation r, PrimeDbContext db = null) => Add(r, db);

        public void Update(AssetRevaluation r, PrimeDbContext db = null) =>
            Edit(x => x.Id == r.Id, row =>
            {
                row.AssetId = r.AssetId;
                row.RevaluationDate = r.RevaluationDate;
                row.OldValue = r.OldValue;
                row.NewValue = r.NewValue;
                row.Notes = r.Notes ?? "";
                row.UpdatedBy = r.UpdatedBy;
            }, db);

        public void Delete(int id, string deletedBy, PrimeDbContext db = null) =>
            SoftDelete(id, deletedBy, db);

        public void SetJournalEntryId(PrimeDbContext db, int id, int journalEntryId) =>
            Set(x => x.Id == id, s => s.SetProperty(r => r.JournalEntryId, journalEntryId), db);
    }
}
