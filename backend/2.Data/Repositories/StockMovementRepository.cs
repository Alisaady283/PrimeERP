using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع StockMovement</summary>
    public interface IStockMovementRepository
    {
        int Insert(StockMovement m, PrimeDbContext db = null);

        void DeleteBySource(PrimeDbContext db, string sourceDocType, int sourceDocId);
        decimal GetBalance(int productId, int? warehouseId, PrimeDbContext db = null);
        List<(string Batch, DateTime? Expiry, decimal Balance)> BatchBalances(int productId, int warehouseId, PrimeDbContext db = null);
        List<(int ProductId, int WarehouseId, string Batch, DateTime? Expiry, decimal Balance)> AllBatches();
        List<StockMovement> BySource(string sourceDocType, DateTime from, DateTime to);
        bool AnyForProduct(int productId);
        List<StockMovement> GetHistory(int productId, int? warehouseId, int maxResults);

        List<(int ProductId, int WarehouseId, decimal Balance)> GetAllBalances();

        List<(int ProductId, int WarehouseId, decimal Opening, decimal In, decimal Out)> BalanceGrid(DateTime from, DateTime to);

        List<StockMovement> GetMovements(System.DateTime from, System.DateTime to, int? warehouseId, int maxResults);

        List<StockMovement> GetForCosting(int productId, PrimeDbContext db = null);

        decimal? GetSourceUnitCost(string sourceDocType, int sourceDocId, int productId,
            PrimeDbContext db = null);

        List<StockMovement> GetBySource(string sourceDocType, int sourceDocId, PrimeDbContext db = null);
        Dictionary<(int ProductId, int WarehouseId), decimal> BalancesOf(IReadOnlyCollection<(int ProductId, int WarehouseId)> pairs, PrimeDbContext db = null);
        bool AnyInWarehouse(string sourceDocType, int warehouseId, int exceptSourceId);
        List<(int ProductId, int WarehouseId, decimal Qty, decimal Value)> Valuation(DateTime asOf);
        List<(int Year, int Month, decimal Cost)> MonthlyCost(string sourceDocType, DateTime from, DateTime to);
        Dictionary<int, decimal> CostByProduct(string sourceDocType, DateTime from, DateTime to);
        Dictionary<int, DateTime> LastMovement(string sourceDocType);
    }

    public class StockMovementRepository : RepositoryBase<StockMovement>, IStockMovementRepository
    {
        protected override string TableName => "StockMovements";


        public int Insert(StockMovement m, PrimeDbContext db = null) => Add(m, db);

        public void DeleteBySource(PrimeDbContext db, string sourceDocType, int sourceDocId) =>
            Remove(m => m.SourceDocType == sourceDocType && m.SourceDocId == sourceDocId, db);

        public bool AnyForProduct(int productId) => Any(q => q.Where(m => m.ProductId == productId));

        public decimal GetBalance(int productId, int? warehouseId, PrimeDbContext db = null)
        {
            return Scope(db, ctx =>
            {
                return Rows(ctx).AsNoTracking()
                    .Where(m => m.ProductId == productId && (warehouseId == null || m.WarehouseId == warehouseId))
                    .Sum(m => (decimal?)(m.MovementType == MovementType.Out ? -m.Qty : m.Qty)) ?? 0m;
            });
        }

        public List<(string Batch, DateTime? Expiry, decimal Balance)> BatchBalances(int productId, int warehouseId, PrimeDbContext db = null) =>
            Scope(db, ctx => Rows(ctx).AsNoTracking()
                .Where(m => m.ProductId == productId && m.WarehouseId == warehouseId)
                .GroupBy(m => new { m.BatchNo, m.ExpiryDate })
                .Select(g => new { g.Key.BatchNo, g.Key.ExpiryDate, Balance = g.Sum(m => m.MovementType == MovementType.Out ? -m.Qty : m.Qty) })
                .ToList()
                .Select(b => (b.BatchNo, b.ExpiryDate, b.Balance))
                .ToList());

        public List<(int ProductId, int WarehouseId, string Batch, DateTime? Expiry, decimal Balance)> AllBatches() =>
            Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(m => m.BatchNo != null)
                .GroupBy(m => new { m.ProductId, m.WarehouseId, m.BatchNo, m.ExpiryDate })
                .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, g.Key.BatchNo, g.Key.ExpiryDate, Balance = g.Sum(m => m.MovementType == MovementType.Out ? -m.Qty : m.Qty) })
                .Where(b => b.Balance != 0)
                .OrderBy(b => b.ExpiryDate)
                .ToList()
                .Select(b => (b.ProductId, b.WarehouseId, b.BatchNo, b.ExpiryDate, b.Balance))
                .ToList());

        public List<StockMovement> BySource(string sourceDocType, DateTime from, DateTime to) =>
            Fetch(q => q.Where(m => m.SourceDocType == sourceDocType && m.MovementDate >= from && m.MovementDate <= to).OrderBy(m => m.MovementDate));

        public List<(int ProductId, int WarehouseId, decimal Balance)> GetAllBalances()
        {
            using var db = DbContextFactory.Open();
            return Rows(db).AsNoTracking()
                .GroupBy(m => new { m.ProductId, m.WarehouseId })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.WarehouseId,
                    Balance = g.Sum(m => m.MovementType == MovementType.Out ? -m.Qty : m.Qty)
                })
                .Where(x => x.Balance != 0)
                .AsEnumerable()
                .Select(x => (x.ProductId, x.WarehouseId, x.Balance))
                .ToList();
        }

        /// <summary>أرصدة الأصناف في مدى</summary>
        public List<(int ProductId, int WarehouseId, decimal Opening, decimal In, decimal Out)> BalanceGrid(DateTime from, DateTime to) =>
            Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(m => m.MovementDate <= to)
                .Select(m => new
                {
                    m.ProductId, m.WarehouseId, Before = m.MovementDate < from,
                    Signed = m.MovementType == MovementType.Out ? -m.Qty : m.Qty
                })
                .GroupBy(m => new { m.ProductId, m.WarehouseId })
                .Select(g => new
                {
                    g.Key.ProductId, g.Key.WarehouseId,
                    Opening = g.Sum(m => m.Before ? m.Signed : 0m),
                    In = g.Sum(m => !m.Before && m.Signed > 0 ? m.Signed : 0m),
                    Out = g.Sum(m => !m.Before && m.Signed < 0 ? -m.Signed : 0m)
                })
                .AsEnumerable()
                .Select(x => (x.ProductId, x.WarehouseId, x.Opening, x.In, x.Out))
                .ToList());

        public List<(int ProductId, int WarehouseId, decimal Qty, decimal Value)> Valuation(DateTime asOf) =>
            Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(m => m.MovementDate < asOf.Date.AddDays(1))
                .Select(m => new
                {
                    m.ProductId, m.WarehouseId,
                    Qty = m.MovementType == MovementType.Out ? -m.Qty : m.Qty,
                    Value = m.MovementType == MovementType.Out ? -m.TotalCost : m.TotalCost
                })
                .GroupBy(m => new { m.ProductId, m.WarehouseId })
                .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, Qty = g.Sum(m => m.Qty), Value = g.Sum(m => m.Value) })
                .AsEnumerable().Select(x => (x.ProductId, x.WarehouseId, x.Qty, x.Value)).ToList());

        public List<(int Year, int Month, decimal Cost)> MonthlyCost(string sourceDocType, DateTime from, DateTime to) =>
            Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(m => m.SourceDocType == sourceDocType && m.MovementDate >= from.Date && m.MovementDate < to.Date.AddDays(1))
                .GroupBy(m => new { m.MovementDate.Year, m.MovementDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Cost = g.Sum(m => m.TotalCost) })
                .AsEnumerable().Select(x => (x.Year, x.Month, x.Cost)).ToList());

        public Dictionary<int, decimal> CostByProduct(string sourceDocType, DateTime from, DateTime to) =>
            Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(m => m.SourceDocType == sourceDocType && m.MovementDate >= from.Date && m.MovementDate < to.Date.AddDays(1))
                .GroupBy(m => m.ProductId)
                .Select(g => new { g.Key, Cost = g.Sum(m => m.TotalCost) })
                .ToDictionary(x => x.Key, x => x.Cost));

        public Dictionary<int, DateTime> LastMovement(string sourceDocType) =>
            Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(m => m.SourceDocType == sourceDocType)
                .GroupBy(m => m.ProductId)
                .Select(g => new { g.Key, Last = g.Max(m => m.MovementDate) })
                .ToDictionary(x => x.Key, x => x.Last));

        public List<StockMovement> GetMovements(DateTime from, DateTime to, int? warehouseId, int maxResults) =>
            Fetch(q => q.Where(m => m.MovementDate >= from && m.MovementDate <= to
                                 && (warehouseId == null || m.WarehouseId == warehouseId))
                        .OrderByDescending(m => m.MovementDate).ThenByDescending(m => m.Id).Take(maxResults));

        public List<StockMovement> GetHistory(int productId, int? warehouseId, int maxResults) =>
            Fetch(q => q.Where(m => m.ProductId == productId && (warehouseId == null || m.WarehouseId == warehouseId))
                        .OrderByDescending(m => m.Id).Take(maxResults));

        public List<StockMovement> GetForCosting(int productId, PrimeDbContext db = null) =>
            Fetch(q => q.Where(m => m.ProductId == productId).OrderBy(m => m.MovementDate).ThenBy(m => m.Id), db);

        public decimal? GetSourceUnitCost(string sourceDocType, int sourceDocId, int productId,
            PrimeDbContext db = null) =>
            One(q => q.Where(m => m.SourceDocType == sourceDocType && m.SourceDocId == sourceDocId
                               && m.ProductId == productId).OrderBy(m => m.Id), db)?.UnitCost;

        public List<StockMovement> GetBySource(string sourceDocType, int sourceDocId, PrimeDbContext db = null) =>
            Fetch(q => q.Where(m => m.SourceDocType == sourceDocType && m.SourceDocId == sourceDocId).OrderBy(m => m.Id), db);

        /// <summary>أرصدة أزواجٍ بعينها</summary>
        public Dictionary<(int ProductId, int WarehouseId), decimal> BalancesOf(IReadOnlyCollection<(int ProductId, int WarehouseId)> pairs,
            PrimeDbContext db = null)
        {
            var products = pairs.Select(p => p.ProductId).Distinct().ToList();
            var warehouses = pairs.Select(p => p.WarehouseId).Distinct().ToList();

            return Scope(db, ctx => Rows(ctx).AsNoTracking()
                .Where(m => products.Contains(m.ProductId) && warehouses.Contains(m.WarehouseId))
                .GroupBy(m => new { m.ProductId, m.WarehouseId })
                .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, Balance = g.Sum(m => m.MovementType == MovementType.Out ? -m.Qty : m.Qty) })
                .AsEnumerable()
                .ToDictionary(x => (x.ProductId, x.WarehouseId), x => x.Balance));
        }

        public bool AnyInWarehouse(string sourceDocType, int warehouseId, int exceptSourceId) =>
            Any(q => q.Where(m => m.SourceDocType == sourceDocType && m.WarehouseId == warehouseId && m.SourceDocId != exceptSourceId));
    }
}
