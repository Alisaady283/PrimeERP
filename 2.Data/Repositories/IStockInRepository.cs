using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IStockInRepository
    {
        void CreateTable();
        void DeleteDocument(DbConnection conn, DbTransaction tx, int id);
        StockAdjustment GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<StockAdjustmentLine> GetLines(int documentId, DbConnection conn = null, DbTransaction tx = null);
        (List<StockAdjustment> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending);
        int InsertHeader(DbConnection conn, DbTransaction tx, StockAdjustment doc);
        int InsertLine(DbConnection conn, DbTransaction tx, int documentId, StockAdjustmentLine line);
    }

    public interface IStockOutRepository : IStockInRepository { }

    public interface IGoodsReceiptRepository : IStockInRepository { }
    public interface IGoodsIssueRepository : IStockInRepository { }
    public interface IDeliveryNoteRepository : IStockInRepository { }
    public interface ISalesReceiptRepository : IStockInRepository { }
}
