using PrimeERP.Domain.Calculations;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Data.Repositories.Base
{
    /// <summary>ما يتقاسمه مستودعا العملاء والموردين</summary>
    public abstract class PartyRepositoryBase<T> : RepositoryBase<T> where T : PartyBase, new()
    {

        public T GetByCode(string code) => One(q => q.Where(p => p.Code == code));

        public T GetByAccountCode(string accountCode, PrimeDbContext db = null) =>
            One(q => q.Where(p => p.AccountCode == accountCode), db);

        public List<T> GetAll(bool activeOnly = true) =>
            Fetch(q => q.Where(p => !activeOnly || p.IsActive).OrderBy(p => p.Name));

        public List<T> Search(string term, int maxResults) =>
            Fetch(q => q
                .Where(p => p.IsActive && (EF.Functions.Like(p.Name, $"%{term}%")
                                        || EF.Functions.Like(p.Code, $"%{term}%")
                                        || EF.Functions.Like(p.Phone, $"%{term}%")))
                .OrderBy(p => p.Name)
                .Take(maxResults));

        public int CountAll(bool activeOnly = true) =>
            Count(q => q.Where(p => !activeOnly || p.IsActive));

        public bool ExistsCode(string code, int? excludeId = null) =>
            Any(q => q.Where(p => p.Code == code && (excludeId == null || p.Id != excludeId)));

        public bool ExistsPhone(string phone, int? excludeId = null) =>
            !string.IsNullOrWhiteSpace(phone) &&
            Any(q => q.Where(p => p.Phone == phone && (excludeId == null || p.Id != excludeId)));

        public bool ExistsName(string name, int? excludeId = null) =>
            Any(q => q.Where(p => p.Name == name && (excludeId == null || p.Id != excludeId)));

        public (List<T> Items, int Total) GetPaged(
            int page, int pageSize,
            string searchText = null, bool? isActive = null, bool? hasBalance = null, bool? overCreditLimit = null,
            int? categoryId = null, string sortColumn = "Name", bool sortDescending = false)
        {
            IQueryable<T> Shape(IQueryable<T> rows)
            {
                var q = rows;
                if (!string.IsNullOrWhiteSpace(searchText))
                    q = q.Where(p => EF.Functions.Like(p.Name, $"%{searchText}%")
                                  || EF.Functions.Like(p.Code, $"%{searchText}%")
                                  || EF.Functions.Like(p.Phone, $"%{searchText}%"));
                if (isActive != null) q = q.Where(p => p.IsActive == isActive);
                if (hasBalance == true) q = q.Where(p => p.Balance != 0);
                if (hasBalance == false) q = q.Where(p => p.Balance == 0);
                if (overCreditLimit != null) q = q.Where(PartyCalc.OverCreditLimit<T>(overCreditLimit.Value));
                if (categoryId != null) q = q.Where(p => p.CategoryId == categoryId);
                return q;
            }

            var (items, total) = Page(page, pageSize, Shape, Order(sortColumn, sortDescending));

            return (WithCategoryNames(items, (p => p.CategoryId, (p, name) => p.CategoryName = name)), total);
        }

        private static Func<IQueryable<T>, IOrderedQueryable<T>> Order(string column, bool descending) => column switch
        {
            "Name"        => By(p => p.Name, descending),
            "Balance"     => By(p => p.Balance, descending),
            "CreditLimit" => By(p => p.CreditLimit, descending),
            "CreatedAt"   => By(p => p.CreatedAt, descending),
            _             => By(p => p.Code, descending),
        };

        public int Insert(T party, PrimeDbContext db = null) => Add(party, db);

        public void Update(T party, PrimeDbContext db = null) =>
            Modify(party, db, nameof(PartyBase.Balance), nameof(PartyBase.Code));

        public void UpdateNameByAccountCode(PrimeDbContext db, string accountCode, string name) =>
            Set(p => p.AccountCode == accountCode, s => s.SetProperty(r => r.Name, name), db);

        public void Delete(int id, string deletedBy, PrimeDbContext db = null) =>
            SoftDelete(id, deletedBy, db);

        public void SetBalance(int id, decimal balance, PrimeDbContext db = null) =>
            Set(p => p.Id == id, s => s.SetProperty(r => r.Balance, balance), db);
    }
}
