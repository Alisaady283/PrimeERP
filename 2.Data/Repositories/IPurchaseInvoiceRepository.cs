using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IPurchaseInvoiceRepository
    {
        void CreateTable();
        PurchaseInvoice GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<PurchaseInvoiceLine> GetLines(int invoiceId, DbConnection conn = null, DbTransaction tx = null);
        (List<PurchaseInvoice> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? supplierId, string sortColumn, bool sortDescending);

        int InsertHeader(DbConnection conn, DbTransaction tx, PurchaseInvoice invoice);
        void InsertLine(DbConnection conn, DbTransaction tx, int invoiceId, PurchaseInvoiceLine line);
        void SetJournalEntryId(DbConnection conn, DbTransaction tx, int invoiceId, int journalEntryId);
    }
}
