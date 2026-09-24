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



        public (List<AssetRevaluation> Items, int Total) GetPaged(int page, int pageSize, string searchText = null,
            int? assetId = null, string sortColumn = "RevaluationDate", bool sortDescending = true)
        {
            IQueryable<AssetRevaluation> Shape(IQueryable<AssetRevaluation> rows)
            {
                var q = rows.Where(x => !x.IsDeleted);
                if (!string.IsNullOrWhiteSpace(searchText))
                    q = q.Where(x => EF.Functions.Like(x.Notes, $"%{searchText}%"));
                if (assetId != null) q = q.Where(x => x.AssetId == assetId);
                return q;
            }

            return Page(page, pageSize, Shape, q => (By(x => x.RevaluationDate, sortDescending))(q).ThenByDescending(x => x.Id));
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
                row.UpdatedAt = DateTime.Now;
                row.UpdatedBy = r.UpdatedBy;
            }, db);

        public void Delete(int id, string deletedBy, PrimeDbContext db = null) =>
            SoftDelete(id, deletedBy, db);

        public void SetJournalEntryId(PrimeDbContext db, int id, int journalEntryId) =>
            Write(db =>
            {
                var row = Rows(db).AsTracking().FirstOrDefault(x => x.Id == id);
                if (row != null) row.JournalEntryId = journalEntryId;
                return 0;
            }, db);
    }
}
