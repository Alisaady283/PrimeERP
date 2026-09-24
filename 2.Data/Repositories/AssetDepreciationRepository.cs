using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع AssetDepreciation</summary>
    public interface IAssetDepreciationRepository
    {
        AssetDepreciation GetById(int id, PrimeDbContext db = null);

        (List<AssetDepreciation> Items, int Total) GetPaged(int page, int pageSize, string searchText = null,
            int? assetId = null, string sortColumn = "PeriodDate", bool sortDescending = true);

        List<AssetDepreciation> OfAsset(int assetId, PrimeDbContext db = null);

        List<int> LinkedEntryIds();

        int Insert(AssetDepreciation charge, PrimeDbContext db = null);
        void Delete(int id, PrimeDbContext db = null);
        void SetJournalEntryId(PrimeDbContext db, int id, int journalEntryId);
    }

    public class AssetDepreciationRepository : RepositoryBase<AssetDepreciation>, IAssetDepreciationRepository
    {
        protected override string TableName => "AssetDepreciations";



        public (List<AssetDepreciation> Items, int Total) GetPaged(int page, int pageSize, string searchText = null,
            int? assetId = null, string sortColumn = "PeriodDate", bool sortDescending = true)
        {
            IQueryable<AssetDepreciation> Shape(IQueryable<AssetDepreciation> rows)
            {
                var q = rows;
                if (!string.IsNullOrWhiteSpace(searchText))
                    q = q.Where(c => EF.Functions.Like(c.Notes, $"%{searchText}%"));
                if (assetId != null) q = q.Where(c => c.AssetId == assetId);
                return q;
            }

            return Page(page, pageSize, Shape, q => (sortColumn == "Amount" ? By(c => c.Amount, sortDescending)
                                         : By(c => c.PeriodDate, sortDescending))(q).ThenByDescending(c => c.Id));
        }

        public List<AssetDepreciation> OfAsset(int assetId, PrimeDbContext db = null) =>
            Fetch(q => q.Where(c => c.AssetId == assetId).OrderBy(c => c.PeriodDate), db);

        public List<int> LinkedEntryIds() =>
            Fetch(q => q.Where(c => c.JournalEntryId != null)).Select(c => c.JournalEntryId.Value).ToList();

        public int Insert(AssetDepreciation c, PrimeDbContext db = null) => Add(c, db);

        public void Delete(int id, PrimeDbContext db = null) =>
            Write(db =>
            {
                var row = Rows(db).AsTracking().FirstOrDefault(c => c.Id == id);
                if (row != null) SetOf(db).Remove(row);
                return 0;
            }, db);

        public void SetJournalEntryId(PrimeDbContext db, int id, int journalEntryId) =>
            Write(db =>
            {
                var row = Rows(db).AsTracking().FirstOrDefault(c => c.Id == id);
                if (row != null) row.JournalEntryId = journalEntryId;
                return 0;
            }, db);
    }
}
