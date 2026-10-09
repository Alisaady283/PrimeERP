using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع Attendance</summary>
    public interface IAttendanceRepository
    {
        AttendanceSheet GetById(int id, PrimeDbContext db = null);
        (List<AttendanceSheet> Items, int Total) GetPaged(int page, int pageSize, string searchText);
        int? SheetOn(DateTime date);
        Dictionary<int, Attendance> LinesOf(int sheetId);
        Dictionary<int, Dictionary<AttendanceStatus, int>> Counts(IEnumerable<int> sheetIds);
        Dictionary<int, List<Attendance>> DaysByEmployee(DateTime from, DateTime to);
        int InsertHeader(PrimeDbContext db, AttendanceSheet sheet);
        void InsertLine(PrimeDbContext db, int sheetId, Attendance line);
        void DeleteDocument(PrimeDbContext db, int id);
    }

    public class AttendanceRepository : RepositoryBase<AttendanceSheet>, IAttendanceRepository
    {
        protected override string TableName => "AttendanceSheets";
        protected override string LineTable => "Attendances";
        protected override string LineForeignKey => "SheetId";

        public (List<AttendanceSheet> Items, int Total) GetPaged(int page, int pageSize, string searchText) =>
            Page(page, pageSize, q => string.IsNullOrWhiteSpace(searchText) ? q : q.Where(s => EF.Functions.Like(s.Notes, $"%{searchText}%")),
                By(s => s.Date, descending: true));

        public int? SheetOn(DateTime date) => One(q => q.Where(s => s.Date == date.Date))?.Id;

        public Dictionary<int, Attendance> LinesOf(int sheetId) =>
            FetchOf<Attendance>(LineTable, q => q.Where(a => a.SheetId == sheetId)).ToDictionary(a => a.EmployeeId);

        public Dictionary<int, Dictionary<AttendanceStatus, int>> Counts(IEnumerable<int> sheetIds)
        {
            var ids = sheetIds.ToList();
            return Scope(null, db => RowsOf<Attendance>(db, LineTable).AsNoTracking()
                    .Where(a => ids.Contains(a.SheetId))
                    .GroupBy(a => new { a.SheetId, a.Status })
                    .Select(g => new { g.Key.SheetId, g.Key.Status, Count = g.Count() })
                    .ToList())
                .GroupBy(c => c.SheetId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(c => c.Status, c => c.Count));
        }

        public Dictionary<int, List<Attendance>> DaysByEmployee(DateTime from, DateTime to) =>
            FetchOf<Attendance>(LineTable, q => q.Where(a => a.SheetId > 0 && a.Date >= from.Date && a.Date <= to.Date))
                .GroupBy(a => a.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        public int InsertHeader(PrimeDbContext db, AttendanceSheet sheet) => Add(sheet, db);

        public void InsertLine(PrimeDbContext db, int sheetId, Attendance line) =>
            Write(ctx =>
            {
                line.SheetId = sheetId;
                SetOf<Attendance>(ctx, LineTable).Add(line);
                return 0;
            }, db);

        public void DeleteDocument(PrimeDbContext db, int id)
        {
            RemoveIn<Attendance>(LineTable, a => a.SheetId == id, db);
            Remove(s => s.Id == id, db);
        }
    }
}
