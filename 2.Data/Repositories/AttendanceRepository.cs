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
        private static List<Attendance> WithEmployee(PrimeDbContext db, IQueryable<Attendance> rows) =>
            (from a in rows
             join e in db.Employees.AsNoTracking() on a.EmployeeId equals e.Id into found
             from e in found.DefaultIfEmpty()
             select new { Row = a, e.Name, e.Code })
            .AsEnumerable()
            .Select(x =>
            {
                x.Row.EmployeeName = x.Name;
                x.Row.EmployeeCode = x.Code;
                return x.Row;
            })
            .ToList();

        public override Attendance GetById(int id, PrimeDbContext db = null)
        {
            return Scope(db, ctx =>
            {
                return WithEmployee(ctx, Live(Rows(ctx).AsNoTracking()).Where(a => a.Id == id)).FirstOrDefault();
            });
        }

        public (List<Attendance> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? employeeId,
            string sortColumn, bool sortDescending)
        {
            using var db = DbContextFactory.Open();
            var rows = Rows(db).AsNoTracking().Where(a => !a.IsDeleted);
            if (employeeId != null) rows = rows.Where(a => a.EmployeeId == employeeId);
            if (!string.IsNullOrWhiteSpace(searchText))
                rows = rows.Where(a => db.Employees.Any(e => e.Id == a.EmployeeId
                                                          && EF.Functions.Like(e.Name, $"%{searchText}%")));

            var total = rows.Count();
            var ordered = (sortColumn == "OvertimeHours" ? By(a => a.OvertimeHours, sortDescending)
                                                        : By(a => a.Date,          sortDescending))(rows);

            var page_ = ordered.ThenByDescending(a => a.Id).Skip(Math.Max(0, page - 1) * pageSize).Take(pageSize);
            return (WithEmployee(db, page_), total);
        }

        public Dictionary<int, decimal> OvertimeByEmployee(DateTime from, DateTime to)
        {
            using var db = DbContextFactory.Open();
            return Rows(db).AsNoTracking()
                .Where(a => !a.IsDeleted && a.Date >= from && a.Date <= to)
                .GroupBy(a => a.EmployeeId)
                .Select(g => new { g.Key, Total = g.Sum(a => a.OvertimeHours) })
                .ToDictionary(x => x.Key, x => x.Total);
        }

        public Dictionary<int, int> AbsenceDaysByEmployee(DateTime from, DateTime to)
        {
            using var db = DbContextFactory.Open();
            return Rows(db).AsNoTracking()
                .Where(a => !a.IsDeleted && a.IsAbsent && a.Date >= from && a.Date <= to)
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
