using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع Payroll</summary>
    public interface IPayrollRepository
    {
        void DeleteDocument(PrimeDbContext db, int id);
        Payroll GetById(int id, PrimeDbContext db = null);
        List<PayrollLine> GetLines(int payrollId, PrimeDbContext db = null);
        (List<Payroll> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending);
        int InsertHeader(PrimeDbContext db, Payroll payroll);
        void InsertLine(PrimeDbContext db, int payrollId, PayrollLine line);
        void SetJournalEntryId(PrimeDbContext db, int payrollId, int? journalEntryId);
        void SetPosted(PrimeDbContext db, int payrollId, bool posted);
        DateTime? LastPeriodStart();
        bool HasPeriod(DateTime start);
        List<Payroll> Between(DateTime from, DateTime to);
        List<(int Year, int Month, decimal Gross, decimal Deductions, decimal Net)> Monthly(DateTime from, DateTime to);
        List<(int EmployeeId, decimal Gross, decimal Net)> CostByEmployee(DateTime from, DateTime to);
    }

    public class PayrollRepository : RepositoryBase<Payroll>, IPayrollRepository
    {
        protected override string TableName => "Payrolls";


        protected override string LineTable => "PayrollLines";
        protected override string LineForeignKey => "PayrollId";

        public void DeleteDocument(PrimeDbContext db, int id)
        {
            RemoveIn<PayrollLine>(LineTable, l => l.PayrollId == id, db);

            Remove(p => p.Id == id, db);
        }


        public List<PayrollLine> GetLines(int payrollId, PrimeDbContext db = null) =>
            FetchOf<PayrollLine>(LineTable, q => q.Where(l => l.PayrollId == payrollId), db);

        public (List<Payroll> Items, int Total) GetPaged(int page, int pageSize, string searchText,
                                                         string sortColumn, bool sortDescending)
        {
            IQueryable<Payroll> Shape(IQueryable<Payroll> rows) =>
                string.IsNullOrWhiteSpace(searchText)
                    ? rows
                    : rows.Where(p => EF.Functions.Like(p.PayrollNo, $"%{searchText}%"));

            return Page(page, pageSize, Shape,
                sortColumn == "PayrollNo" ? DocumentOrder(p => p.PayrollNo,   sortDescending, p => p.PayrollNo)
                                          : DocumentOrder(p => p.PaymentDate, sortDescending, p => p.PayrollNo));
        }

        public int InsertHeader(PrimeDbContext db, Payroll payroll) => Add(payroll, db);

        public void InsertLine(PrimeDbContext db, int payrollId, PayrollLine line) =>
            Write(db =>
            {
                line.PayrollId = payrollId;
                SetOf<PayrollLine>(db, LineTable).Add(line);
                return 0;
            }, db);

        public void SetJournalEntryId(PrimeDbContext db, int payrollId, int? journalEntryId) =>
            Set(p => p.Id == payrollId, s => s.SetProperty(r => r.JournalEntryId, journalEntryId), db);

        public void SetPosted(PrimeDbContext db, int payrollId, bool posted) =>
            Set(p => p.Id == payrollId, s => s.SetProperty(r => r.IsPosted, posted), db);

        public DateTime? LastPeriodStart() => One(q => q.OrderByDescending(p => p.PeriodStart))?.PeriodStart;

        public bool HasPeriod(DateTime start) => Any(q => q.Where(p => p.PeriodStart == start));

        private IQueryable<Payroll> Posted(PrimeDbContext ctx, DateTime from, DateTime to) =>
            Rows(ctx).AsNoTracking().Where(p => p.IsPosted && p.PaymentDate >= from.Date && p.PaymentDate < to.Date.AddDays(1));

        public List<(int Year, int Month, decimal Gross, decimal Deductions, decimal Net)> Monthly(DateTime from, DateTime to) =>
            Scope(null, ctx => Posted(ctx, from, to)
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Gross = g.Sum(p => p.TotalBasic + p.TotalAllowances), Deductions = g.Sum(p => p.TotalDeductions), Net = g.Sum(p => p.NetTotal) })
                .AsEnumerable().Select(x => (x.Year, x.Month, x.Gross, x.Deductions, x.Net)).ToList());

        public List<(int EmployeeId, decimal Gross, decimal Net)> CostByEmployee(DateTime from, DateTime to) =>
            Scope(null, ctx => Posted(ctx, from, to)
                .Join(RowsOf<PayrollLine>(ctx, LineTable).AsNoTracking(), p => p.Id, l => l.PayrollId, (p, l) => l)
                .GroupBy(l => l.EmployeeId)
                .Select(g => new { g.Key, Gross = g.Sum(l => l.BasicSalary + l.Allowances + l.Overtime), Net = g.Sum(l => l.NetSalary) })
                .AsEnumerable().Select(x => (x.Key, x.Gross, x.Net)).ToList());

        public List<Payroll> Between(DateTime from, DateTime to) =>
            Fetch(q => q.Where(p => p.IsPosted && p.PaymentDate >= from && p.PaymentDate <= to).OrderBy(p => p.PaymentDate));
    }
}
