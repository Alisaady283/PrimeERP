using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع License</summary>
    public interface ILicenseRepository
    {
        List<License> GetAll(bool includeInactive = false);
        License GetById(int id, PrimeDbContext db = null);
        License GetBySerial(string serial);
        int  Insert(License license);
        void Update(License license);
        void Delete(int id);
        void Clear(PrimeDbContext db = null);
    }

    public class LicenseRepository : RepositoryBase<License>, ILicenseRepository
    {
        protected override string TableName => "Licenses";


        public List<License> GetAll(bool includeInactive = false) =>
            Fetch(q => q.Where(l => includeInactive || l.IsActive).OrderBy(l => l.CustomerName));


        public License GetBySerial(string serial) => One(q => q.Where(l => l.Serial == serial));

        public int Insert(License l)
        {
            return Add(l);
        }

        public void Update(License l) =>
            Write(db =>
            {
                var row = Rows(db).AsTracking().FirstOrDefault(x => x.Id == l.Id);
                if (row == null) return 0;
                row.CustomerName = l.CustomerName;
                row.Location = l.Location ?? "";
                row.Manifest = l.Manifest ?? "";
                row.Simplified = l.Simplified;
                row.Version = l.Version;
                row.MachineHash = l.MachineHash ?? "";
                row.IsActive = l.IsActive;
                return 0;
            });

        public void Delete(int id) =>
            SoftDelete(id, null);

        public void Clear(PrimeDbContext db = null) =>
            Scope(db, ctx => Rows(ctx).IgnoreQueryFilters().ExecuteDelete());

    }
}
