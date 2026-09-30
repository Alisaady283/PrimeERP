using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories.Base
{
    /// <summary>عقد مستودع الفواتير</summary>
    public interface IInvoiceRepository<TInvoice, TLine>
    {
        void DeleteDocument(PrimeDbContext db, int id);
        TInvoice GetById(int id, PrimeDbContext db = null);
        List<TLine> GetLines(int invoiceId, PrimeDbContext db = null);
        (List<TInvoice> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? partyId,
                                                   string sortColumn, bool sortDescending);

        int InsertHeader(PrimeDbContext db, TInvoice invoice);
        int InsertLine(PrimeDbContext db, int invoiceId, TLine line);
        void SetJournalEntryId(PrimeDbContext db, int invoiceId, int journalEntryId);
        List<TInvoice> Between(DateTime from, DateTime to);
    }

    /// <summary>أساس فاتورة: رأس وسطور</summary>
    public abstract class InvoiceRepositoryBase<TInvoice, TLine> : RepositoryBase<TInvoice>
        where TInvoice : InvoiceBase, new()
        where TLine : InvoiceLineBase, new()
    {
        protected abstract string HeaderTable { get; }
        protected abstract string LinesTable { get; }
        protected abstract string PartyColumn { get; }
        protected abstract int PartyIdOf(TInvoice invoice);
        protected abstract void ApplyPartyId(TInvoice invoice, int partyId);

        protected override string TableName => HeaderTable;
        protected override string LineTable => LinesTable;
        protected override string LineForeignKey => "InvoiceId";


        public void DeleteDocument(PrimeDbContext db, int id)
        {
            RemoveIn<TLine>(LinesTable, l => l.InvoiceId == id, db);

            Remove(i => i.Id == id, db);
        }


        public List<TLine> GetLines(int invoiceId, PrimeDbContext db = null) =>
            FetchOf<TLine>(LinesTable, q => q.Where(l => l.InvoiceId == invoiceId).OrderBy(l => l.LineNo), db);

        public (List<TInvoice> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? partyId,
                                                          string sortColumn, bool sortDescending)
        {
            IQueryable<TInvoice> Shape(IQueryable<TInvoice> rows)
            {
                var q = rows;
                if (!string.IsNullOrWhiteSpace(searchText))
                    q = q.Where(i => EF.Functions.Like(i.InvoiceNo, $"%{searchText}%"));
                if (partyId != null)
                    q = q.Where(i => EF.Property<int>(i, PartyColumn) == partyId);
                return q;
            }

            return Page(page, pageSize, Shape, Order(sortColumn, sortDescending));
        }

        private static Func<IQueryable<TInvoice>, IOrderedQueryable<TInvoice>> Order(string column, bool descending) => column switch
        {
            "InvoiceNo" => DocumentOrder(i => i.InvoiceNo,   descending, i => i.InvoiceNo),
            "NetTotal"  => DocumentOrder(i => i.NetTotal,    descending, i => i.InvoiceNo),
            _           => DocumentOrder(i => i.InvoiceDate, descending, i => i.InvoiceNo),
        };

        public List<TInvoice> Between(DateTime from, DateTime to) =>
            Fetch(q => q.Where(i => i.InvoiceDate >= from.Date && i.InvoiceDate < to.Date.AddDays(1))
                        .OrderBy(i => i.InvoiceDate).ThenBy(i => i.InvoiceNo));

        public int InsertHeader(PrimeDbContext db, TInvoice invoice) => Add(invoice, db);

        public int InsertLine(PrimeDbContext db, int invoiceId, TLine line)
        {
            line.InvoiceId = invoiceId;
            Write(db => { SetOf<TLine>(db, LinesTable).Add(line); return 0; }, db);
            return line.Id;
        }

        public void SetJournalEntryId(PrimeDbContext db, int invoiceId, int journalEntryId) =>
            Set(i => i.Id == invoiceId, s => s.SetProperty(r => r.JournalEntryId, journalEntryId), db);
    }
}
