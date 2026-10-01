using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع Asset</summary>
    public interface IAssetRepository
    {
        Asset GetById(int id, PrimeDbContext db = null);
        List<Asset> Depreciable(PrimeDbContext db = null);
        bool AnyInCategory(int categoryId);
        List<Asset> Search(string term, int maxResults);
        (List<Asset> Items, int Total) GetPaged(
            int page, int pageSize, string searchText = null, bool? isActive = null, int? categoryId = null,
            string sortColumn = "Name", bool sortDescending = false);

        int Insert(Asset a, PrimeDbContext db = null);
        void Update(Asset a, PrimeDbContext db = null);
        void Edit(Asset a, PrimeDbContext db = null);
        void Delete(int id, string deletedBy, PrimeDbContext db = null);
        void SetJournalEntryId(PrimeDbContext db, int id, int journalEntryId);
        void SetActive(PrimeDbContext db, int id, bool active);
    }

    public class AssetRepository : RepositoryBase<Asset>, IAssetRepository
    {
        protected override string TableName => "Assets";




        public List<Asset> Search(string term, int maxResults) =>
            Fetch(q => q.Where(a => a.IsActive
                                 && (EF.Functions.Like(a.Name, $"%{term}%") || EF.Functions.Like(a.Code, $"%{term}%")))
                        .OrderBy(a => a.Name).Take(maxResults));

        public (List<Asset> Items, int Total) GetPaged(
            int page, int pageSize, string searchText = null, bool? isActive = null, int? categoryId = null,
            string sortColumn = "Name", bool sortDescending = false)
        {
            IQueryable<Asset> Shape(IQueryable<Asset> rows)
            {
                var q = rows;
                if (!string.IsNullOrWhiteSpace(searchText))
                    q = q.Where(a => EF.Functions.Like(a.Name, $"%{searchText}%")
                                  || EF.Functions.Like(a.Code, $"%{searchText}%"));
                if (isActive != null) q = q.Where(a => a.IsActive == isActive);
                if (categoryId != null) q = q.Where(a => a.CategoryId == categoryId);
                return q;
            }

            var (items, total) = Page(page, pageSize, Shape, sortColumn switch
            {
                "Name"         => By(a => a.Name, sortDescending),
                "PurchaseDate" => By(a => a.PurchaseDate, sortDescending),
                "CreatedAt"    => By(a => a.CreatedAt, sortDescending),
                _              => By(a => a.Code, sortDescending),
            });

            return (WithCategoryNames(items, (a => a.CategoryId, (a, name) => a.CategoryName = name)), total);
        }

        public List<Asset> Depreciable(PrimeDbContext db = null) =>
            Fetch(q => q.Where(a => a.IsActive && a.UsefulLifeYears > 0
                                 && a.DepreciationAccountCode != null && a.DepreciationAccountCode != ""), db);

        public bool AnyInCategory(int categoryId) => Any(q => q.Where(a => a.CategoryId == categoryId));

        public int Insert(Asset a, PrimeDbContext db = null) => Add(a, db);

        public void Update(Asset a, PrimeDbContext db = null) =>
            Modify(a, db);

        /// <summary>تعديل المستخدم عدا حقول النظام</summary>
        public void Edit(Asset a, PrimeDbContext db = null) =>
            Modify(a, db, nameof(Asset.Code), nameof(Asset.AccountCode), nameof(Asset.DepreciationAccountCode),
                nameof(Asset.AccumulatedDepreciation), nameof(Asset.RevaluedValue), nameof(Asset.LastDepreciationDate),
                nameof(Asset.JournalEntryId));

        public void Delete(int id, string deletedBy, PrimeDbContext db = null) =>
            SoftDelete(id, deletedBy, db);

        public void SetJournalEntryId(PrimeDbContext db, int id, int journalEntryId) =>
            Set(a => a.Id == id, s => s.SetProperty(r => r.JournalEntryId, journalEntryId), db);

        public void SetActive(PrimeDbContext db, int id, bool active) =>
            Set(a => a.Id == id, s => s.SetProperty(r => r.IsActive, active), db);
    }
}
