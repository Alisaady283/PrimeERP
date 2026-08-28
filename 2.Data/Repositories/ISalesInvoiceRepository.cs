using System;
using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface ISalesInvoiceRepository
    {
        void CreateTable();
        SalesInvoice GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<SalesInvoiceLine> GetLines(int invoiceId, DbConnection conn = null, DbTransaction tx = null);
        (List<SalesInvoice> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? customerId, string sortColumn, bool sortDescending);

        int InsertHeader(DbConnection conn, DbTransaction tx, SalesInvoice invoice);
        void InsertLine(DbConnection conn, DbTransaction tx, int invoiceId, SalesInvoiceLine line);
        void SetJournalEntryId(DbConnection conn, DbTransaction tx, int invoiceId, int journalEntryId);
    }
}
