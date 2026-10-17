using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IBillOfMaterialsRepository
    {
        BillOfMaterials GetById(int id, PrimeDbContext db = null);
        List<BillOfMaterials> GetAll(bool includeInactive = false);
        List<BomLine> GetLines(int bomId, PrimeDbContext db = null);
        (List<BillOfMaterials> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending);
        int InsertHeader(PrimeDbContext db, BillOfMaterials bom);
        void InsertLine(PrimeDbContext db, int bomId, BomLine line);
        void DeleteDocument(PrimeDbContext db, int id);
        bool UsedByOrders(int bomId);
    }

    public class BillOfMaterialsRepository : RepositoryBase<BillOfMaterials>, IBillOfMaterialsRepository
    {
        protected override string TableName => "BillsOfMaterials";
        protected override string LineTable => "BomLines";
        protected override string LineForeignKey => "BomId";

        public List<BillOfMaterials> GetAll(bool includeInactive = false) =>
            Fetch(q => q.Where(b => includeInactive || b.IsActive).OrderBy(b => b.Name));

        public List<BomLine> GetLines(int bomId, PrimeDbContext db = null) =>
            FetchOf<BomLine>(LineTable, q => q.Where(l => l.BomId == bomId).OrderBy(l => l.LineNo), db);

        public (List<BillOfMaterials> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending)
        {
            IQueryable<BillOfMaterials> Shape(IQueryable<BillOfMaterials> rows) =>
                string.IsNullOrWhiteSpace(searchText) ? rows
                    : rows.Where(b => EF.Functions.Like(b.Code, $"%{searchText}%") || EF.Functions.Like(b.Name, $"%{searchText}%")
                                   || EF.Functions.Like(b.ProductName, $"%{searchText}%"));
            return Page(page, pageSize, Shape, By(b => b.Name, sortDescending));
        }

        public int InsertHeader(PrimeDbContext db, BillOfMaterials bom) => Add(bom, db);

        public void InsertLine(PrimeDbContext db, int bomId, BomLine line)
        {
            line.BomId = bomId;
            Write(ctx => { SetOf<BomLine>(ctx, LineTable).Add(line); return 0; }, db);
        }

        public void DeleteDocument(PrimeDbContext db, int id)
        {
            RemoveIn<BomLine>(LineTable, l => l.BomId == id, db);
            Remove(b => b.Id == id, db);
        }

        public bool UsedByOrders(int bomId) =>
            FetchOf<ProductionOrder>("ProductionOrders", q => q.Where(o => o.BomId == bomId).Take(1)).Count > 0;
    }

    public interface IProductionOrderRepository
    {
        ProductionOrder GetById(int id, PrimeDbContext db = null);
        List<ProductionOrderLine> GetLines(int orderId, PrimeDbContext db = null);
        (List<ProductionOrder> Items, int Total) GetPaged(int page, int pageSize, bool unbuild, string searchText, string sortColumn, bool sortDescending);
        int InsertHeader(PrimeDbContext db, ProductionOrder order);
        void InsertLine(PrimeDbContext db, int orderId, ProductionOrderLine line);
        void Update(PrimeDbContext db, ProductionOrder order);
        void DeleteDocument(PrimeDbContext db, int id);
        List<ProductionOrder> Between(DateTime from, DateTime to, bool unbuild);
        List<ProductionOrderLine> LinesBetween(DateTime from, DateTime to, Domain.Enums.BomLineKind kind);
        List<(int Year, int Month, decimal Qty, decimal Materials, decimal Operations, int Count)> Monthly(DateTime from, DateTime to);
        List<(string Name, decimal Qty, decimal Cost)> ByProduct(DateTime from, DateTime to);
        List<(int WorkCenterId, decimal Minutes, decimal Rate)> LoadByCenter(DateTime from, DateTime to);
    }

    public class ProductionOrderRepository : RepositoryBase<ProductionOrder>, IProductionOrderRepository
    {
        protected override string TableName => "ProductionOrders";
        protected override string LineTable => "ProductionOrderLines";
        protected override string LineForeignKey => "OrderId";

        public List<ProductionOrderLine> GetLines(int orderId, PrimeDbContext db = null) =>
            FetchOf<ProductionOrderLine>(LineTable, q => q.Where(l => l.OrderId == orderId).OrderBy(l => l.LineNo), db);

        public (List<ProductionOrder> Items, int Total) GetPaged(int page, int pageSize, bool unbuild, string searchText, string sortColumn, bool sortDescending)
        {
            IQueryable<ProductionOrder> Shape(IQueryable<ProductionOrder> rows) =>
                rows.Where(o => o.Unbuild == unbuild && (string.IsNullOrWhiteSpace(searchText)
                    || EF.Functions.Like(o.OrderNo, $"%{searchText}%") || EF.Functions.Like(o.ProductName, $"%{searchText}%")));
            return Page(page, pageSize, Shape, DocumentOrder(o => o.OrderDate, sortDescending, o => o.OrderNo));
        }

        public int InsertHeader(PrimeDbContext db, ProductionOrder order) => Add(order, db);

        public void InsertLine(PrimeDbContext db, int orderId, ProductionOrderLine line)
        {
            line.OrderId = orderId;
            Write(ctx => { SetOf<ProductionOrderLine>(ctx, LineTable).Add(line); return 0; }, db);
        }

        public void Update(PrimeDbContext db, ProductionOrder order) => Modify(order, db);

        public List<ProductionOrder> Between(DateTime from, DateTime to, bool unbuild) =>
            Fetch(q => q.Where(o => o.Unbuild == unbuild && o.OrderDate >= from && o.OrderDate <= to).OrderBy(o => o.OrderDate));

        public List<(int Year, int Month, decimal Qty, decimal Materials, decimal Operations, int Count)> Monthly(DateTime from, DateTime to) =>
            Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(o => !o.Unbuild && o.OrderDate >= from.Date && o.OrderDate < to.Date.AddDays(1))
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Qty = g.Sum(o => o.Qty), Materials = g.Sum(o => o.MaterialsCost), Operations = g.Sum(o => o.OperationsCost), Count = g.Count() })
                .AsEnumerable().Select(x => (x.Year, x.Month, x.Qty, x.Materials, x.Operations, x.Count)).ToList());

        public List<(string Name, decimal Qty, decimal Cost)> ByProduct(DateTime from, DateTime to) =>
            Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(o => !o.Unbuild && o.OrderDate >= from.Date && o.OrderDate < to.Date.AddDays(1))
                .GroupBy(o => o.ProductName)
                .Select(g => new { g.Key, Qty = g.Sum(o => o.Qty), Cost = g.Sum(o => o.TotalCost) })
                .AsEnumerable().Select(x => (x.Key, x.Qty, x.Cost)).ToList());

        public List<(int WorkCenterId, decimal Minutes, decimal Rate)> LoadByCenter(DateTime from, DateTime to) =>
            Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(o => !o.Unbuild && o.OrderDate >= from.Date && o.OrderDate < to.Date.AddDays(1))
                .Join(RowsOf<ProductionOrderLine>(ctx, LineTable).AsNoTracking().Where(l => l.Kind == Domain.Enums.BomLineKind.Operation),
                    o => o.Id, l => l.OrderId, (o, l) => l)
                .GroupBy(l => l.WorkCenterId ?? 0)
                .Select(g => new { g.Key, Minutes = g.Sum(l => l.Qty), Weighted = g.Sum(l => l.Qty * l.UnitCost) })
                .AsEnumerable().Select(x => (x.Key, x.Minutes, x.Minutes == 0 ? 0 : x.Weighted / x.Minutes)).ToList());

        public List<ProductionOrderLine> LinesBetween(DateTime from, DateTime to, Domain.Enums.BomLineKind kind)
        {
            var orders = Fetch(q => q.Where(o => !o.Unbuild && o.OrderDate >= from && o.OrderDate <= to)).Select(o => o.Id).ToList();
            return orders.Count == 0 ? new List<ProductionOrderLine>()
                : FetchOf<ProductionOrderLine>(LineTable, q => q.Where(l => l.Kind == kind && orders.Contains(l.OrderId)));
        }

        public void DeleteDocument(PrimeDbContext db, int id)
        {
            RemoveIn<ProductionOrderLine>(LineTable, l => l.OrderId == id, db);
            Remove(o => o.Id == id, db);
        }
    }
}
