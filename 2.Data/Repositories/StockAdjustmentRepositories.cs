using System;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع StockIn</summary>
    public interface IStockInRepository
    {
        void DeleteDocument(PrimeDbContext db, int id);
        StockAdjustment GetById(int id, PrimeDbContext db = null);
        List<StockAdjustmentLine> GetLines(int documentId, PrimeDbContext db = null);
        Dictionary<int, decimal> TotalQty(IEnumerable<int> documentIds);
        (List<StockAdjustment> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending);
        int InsertHeader(PrimeDbContext db, StockAdjustment doc);
        int InsertLine(PrimeDbContext db, int documentId, StockAdjustmentLine line);
    }

    public interface IStockOutRepository : IStockInRepository { }

    public interface IScrapRepository : IStockInRepository { }

/// <summary>مستودع StockIn</summary>

    public class StockInRepository : StockAdjustmentRepositoryBase, IStockInRepository
    {
        public StockInRepository() : base("StockInDocuments", "StockInLines") { }
    }

    public class StockOutRepository : StockAdjustmentRepositoryBase, IStockOutRepository
    {
        public StockOutRepository() : base("StockOutDocuments", "StockOutLines") { }
    }

    public class ScrapRepository : StockAdjustmentRepositoryBase, IScrapRepository
    {
        public ScrapRepository() : base("ScrapDocuments", "ScrapLines") { }
    }

}
