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
        bool AnyForProduct(int productId);
        List<StockMovement> GetHistory(int productId, int? warehouseId, int maxResults);

        List<(int ProductId, int WarehouseId, decimal Balance)> GetAllBalances();

        List<(int ProductId, int WarehouseId, decimal Opening, decimal In, decimal Out)> BalanceGrid(DateTime from, DateTime to);

        List<StockMovement> GetMovements(System.DateTime from, System.DateTime to, int? warehouseId, int maxResults);

        List<StockMovement> GetForCosting(int productId, PrimeDbContext db = null);

        decimal? GetSourceUnitCost(string sourceDocType, int sourceDocId, int productId,
            PrimeDbContext db = null);
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
    }
}
