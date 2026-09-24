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
        Treasury GetById(int id, PrimeDbContext db = null);
        int Insert(Treasury t, PrimeDbContext db = null);
        void Update(Treasury t, PrimeDbContext db = null);
        void Delete(int id, PrimeDbContext db = null);
        Treasury GetByAccountCode(string accountCode, PrimeDbContext db = null);
        void UpdateNameByAccountCode(PrimeDbContext db, string accountCode, string name);
        void DeleteByAccountCode(PrimeDbContext db, string accountCode);
    }

    public class TreasuryRepository : RepositoryBase<Treasury>, ITreasuryRepository
    {
        protected override string TableName => "Treasuries";


        public List<Treasury> GetAll(bool includeInactive = false) =>
            Fetch(q => q.Where(t => includeInactive || t.IsActive).OrderBy(t => t.Code));


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
            Write(db =>
            {
                foreach (var row in Rows(db).AsTracking().Where(t => t.AccountCode == (accountCode ?? "")))
                    row.Name = name ?? "";
                return 0;
            }, db);

        public void DeleteByAccountCode(PrimeDbContext db, string accountCode) =>
            Write(db =>
            {
                foreach (var row in Rows(db).AsTracking().Where(t => t.AccountCode == (accountCode ?? "")))
                    row.IsActive = false;
                return 0;
            }, db);

        public void Delete(int id, PrimeDbContext db = null) =>
            Write(db =>
            {
                var row = Rows(db).AsTracking().FirstOrDefault(t => t.Id == id);
                if (row != null) row.IsActive = false;
                return 0;
            }, db);
    }
}
