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
    public interface IInvoiceRepository<TInvoice, TLine> : IFlowLines
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
        List<(int Year, int Month, decimal Value, int Count)> Monthly(DateTime from, DateTime to, TradeScope scope = null);
        List<(int PartyId, decimal Value, int Count)> ByParty(DateTime from, DateTime to, TradeScope scope = null);
        List<(int ProductId, decimal Qty, decimal Value)> ByProduct(DateTime from, DateTime to, TradeScope scope = null);
        List<(int PartyId, DateTime Date, DateTime? DueDate, decimal Remaining)> Open(TradeScope scope = null);
        List<TInvoice> OfParty(int partyId, PrimeDbContext db = null);
        List<(int PartyId, DateTime First, DateTime Last)> FirstLast(DateTime to, TradeScope scope = null);
        (decimal Net, decimal Paid) Settled(DateTime from, DateTime to, TradeScope scope = null);
        void SetSettlement(PrimeDbContext db, IEnumerable<(int Id, decimal Paid, decimal Remaining)> settled);
        List<int> UnsettledParties();
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


        public List<FlowLine> FlowLines(DateTime from, DateTime to) =>
            Scope(null, ctx => Rows(ctx).AsNoTracking().Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to)
                .Join(RowsOf<TLine>(ctx, LinesTable).AsNoTracking(), i => i.Id, l => l.InvoiceId,
                    (i, l) => new FlowLine(i.Id, i.InvoiceNo, i.InvoiceDate, (int?)EF.Property<int>(i, PartyColumn), l.Id, l.ProductCode, l.ProductName, l.Qty))
                .ToList());

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

        private IQueryable<TInvoice> Valid(PrimeDbContext ctx, DateTime from, DateTime to, TradeScope scope = null) =>
            Scoped(ctx, Rows(ctx).AsNoTracking().Where(i => i.Status != InvoiceStatus.Cancelled && i.InvoiceDate >= from.Date && i.InvoiceDate < to.Date.AddDays(1)), scope);

        /// <summary>فواتير النطاق: طرفه، أو ما فيه سطرٌ من أصنافه</summary>
        private IQueryable<TInvoice> Scoped(PrimeDbContext ctx, IQueryable<TInvoice> invoices, TradeScope scope)
        {
            if (scope?.PartyId is int party) invoices = invoices.Where(i => EF.Property<int>(i, PartyColumn) == party);
            if (scope?.ProductIds is { } ids)
            {
                var lines = RowsOf<TLine>(ctx, LinesTable).AsNoTracking();
                invoices = invoices.Where(i => lines.Any(l => l.InvoiceId == i.Id && ids.Contains(l.ProductId)));
            }
            return invoices;
        }

        public List<(int Year, int Month, decimal Value, int Count)> Monthly(DateTime from, DateTime to, TradeScope scope = null) =>
            scope?.ProductIds is { } ids
                ? Scope(null, ctx => Valid(ctx, from, to, scope)
                    .Join(RowsOf<TLine>(ctx, LinesTable).AsNoTracking().Where(l => ids.Contains(l.ProductId)), i => i.Id, l => l.InvoiceId, (i, l) => new { i.Id, i.InvoiceDate, Value = l.LineTotal - l.DiscountAmount })
                    .GroupBy(x => new { x.InvoiceDate.Year, x.InvoiceDate.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Value = g.Sum(x => x.Value), Count = g.Select(x => x.Id).Distinct().Count() })
                    .AsEnumerable().Select(x => (x.Year, x.Month, x.Value, x.Count)).ToList())
                : Scope(null, ctx => Valid(ctx, from, to, scope)
                    .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Value = g.Sum(i => i.SubTotal - i.DiscountAmount), Count = g.Count() })
                    .AsEnumerable().Select(x => (x.Year, x.Month, x.Value, x.Count)).ToList());

        public List<(int PartyId, decimal Value, int Count)> ByParty(DateTime from, DateTime to, TradeScope scope = null) =>
            scope?.ProductIds is { } ids
                ? Scope(null, ctx => Valid(ctx, from, to, scope)
                    .Join(RowsOf<TLine>(ctx, LinesTable).AsNoTracking().Where(l => ids.Contains(l.ProductId)), i => i.Id, l => l.InvoiceId, (i, l) => new { i.Id, Party = EF.Property<int>(i, PartyColumn), Value = l.LineTotal - l.DiscountAmount })
                    .GroupBy(x => x.Party)
                    .Select(g => new { g.Key, Value = g.Sum(x => x.Value), Count = g.Select(x => x.Id).Distinct().Count() })
                    .AsEnumerable().Select(x => (x.Key, x.Value, x.Count)).ToList())
                : Scope(null, ctx => Valid(ctx, from, to, scope)
                    .GroupBy(i => EF.Property<int>(i, PartyColumn))
                    .Select(g => new { g.Key, Value = g.Sum(i => i.SubTotal - i.DiscountAmount), Count = g.Count() })
                    .AsEnumerable().Select(x => (x.Key, x.Value, x.Count)).ToList());

        public List<(int ProductId, decimal Qty, decimal Value)> ByProduct(DateTime from, DateTime to, TradeScope scope = null) =>
            Scope(null, ctx => Valid(ctx, from, to, scope)
                .Join(scope?.ProductIds is { } ids ? RowsOf<TLine>(ctx, LinesTable).AsNoTracking().Where(l => ids.Contains(l.ProductId)) : RowsOf<TLine>(ctx, LinesTable).AsNoTracking(),
                    i => i.Id, l => l.InvoiceId, (i, l) => l)
                .GroupBy(l => l.ProductId)
                .Select(g => new { g.Key, Qty = g.Sum(l => l.Qty), Value = g.Sum(l => l.LineTotal - l.DiscountAmount) })
                .AsEnumerable().Select(x => (x.Key, x.Qty, x.Value)).ToList());

        public List<(int PartyId, DateTime Date, DateTime? DueDate, decimal Remaining)> Open(TradeScope scope = null) =>
            Scope(null, ctx => Scoped(ctx, Rows(ctx).AsNoTracking(), scope)
                .Where(i => i.Status != InvoiceStatus.Cancelled && i.RemainingAmount > 0)
                .Select(i => new { Party = EF.Property<int>(i, PartyColumn), i.InvoiceDate, i.DueDate, i.RemainingAmount })
                .AsEnumerable().Select(x => (x.Party, x.InvoiceDate, x.DueDate, x.RemainingAmount)).ToList());

        public List<(int PartyId, DateTime First, DateTime Last)> FirstLast(DateTime to, TradeScope scope = null) =>
            Scope(null, ctx => Scoped(ctx, Rows(ctx).AsNoTracking(), scope)
                .Where(i => i.Status != InvoiceStatus.Cancelled && i.InvoiceDate < to.Date.AddDays(1))
                .GroupBy(i => EF.Property<int>(i, PartyColumn))
                .Select(g => new { g.Key, First = g.Min(i => i.InvoiceDate), Last = g.Max(i => i.InvoiceDate) })
                .AsEnumerable().Select(x => (x.Key, x.First, x.Last)).ToList());

        public (decimal Net, decimal Paid) Settled(DateTime from, DateTime to, TradeScope scope = null) =>
            Scope(null, ctx => Valid(ctx, from, to, scope)
                .GroupBy(_ => 1)
                .Select(g => new { Net = g.Sum(i => i.NetTotal), Paid = g.Sum(i => i.PaidAmount) })
                .AsEnumerable().Select(x => (x.Net, x.Paid)).FirstOrDefault());

        public List<TInvoice> OfParty(int partyId, PrimeDbContext db = null) =>
            Fetch(q => q.Where(i => EF.Property<int>(i, PartyColumn) == partyId && i.Status != InvoiceStatus.Cancelled)
                        .OrderBy(i => i.InvoiceDate).ThenBy(i => i.Id), db);

        public void SetSettlement(PrimeDbContext db, IEnumerable<(int Id, decimal Paid, decimal Remaining)> settled)
        {
            foreach (var (id, paid, remaining) in settled)
                Set(i => i.Id == id, s => s.SetProperty(i => i.PaidAmount, paid).SetProperty(i => i.RemainingAmount, remaining), db);
        }

        public List<int> UnsettledParties() =>
            Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(i => i.Status != InvoiceStatus.Cancelled && i.NetTotal > 0 && i.PaidAmount + i.RemainingAmount == 0)
                .Select(i => EF.Property<int>(i, PartyColumn)).Distinct().ToList());

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
