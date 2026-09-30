using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع Attendance</summary>
    public interface IAttendanceRepository
    {
        Attendance GetById(int id, PrimeDbContext db = null);
        (List<Attendance> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? employeeId,
            string sortColumn, bool sortDescending);

        Dictionary<int, decimal> OvertimeByEmployee(DateTime from, DateTime to);

        Dictionary<int, int> AbsenceDaysByEmployee(DateTime from, DateTime to);

        int Insert(Attendance a, PrimeDbContext db = null);
        void Update(Attendance a, PrimeDbContext db = null);
        void Delete(int id, string deletedBy, PrimeDbContext db = null);
    }

    public class AttendanceRepository : RepositoryBase<Attendance>, IAttendanceRepository
    {
        protected override string TableName => "Attendances";


        private static TimeSpan? Minutes(object value) =>
            value == DBNull.Value ? null : TimeSpan.FromMinutes(Convert.ToInt32(value));


        /// <summary>اسم الموظف وكوده عرضٌ فقط</summary>
        private static List<Attendance> WithEmployee(List<Attendance> rows) =>
            WithCodeNames<Employee>("Employees", rows, a => a.EmployeeId, (a, code, name) => (a.EmployeeCode, a.EmployeeName) = (code, name));

        public override Attendance GetById(int id, PrimeDbContext db = null) =>
            WithEmployee(Fetch(q => q.Where(a => a.Id == id), db)).FirstOrDefault();

        public (List<Attendance> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? employeeId,
            string sortColumn, bool sortDescending)
        {
            using var db = DbContextFactory.Open();

            IQueryable<Attendance> Shape(IQueryable<Attendance> rows)
            {
                var q = rows;
                if (employeeId != null) q = q.Where(a => a.EmployeeId == employeeId);
                if (!string.IsNullOrWhiteSpace(searchText))
                    q = q.Where(a => db.Employees.Any(e => e.Id == a.EmployeeId && EF.Functions.Like(e.Name, $"%{searchText}%")));
                return q;
            }

            var (items, total) = Page(page, pageSize, Shape,
                sortColumn == "OvertimeHours" ? By(a => a.OvertimeHours, sortDescending) : By(a => a.Date, sortDescending), db);
            return (WithEmployee(items), total);
        }

        public Dictionary<int, decimal> OvertimeByEmployee(DateTime from, DateTime to)
        {
            using var db = DbContextFactory.Open();
            return Rows(db).AsNoTracking()
                .Where(a => a.Date >= from && a.Date <= to)
                .GroupBy(a => a.EmployeeId)
                .Select(g => new { g.Key, Total = g.Sum(a => a.OvertimeHours) })
                .ToDictionary(x => x.Key, x => x.Total);
        }

        public Dictionary<int, int> AbsenceDaysByEmployee(DateTime from, DateTime to)
        {
            using var db = DbContextFactory.Open();
            return Rows(db).AsNoTracking()
                .Where(a => a.IsAbsent && a.Date >= from && a.Date <= to)
                .GroupBy(a => a.EmployeeId)
                .Select(g => new { g.Key, Days = g.Count() })
                .ToDictionary(x => x.Key, x => x.Days);
        }

        public int Insert(Attendance a, PrimeDbContext db = null) => Add(a, db);

        public void Update(Attendance a, PrimeDbContext db = null) =>
            Modify(a, db);

        public void Delete(int id, string deletedBy, PrimeDbContext db = null) =>
            SoftDelete(id, deletedBy, db);
    }
}
