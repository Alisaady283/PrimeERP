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
    }
}
