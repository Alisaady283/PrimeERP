using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IPosShiftRepository
    {
        PosShift OpenOf(int userId, PrimeDbContext db = null);
        PosShift GetById(int id, PrimeDbContext db = null);
        (List<PosShift> Items, int Total) GetPaged(int page, int pageSize);
        int Insert(PosShift shift, PrimeDbContext db = null);
        void Update(PosShift shift, PrimeDbContext db = null);
        void Record(int shiftId, decimal cashIn, decimal cashOut, int sales, int returns);
        List<(string Cashier, int Shifts, int Sales, int Returns, decimal CashIn, decimal CashOut, decimal Difference)> CashierTotals(DateTime from, DateTime to);
    }

    public class PosShiftRepository : RepositoryBase<PosShift>, IPosShiftRepository
    {
        protected override string TableName => "PosShifts";

        public PosShift OpenOf(int userId, PrimeDbContext db = null) =>
            One(q => q.Where(s => s.UserId == userId && s.ClosedAt == null).OrderByDescending(s => s.OpenedAt), db);

        public (List<PosShift> Items, int Total) GetPaged(int page, int pageSize) =>
            (Fetch(q => q.OrderByDescending(s => s.OpenedAt).Skip((page - 1) * pageSize).Take(pageSize)), Count(q => q));

        public int Insert(PosShift shift, PrimeDbContext db = null) => Add(shift, db);

        public void Update(PosShift shift, PrimeDbContext db = null) => Modify(shift, db);

        public List<(string Cashier, int Shifts, int Sales, int Returns, decimal CashIn, decimal CashOut, decimal Difference)> CashierTotals(DateTime from, DateTime to) =>
            Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(s => s.OpenedAt >= from && s.OpenedAt <= to)
                .GroupBy(s => s.UserName)
                .Select(g => new
                {
                    g.Key, Shifts = g.Count(), Sales = g.Sum(s => s.SalesCount), Returns = g.Sum(s => s.ReturnsCount),
                    CashIn = g.Sum(s => s.CashIn), CashOut = g.Sum(s => s.CashOut), Difference = g.Sum(s => s.Difference ?? 0)
                })
                .ToList()
                .Select(x => (x.Key, x.Shifts, x.Sales, x.Returns, x.CashIn, x.CashOut, x.Difference))
                .ToList());

        public void Record(int shiftId, decimal cashIn, decimal cashOut, int sales, int returns) =>
            Set(s => s.Id == shiftId, set => set
                .SetProperty(s => s.CashIn, s => s.CashIn + cashIn)
                .SetProperty(s => s.CashOut, s => s.CashOut + cashOut)
                .SetProperty(s => s.SalesCount, s => s.SalesCount + sales)
                .SetProperty(s => s.ReturnsCount, s => s.ReturnsCount + returns));
    }

    public interface IHeldSaleRepository
    {
        List<HeldSale> Of(int? branchId);
        HeldSale GetById(int id, PrimeDbContext db = null);
        int Insert(HeldSale sale, PrimeDbContext db = null);
        void Delete(int id, PrimeDbContext db = null);
    }

    public class HeldSaleRepository : RepositoryBase<HeldSale>, IHeldSaleRepository
    {
        protected override string TableName => "HeldSales";

        public List<HeldSale> Of(int? branchId) =>
            Fetch(q => q.Where(h => h.BranchId == branchId).OrderByDescending(h => h.HeldAt));

        public int Insert(HeldSale sale, PrimeDbContext db = null) => Add(sale, db);

        public void Delete(int id, PrimeDbContext db = null) => Remove(h => h.Id == id, db);
    }
}
