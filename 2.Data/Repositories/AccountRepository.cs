using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع Account</summary>
    public interface IAccountRepository
    {
        bool HasAny();
        void InsertMany(IEnumerable<Account> accounts);
        List<Account> GetAll(bool includeInactive = false);
        Account GetById(int id, PrimeDbContext db = null);
        Account GetByCode(string code, PrimeDbContext db = null);
        List<Account> GetByCodes(IEnumerable<string> codes, PrimeDbContext db = null);
        List<Account> GetChildren(string parentCode, PrimeDbContext db = null);
        List<Account> Find(string searchText, int? level, int? type, bool leafOnly, bool includeInactive);
        (List<Account> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? level, int? type,
            bool leafOnly, bool includeInactive);
        HashSet<string> CodesWithChildren(IEnumerable<string> codes, bool includeInactive);

        List<Account> GetAllChildren(string parentCode, PrimeDbContext db = null);
        List<Account> GetLeaves();
        Account LeafNamed(string rootCode, string name, IEnumerable<string> except = null);
        int GetLevel(string code);
        int GetTypeOf(string code);
        bool HasChildren(string code);
        bool HasChildren(string code, PrimeDbContext db);
        int Insert(Account a, PrimeDbContext db = null);
        void Update(Account a, PrimeDbContext db = null);
        void UpdateName(PrimeDbContext db, string code, string name);
        void SetIsLeaf(string code, bool isLeaf, PrimeDbContext db = null);
        void Delete(string code, PrimeDbContext db = null);
        void UpdateBalance(string code, decimal balance, PrimeDbContext db = null);
    }

    /// <summary>طبقة وصول بيانات شجرة الحسابات</summary>
    public class AccountRepository : RepositoryBase<Account>, IAccountRepository
    {
        protected override string TableName => "Accounts";

        public bool HasAny() => Any(q => q);

        public List<Account> GetAll(bool includeInactive = false) =>
            Fetch(q => q.Where(a => includeInactive || a.IsActive).OrderBy(a => a.Code));

        public Account GetByCode(string code, PrimeDbContext db = null) =>
            One(q => q.Where(a => a.Code == code), db);

        public List<Account> GetChildren(string parentCode, PrimeDbContext db = null) =>
            Fetch(q => q.Where(a => a.ParentCode == parentCode && a.IsActive).OrderBy(a => a.Code), db);

        public List<Account> Find(string searchText, int? level, int? type, bool leafOnly, bool includeInactive) =>
            Fetch(q => Matching(q, searchText, level, type, leafOnly, includeInactive).OrderBy(a => a.Code));

        public (List<Account> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? level, int? type,
            bool leafOnly, bool includeInactive) =>
            Page(page, pageSize, q => Matching(q, searchText, level, type, leafOnly, includeInactive), By(a => a.Code, false));

        private static IQueryable<Account> Matching(IQueryable<Account> rows, string searchText, int? level, int? type,
            bool leafOnly, bool includeInactive)
        {
            var q = rows.Where(a => includeInactive || a.IsActive);
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var term = searchText.Trim();
                q = q.Where(a => EF.Functions.Like(a.Code, $"%{term}%") || EF.Functions.Like(a.Name, $"%{term}%"));
            }
            if (level != null) q = q.Where(a => a.Level == level);
            if (type != null) q = q.Where(a => a.Type == type);
            if (leafOnly) q = q.Where(a => a.IsLeaf);
            return q;
        }

        /// <summary>أيّ هذه الحسابات له أبناء</summary>
        public HashSet<string> CodesWithChildren(IEnumerable<string> codes, bool includeInactive)
        {
            var wanted = codes.Distinct().ToList();
            return Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(a => wanted.Contains(a.ParentCode) && (includeInactive || a.IsActive))
                .Select(a => a.ParentCode).Distinct().ToHashSet());
        }

        /// <summary>حسابات القيد بضمّةٍ واحدة</summary>
        public List<Account> GetByCodes(IEnumerable<string> codes, PrimeDbContext db = null)
        {
            var wanted = codes.Distinct().ToList();
            return Fetch(q => q.Where(a => wanted.Contains(a.Code)), db);
        }

        public List<Account> GetAllChildren(string parentCode, PrimeDbContext db = null) =>
            Fetch(q => q.Where(a => a.ParentCode == parentCode).OrderBy(a => a.Code), db);

        /// <summary>ورقةٌ باسمها تحت جذر</summary>
        public Account LeafNamed(string rootCode, string name, IEnumerable<string> except = null)
        {
            var skip = except?.ToList() ?? new List<string>();
            return One(q => q.Where(a => a.IsActive && a.IsLeaf && a.Name == name && a.Code.StartsWith(rootCode) && !skip.Contains(a.Code))
                             .OrderBy(a => a.Code));
        }

        public List<Account> GetLeaves() =>
            Fetch(q => q.Where(a => a.IsLeaf && a.IsActive).OrderBy(a => a.Code));

        public int GetLevel(string code) => GetByCode(code)?.Level ?? 1;

        public int GetTypeOf(string code) => GetByCode(code)?.Type ?? 1;

        public bool HasChildren(string code) => HasChildren(code, null);

        public bool HasChildren(string code, PrimeDbContext db) =>
            Any(q => q.Where(a => a.ParentCode == code && a.IsActive), db);

        public int Insert(Account a, PrimeDbContext db = null) => Add(a, db);

        public void InsertMany(IEnumerable<Account> accounts)
        {
            var rows = accounts.ToList();
            if (rows.Count == 0) return;

            Write(db =>
            {
                SetOf(db).AddRange(rows);
                return 0;
            });
        }

        public void Update(Account a, PrimeDbContext db = null) =>
            Edit(x => x.Code == a.Code, row =>
            {
                row.Name = a.Name;
                row.Notes = a.Notes ?? "";
                row.IsLeaf = a.IsLeaf;
                row.IsActive = a.IsActive;
            }, db);

        public void UpdateName(PrimeDbContext db, string code, string name) =>
            Set(a => a.Code == code, s => s.SetProperty(r => r.Name, name), db);

        public void SetIsLeaf(string code, bool isLeaf, PrimeDbContext db = null) =>
            Set(a => a.Code == code, s => s.SetProperty(r => r.IsLeaf, isLeaf), db);

        public void Delete(string code, PrimeDbContext db = null) =>
            Set(a => a.Code == code, s => s.SetProperty(r => r.IsActive, false), db);

        public void UpdateBalance(string code, decimal balance, PrimeDbContext db = null) =>
            Set(a => a.Code == code, s => s.SetProperty(r => r.Balance, balance), db);
    }
}
