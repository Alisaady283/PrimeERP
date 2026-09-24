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
        (List<StockAdjustment> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending);
        int InsertHeader(PrimeDbContext db, StockAdjustment doc);
        int InsertLine(PrimeDbContext db, int documentId, StockAdjustmentLine line);
    }

    public interface IStockOutRepository : IStockInRepository { }

    public interface IGoodsReceiptRepository : IStockInRepository { }
    public interface IGoodsIssueRepository : IStockInRepository { }
    public interface IDeliveryNoteRepository : IStockInRepository { }
    public interface ISalesReceiptRepository : IStockInRepository { }

/// <summary>مستودع StockIn</summary>

    public class StockInRepository : StockAdjustmentRepositoryBase, IStockInRepository
    {
        public StockInRepository() : base("StockInDocuments", "StockInLines") { }
    }

    public class StockOutRepository : StockAdjustmentRepositoryBase, IStockOutRepository
    {
        public StockOutRepository() : base("StockOutDocuments", "StockOutLines") { }
    }

    public class GoodsReceiptRepository : StockAdjustmentRepositoryBase, IGoodsReceiptRepository
    {
        public GoodsReceiptRepository() : base("GoodsReceiptDocuments", "GoodsReceiptLines") { }
    }

    public class GoodsIssueRepository : StockAdjustmentRepositoryBase, IGoodsIssueRepository
    {
        public GoodsIssueRepository() : base("GoodsIssueDocuments", "GoodsIssueLines") { }
    }

    public class DeliveryNoteRepository : StockAdjustmentRepositoryBase, IDeliveryNoteRepository
    {
        public DeliveryNoteRepository() : base("DeliveryNoteDocuments", "DeliveryNoteLines") { }
    }

    public class SalesReceiptRepository : StockAdjustmentRepositoryBase, ISalesReceiptRepository
    {
        public SalesReceiptRepository() : base("SalesReceiptDocuments", "SalesReceiptLines") { }
    }
}
