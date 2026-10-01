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

        (decimal Total, DateTime? Last) TotalOf(int assetId, PrimeDbContext db = null);

        List<int> LinkedEntryIds();

        int Insert(AssetDepreciation charge, PrimeDbContext db = null);
        void Delete(int id, PrimeDbContext db = null);
        void SetJournalEntryId(PrimeDbContext db, int id, int journalEntryId);
    }

    public class AssetDepreciationRepository : RepositoryBase<AssetDepreciation>, IAssetDepreciationRepository
    {
        protected override string TableName => "AssetDepreciations";

        /// <summary>كود الأصل واسمه عرضٌ فقط</summary>
        private static List<AssetDepreciation> WithAsset(List<AssetDepreciation> rows) =>
            WithCodeNames<Asset>("Assets", rows, c => c.AssetId, (c, code, name) => (c.AssetCode, c.AssetName) = (code, name));

        public override AssetDepreciation GetById(int id, PrimeDbContext db = null) =>
            WithAsset(Fetch(q => q.Where(c => c.Id == id), db)).FirstOrDefault();



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

            var (items, total) = Page(page, pageSize, Shape, q => (sortColumn == "Amount" ? By(c => c.Amount, sortDescending)
                                         : By(c => c.PeriodDate, sortDescending))(q).ThenByDescending(c => c.Id));
            return (WithAsset(items), total);
        }

        /// <summary>مجموع أقساط الأصل وآخرها</summary>
        public (decimal Total, DateTime? Last) TotalOf(int assetId, PrimeDbContext db = null) =>
            Scope(db, ctx =>
            {
                var charges = Rows(ctx).Where(c => c.AssetId == assetId);
                return (charges.Sum(c => (decimal?)c.Amount) ?? 0m, charges.Max(c => (DateTime?)c.PeriodDate));
            });

        public List<int> LinkedEntryIds() =>
            Fetch(q => q.Where(c => c.JournalEntryId != null)).Select(c => c.JournalEntryId.Value).ToList();

        public int Insert(AssetDepreciation c, PrimeDbContext db = null) => Add(c, db);

        public void Delete(int id, PrimeDbContext db = null) =>
            Remove(c => c.Id == id, db);

        public void SetJournalEntryId(PrimeDbContext db, int id, int journalEntryId) =>
            Set(c => c.Id == id, s => s.SetProperty(r => r.JournalEntryId, journalEntryId), db);
    }
}
