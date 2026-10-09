using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>البدل والخصم جدولان بشكلٍ واحد</summary>
    public interface IEmployeeMovementRepository<T> where T : EmployeeMovement
    {
        T GetById(int id, PrimeDbContext db = null);
        (List<T> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? employeeId,
            string sortColumn, bool sortDescending);

        Dictionary<int, List<T>> PendingByEmployee(int year, int month);
        void Settle(PrimeDbContext db, IEnumerable<int> ids, int payrollId);
        void Release(PrimeDbContext db, int payrollId);

        int Insert(T item, PrimeDbContext db = null);
        void Update(T item, PrimeDbContext db = null);
        void Delete(int id, string deletedBy, PrimeDbContext db = null);
    }

    public interface IEmployeeAllowanceRepository : IEmployeeMovementRepository<EmployeeAllowance> { }
    public interface IEmployeeDeductionRepository : IEmployeeMovementRepository<EmployeeDeduction> { }

    public interface IEmployeeAdvanceRepository : IEmployeeMovementRepository<EmployeeAdvance>
    {
        void SetEntry(PrimeDbContext db, int id, int? journalEntryId);
    }

    public abstract class EmployeeMovementRepository<T> : RepositoryBase<T>, IEmployeeMovementRepository<T>
        where T : EmployeeMovement, new()
    {

        /// <summary>اسم الموظف وكوده عرضٌ فقط</summary>
        private List<T> WithEmployee(List<T> rows) =>
            WithType(WithCodeNames<Employee>("Employees", rows, m => m.EmployeeId, (m, code, name) => (m.EmployeeCode, m.EmployeeName) = (code, name)));

        protected abstract List<T> WithType(List<T> rows);

        public override T GetById(int id, PrimeDbContext db = null) =>
            WithEmployee(Fetch(q => q.Where(m => m.Id == id), db)).FirstOrDefault();

        public (List<T> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? employeeId,
            string sortColumn, bool sortDescending)
        {
            using var db = DbContextFactory.Open();

            IQueryable<T> Shape(IQueryable<T> rows)
            {
                var q = rows;
                if (employeeId != null) q = q.Where(m => m.EmployeeId == employeeId);
                if (!string.IsNullOrWhiteSpace(searchText))
                    q = q.Where(m => EF.Functions.Like(m.Reason, $"%{searchText}%")
                                  || db.Employees.Any(e => e.Id == m.EmployeeId && EF.Functions.Like(e.Name, $"%{searchText}%")));
                return q;
            }

            var (items, total) = Page(page, pageSize, Shape,
                sortColumn == "Amount" ? By(m => m.Amount, sortDescending) : By(m => m.Date, sortDescending), db);
            return (WithEmployee(items), total);
        }

        public Dictionary<int, List<T>> PendingByEmployee(int year, int month) =>
            Fetch(q => q.Where(m => m.PayrollId == null && (m.Year < year || m.Year == year && m.Month <= month)))
                .GroupBy(m => m.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        public void Settle(PrimeDbContext db, IEnumerable<int> ids, int payrollId)
        {
            var list = ids.ToList();
            Set(m => list.Contains(m.Id), s => s.SetProperty(r => r.PayrollId, payrollId), db);
        }

        public void Release(PrimeDbContext db, int payrollId) =>
            Set(m => m.PayrollId == payrollId, s => s.SetProperty(r => r.PayrollId, (int?)null), db);

        public int Insert(T item, PrimeDbContext db = null) => Add(item, db);

        public void Update(T item, PrimeDbContext db = null) =>
            Modify(item, db);

        public void Delete(int id, string deletedBy, PrimeDbContext db = null) =>
            SoftDelete(id, deletedBy, db);
    }

    public class EmployeeAllowanceRepository : EmployeeMovementRepository<EmployeeAllowance>, IEmployeeAllowanceRepository
    {
        protected override string TableName => "EmployeeAllowances";

        protected override List<EmployeeAllowance> WithType(List<EmployeeAllowance> rows) =>
            WithNames<AllowanceType>("AllowanceTypes", rows, (m => m.TypeId, (m, name) => m.TypeName = name));
    }

    public class EmployeeDeductionRepository : EmployeeMovementRepository<EmployeeDeduction>, IEmployeeDeductionRepository
    {
        protected override string TableName => "EmployeeDeductions";

        protected override List<EmployeeDeduction> WithType(List<EmployeeDeduction> rows) =>
            WithNames<DeductionType>("DeductionTypes", rows, (m => m.TypeId, (m, name) => m.TypeName = name));
    }

    public class EmployeeAdvanceRepository : EmployeeMovementRepository<EmployeeAdvance>, IEmployeeAdvanceRepository
    {
        protected override string TableName => "EmployeeAdvances";

        protected override List<EmployeeAdvance> WithType(List<EmployeeAdvance> rows) =>
            WithNames<Treasury>("Treasuries", rows, (m => m.TreasuryId, (m, name) => m.TreasuryName = name));

        public void SetEntry(PrimeDbContext db, int id, int? journalEntryId) =>
            Set(m => m.Id == id, s => s.SetProperty(r => r.JournalEntryId, journalEntryId), db);
    }
}
