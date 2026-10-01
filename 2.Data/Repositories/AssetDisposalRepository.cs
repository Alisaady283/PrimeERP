using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع AssetDisposal</summary>
    public interface IAssetDisposalRepository
    {
        AssetDisposal GetById(int id, PrimeDbContext db = null);
        bool AnyForAsset(int assetId, PrimeDbContext db = null);
        (List<AssetDisposal> Items, int Total) GetPaged(int page, int pageSize, string searchText = null,
            int? assetId = null, string sortColumn = "DisposalDate", bool sortDescending = true);

        int Insert(AssetDisposal disposal, PrimeDbContext db = null);
        void Update(AssetDisposal disposal, PrimeDbContext db = null);
        void Delete(int id, string deletedBy, PrimeDbContext db = null);
        void SetJournalEntryId(PrimeDbContext db, int id, int journalEntryId);
    }

    public class AssetDisposalRepository : RepositoryBase<AssetDisposal>, IAssetDisposalRepository
    {
        protected override string TableName => "AssetDisposals";

        /// <summary>كود الأصل واسمه عرضٌ فقط</summary>
        private static List<AssetDisposal> WithAsset(List<AssetDisposal> rows) =>
            WithCodeNames<Asset>("Assets", rows, x => x.AssetId, (x, code, name) => (x.AssetCode, x.AssetName) = (code, name));

        public override AssetDisposal GetById(int id, PrimeDbContext db = null) =>
            WithAsset(Fetch(q => q.Where(x => x.Id == id), db)).FirstOrDefault();



        public (List<AssetDisposal> Items, int Total) GetPaged(int page, int pageSize, string searchText = null,
            int? assetId = null, string sortColumn = "DisposalDate", bool sortDescending = true)
        {
            IQueryable<AssetDisposal> Shape(IQueryable<AssetDisposal> rows)
            {
                var q = rows;
                if (!string.IsNullOrWhiteSpace(searchText))
                    q = q.Where(x => EF.Functions.Like(x.Notes, $"%{searchText}%"));
                if (assetId != null) q = q.Where(x => x.AssetId == assetId);
                return q;
            }

            var (items, total) = Page(page, pageSize, Shape, q => (By(x => x.DisposalDate, sortDescending))(q).ThenByDescending(x => x.Id));
            return (WithAsset(items), total);
        }

        public bool AnyForAsset(int assetId, PrimeDbContext db = null) => Any(q => q.Where(x => x.AssetId == assetId), db);

        public int Insert(AssetDisposal d, PrimeDbContext db = null) => Add(d, db);

        public void Update(AssetDisposal d, PrimeDbContext db = null) =>
            Edit(x => x.Id == d.Id, row =>
            {
                row.AssetId = d.AssetId;
                row.DisposalDate = d.DisposalDate;
                row.TreasuryId = d.TreasuryId;
                row.SalePrice = d.SalePrice;
                row.AssetValue = d.AssetValue;
                row.AccumulatedDepreciation = d.AccumulatedDepreciation;
                row.Notes = d.Notes ?? "";
                row.UpdatedBy = d.UpdatedBy;
            }, db);

        public void Delete(int id, string deletedBy, PrimeDbContext db = null) =>
            SoftDelete(id, deletedBy, db);

        public void SetJournalEntryId(PrimeDbContext db, int id, int journalEntryId) =>
            Set(x => x.Id == id, s => s.SetProperty(r => r.JournalEntryId, journalEntryId), db);
    }
}
