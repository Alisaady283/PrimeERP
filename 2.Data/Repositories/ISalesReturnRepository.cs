using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface ISalesReturnRepository
    {
        void CreateTable();
        void DeleteDocument(DbConnection conn, DbTransaction tx, int id);
        SalesReturn GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<SalesReturnLine> GetLines(int returnId, DbConnection conn = null, DbTransaction tx = null);
        (List<SalesReturn> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? customerId, string sortColumn, bool sortDescending);

        int InsertHeader(DbConnection conn, DbTransaction tx, SalesReturn ret);
        void InsertLine(DbConnection conn, DbTransaction tx, int returnId, SalesReturnLine line);
        void SetJournalEntryId(DbConnection conn, DbTransaction tx, int returnId, int journalEntryId);
    }
}
