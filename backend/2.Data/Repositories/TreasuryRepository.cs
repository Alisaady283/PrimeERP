using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع Treasury</summary>
    public interface ITreasuryRepository
    {
        List<Treasury> GetAll(bool includeInactive = false);
        List<Treasury> Search(string term, int maxResults);
        Treasury GetById(int id, PrimeDbContext db = null);
        Dictionary<int, string> NamesOf(IEnumerable<int> ids, PrimeDbContext db = null);
        int Insert(Treasury t, PrimeDbContext db = null);
        void Update(Treasury t, PrimeDbContext db = null);
        void Delete(int id, PrimeDbContext db = null);
        Treasury GetByAccountCode(string accountCode, PrimeDbContext db = null);
        void UpdateNameByAccountCode(PrimeDbContext db, string accountCode, string name);
    }

    public class TreasuryRepository : RepositoryBase<Treasury>, ITreasuryRepository
    {
        protected override string TableName => "Treasuries";


        public List<Treasury> GetAll(bool includeInactive = false) =>
            Fetch(q => q.Where(t => includeInactive || t.IsActive).OrderBy(t => t.Code));

        public List<Treasury> Search(string term, int maxResults) =>
            Fetch(q => q.Where(t => t.IsActive && EF.Functions.Like(t.Name, $"%{term}%")).OrderBy(t => t.Code).Take(maxResults));


        public Treasury GetByAccountCode(string accountCode, PrimeDbContext db = null) =>
            One(q => q.Where(t => t.AccountCode == (accountCode ?? "")), db);

        public int Insert(Treasury t, PrimeDbContext db = null) => Add(t, db);

        public void Update(Treasury t, PrimeDbContext db = null) =>
            Edit(x => x.Id == t.Id, row =>
            {
                row.Name = t.Name;
                row.Kind = t.Kind;
                row.AccountCode = t.AccountCode ?? "";
                row.BankName = t.BankName ?? "";
                row.AccountNumber = t.AccountNumber ?? "";
                row.Notes = t.Notes ?? "";
                row.IsActive = t.IsActive;
            }, db);

        public void UpdateNameByAccountCode(PrimeDbContext db, string accountCode, string name) =>
            Set(t => t.AccountCode == (accountCode ?? ""), s => s.SetProperty(r => r.Name, name ?? ""), db);

        public void Delete(int id, PrimeDbContext db = null) =>
            Set(t => t.Id == id, s => s.SetProperty(r => r.IsActive, false), db);
    }
}
