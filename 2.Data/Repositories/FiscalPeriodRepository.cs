using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع FiscalPeriod</summary>
    public interface IFiscalPeriodRepository
    {

        List<FiscalYear> GetAllYears();
        FiscalYear GetYearById(int id);
        FiscalYear GetCurrentYear();
        FiscalYear GetYearContaining(string date);
        bool AnyYearOverlapping(string start, string end, int? excludeId);
        int InsertYear(PrimeDbContext db, FiscalYear year);
        void UpdateYear(PrimeDbContext db, FiscalYear year);
        void SetYearClosed(PrimeDbContext db, int id, DateTime closedAt, string closedBy, int? closingEntryId);
        void SetYearReopened(PrimeDbContext db, int id);
        void SetCurrentYear(PrimeDbContext db, int id);

        List<FiscalPeriod> GetPeriods(int yearId);
        FiscalPeriod GetPeriodById(int id);
        FiscalPeriod GetPeriodContaining(string date);
        void InsertPeriod(PrimeDbContext db, FiscalPeriod period);
        void UpdatePeriod(PrimeDbContext db, FiscalPeriod period);
        void SetPeriodClosed(PrimeDbContext db, int id, DateTime closedAt, string closedBy);
        void SetPeriodReopened(PrimeDbContext db, int id);
    }

    /// <summary>طبقة وصول بيانات السنوات/الفترات المالية</summary>
    public class FiscalPeriodRepository : RepositoryBase<FiscalYear>, IFiscalPeriodRepository
    {
        protected override string TableName => "FiscalYears";





        private const string Periods = "FiscalPeriods";

        public List<FiscalYear> GetAllYears() =>
            Fetch(q => q.Where(y => !y.IsDeleted).OrderByDescending(y => y.StartDate));

        public FiscalYear GetYearById(int id) => GetById(id);

        public FiscalYear GetCurrentYear() => One(q => q.Where(y => y.IsCurrent && !y.IsDeleted));

        public FiscalYear GetYearContaining(string date) =>
            One(q => q.Where(y => !y.IsDeleted
                               && string.Compare(date, y.StartDate) >= 0
                               && string.Compare(date, y.EndDate) <= 0));

        public bool AnyYearOverlapping(string start, string end, int? excludeId) =>
            Count(q => q.Where(y => !y.IsDeleted
                                 && string.Compare(y.StartDate, end) <= 0
                                 && string.Compare(y.EndDate, start) >= 0
                                 && (excludeId == null || y.Id != excludeId))) > 0;

        public int InsertYear(PrimeDbContext db, FiscalYear year) => Add(year, db);

        public void UpdateYear(PrimeDbContext db, FiscalYear year) =>
            Edit(y => y.Id == year.Id, row =>
            {
                row.Name = year.Name;
                row.StartDate = year.StartDate;
                row.EndDate = year.EndDate;
            }, db);

        public void SetYearClosed(PrimeDbContext db, int id, DateTime closedAt, string closedBy,
                                  int? closingEntryId) =>
            Edit(y => y.Id == id, row =>
            {
                row.IsClosed = true;
                row.ClosedAt = closedAt;
                row.ClosedBy = closedBy;
                row.ClosingEntryId = closingEntryId;
            }, db);

        public void SetYearReopened(PrimeDbContext db, int id) =>
            Edit(y => y.Id == id, row =>
            {
                row.IsClosed = false;
                row.ClosedAt = null;
                row.ClosedBy = null;
                row.ClosingEntryId = null;
            }, db);

        public void SetCurrentYear(PrimeDbContext db, int id) =>
            Write(db =>
            {
                foreach (var row in Rows(db).AsTracking())
                    row.IsCurrent = row.Id == id;
                return 0;
            }, db);

        public List<FiscalPeriod> GetPeriods(int yearId) =>
            FetchOf<FiscalPeriod>(Periods, q => q.Where(p => p.FiscalYearId == yearId).OrderBy(p => p.PeriodNo));

        public FiscalPeriod GetPeriodById(int id) =>
            FetchOf<FiscalPeriod>(Periods, q => q.Where(p => p.Id == id).Take(1)).FirstOrDefault();

        public FiscalPeriod GetPeriodContaining(string date) =>
            FetchOf<FiscalPeriod>(Periods, q => q.Where(p => string.Compare(date, p.StartDate) >= 0
                                                          && string.Compare(date, p.EndDate) <= 0).Take(1))
                .FirstOrDefault();

        public void InsertPeriod(PrimeDbContext db, FiscalPeriod period) =>
            Write(db => { SetOf<FiscalPeriod>(db, Periods).Add(period); return 0; }, db);

        public void UpdatePeriod(PrimeDbContext db, FiscalPeriod period) =>
            Write(db =>
            {
                var row = RowsOf<FiscalPeriod>(db, Periods).AsTracking().FirstOrDefault(p => p.Id == period.Id);
                if (row == null) return 0;
                row.Name = period.Name;
                row.StartDate = period.StartDate;
                row.EndDate = period.EndDate;
                return 0;
            }, db);

        public void SetPeriodClosed(PrimeDbContext db, int id, DateTime closedAt, string closedBy) =>
            Write(db =>
            {
                var row = RowsOf<FiscalPeriod>(db, Periods).AsTracking().FirstOrDefault(p => p.Id == id);
                if (row == null) return 0;
                row.IsClosed = true;
                row.ClosedAt = closedAt;
                row.ClosedBy = closedBy;
                return 0;
            }, db);

        public void SetPeriodReopened(PrimeDbContext db, int id) =>
            Write(db =>
            {
                var row = RowsOf<FiscalPeriod>(db, Periods).AsTracking().FirstOrDefault(p => p.Id == id);
                if (row == null) return 0;
                row.IsClosed = false;
                row.ClosedAt = null;
                row.ClosedBy = null;
                return 0;
            }, db);
    }
}
