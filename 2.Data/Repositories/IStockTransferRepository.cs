using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IStockTransferRepository
    {
        void CreateTable();
        StockTransferDocument GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<StockTransferLine> GetLines(int documentId, DbConnection conn = null, DbTransaction tx = null);
        (List<StockTransferDocument> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending);
        int InsertHeader(DbConnection conn, DbTransaction tx, StockTransferDocument doc);
        void InsertLine(DbConnection conn, DbTransaction tx, int documentId, StockTransferLine line);
    }
}
