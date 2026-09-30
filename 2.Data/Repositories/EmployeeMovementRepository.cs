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

        Dictionary<int, decimal> SumByEmployee(int month, int year);

        int Insert(T item, PrimeDbContext db = null);
        void Update(T item, PrimeDbContext db = null);
        void Delete(int id, string deletedBy, PrimeDbContext db = null);
    }

    public interface IEmployeeAllowanceRepository : IEmployeeMovementRepository<EmployeeAllowance> { }
    public interface IEmployeeDeductionRepository : IEmployeeMovementRepository<EmployeeDeduction> { }

    public abstract class EmployeeMovementRepository<T> : RepositoryBase<T>, IEmployeeMovementRepository<T>
        where T : EmployeeMovement, new()
    {

        /// <summary>اسم الموظف وكوده عرضٌ فقط</summary>
        private static List<T> WithEmployee(List<T> rows) =>
            WithCodeNames<Employee>("Employees", rows, m => m.EmployeeId, (m, code, name) => (m.EmployeeCode, m.EmployeeName) = (code, name));

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

        public Dictionary<int, decimal> SumByEmployee(int month, int year)
        {
            using var db = DbContextFactory.Open();
            return Rows(db).AsNoTracking()
                .Where(m => m.Month == month && m.Year == year)
                .GroupBy(m => m.EmployeeId)
                .Select(g => new { g.Key, Total = g.Sum(m => m.Amount) })
                .ToDictionary(x => x.Key, x => x.Total);
        }

        public int Insert(T item, PrimeDbContext db = null) => Add(item, db);

        public void Update(T item, PrimeDbContext db = null) =>
            Modify(item, db);

        public void Delete(int id, string deletedBy, PrimeDbContext db = null) =>
            SoftDelete(id, deletedBy, db);
    }

    public class EmployeeAllowanceRepository : EmployeeMovementRepository<EmployeeAllowance>, IEmployeeAllowanceRepository
    {
        protected override string TableName => "EmployeeAllowances";
    }

    public class EmployeeDeductionRepository : EmployeeMovementRepository<EmployeeDeduction>, IEmployeeDeductionRepository
    {
        protected override string TableName => "EmployeeDeductions";
    }
}
