using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع Employee</summary>
    public interface IEmployeeRepository
    {
        Employee GetById(int id, PrimeDbContext db = null);
        Employee GetByCode(string code, PrimeDbContext db = null);
        Dictionary<string, Employee> ByCodes(IEnumerable<string> codes, PrimeDbContext db = null);
        List<Employee> GetByIds(IEnumerable<int> ids, PrimeDbContext db = null);

        Employee GetByAccountCode(string accountCode, PrimeDbContext db = null);
        void UpdateNameByAccountCode(PrimeDbContext db, string accountCode, string name);
        List<Employee> GetAll(bool activeOnly = true);
        List<Employee> Search(string term, int maxResults);
        (List<Employee> Items, int Total) GetPaged(
            int page, int pageSize, string searchText = null, int? departmentId = null,
            string sortColumn = "Name", bool sortDescending = false);

        int Insert(Employee e, PrimeDbContext db = null);
        void Update(Employee e, PrimeDbContext db = null);
        void Delete(int id, string deletedBy, PrimeDbContext db = null);
    }

    public class EmployeeRepository : RepositoryBase<Employee>, IEmployeeRepository
    {
        protected override string TableName => "Employees";




        public Employee GetByCode(string code, PrimeDbContext db = null) =>
            One(q => q.Where(e => e.Code == code), db);

        public Employee GetByAccountCode(string accountCode, PrimeDbContext db = null) =>
            One(q => q.Where(e => e.AccountCode == accountCode), db);

        public List<Employee> Search(string term, int maxResults) =>
            Fetch(q => q.Where(e => (EF.Functions.Like(e.Name, $"%{term}%")
                                                  || EF.Functions.Like(e.Code, $"%{term}%")))
                        .OrderBy(e => e.Name).Take(maxResults));

        public List<Employee> GetAll(bool activeOnly = true) =>
            Fetch(q => q.Where(e => !activeOnly || e.Status == EmployeeStatus.Active)
                        .OrderBy(e => e.Code));

        public (List<Employee> Items, int Total) GetPaged(
            int page, int pageSize, string searchText = null, int? departmentId = null,
            string sortColumn = "Name", bool sortDescending = false)
        {
            IQueryable<Employee> Shape(IQueryable<Employee> rows)
            {
                var q = rows;
                if (!string.IsNullOrWhiteSpace(searchText))
                    q = q.Where(e => EF.Functions.Like(e.Name, $"%{searchText}%")
                                  || EF.Functions.Like(e.Code, $"%{searchText}%"));
                if (departmentId != null) q = q.Where(e => e.DepartmentId == departmentId);
                return q;
            }

            var (items, total) = Page(page, pageSize, Shape, sortColumn switch
            {
                "Name"      => By(e => e.Name, sortDescending),
                "HireDate"  => By(e => e.HireDate, sortDescending),
                "CreatedAt" => By(e => e.CreatedAt, sortDescending),
                _           => By(e => e.Code, sortDescending),
            });

            WithNames<Department>("Departments", items, (e => e.DepartmentId, (e, name) => e.DepartmentName = name));
            return (WithNames<JobTitle>("JobTitles", items, (e => e.JobTitleId, (e, name) => e.JobTitleName = name)), total);
        }

        public int Insert(Employee e, PrimeDbContext db = null) => Add(e, db);

        public void Update(Employee e, PrimeDbContext db = null) =>
            Modify(e, db, nameof(Employee.Code));

        public void UpdateNameByAccountCode(PrimeDbContext db, string accountCode, string name) =>
            Set(e => e.AccountCode == accountCode, s => s.SetProperty(r => r.Name, name), db);

        public void Delete(int id, string deletedBy, PrimeDbContext db = null) =>
            SoftDelete(id, deletedBy, db);
    }
}
