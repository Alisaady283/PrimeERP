using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IPurchaseReturnRepository
    {
        void CreateTable();
        PurchaseReturn GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<PurchaseReturnLine> GetLines(int returnId, DbConnection conn = null, DbTransaction tx = null);
        (List<PurchaseReturn> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? supplierId, string sortColumn, bool sortDescending);

        int InsertHeader(DbConnection conn, DbTransaction tx, PurchaseReturn ret);
        void InsertLine(DbConnection conn, DbTransaction tx, int returnId, PurchaseReturnLine line);
        void SetJournalEntryId(DbConnection conn, DbTransaction tx, int returnId, int journalEntryId);
    }
}
