using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>سداد فواتير الطرف: يُعاد حسابه كاملاً بترتيب التاريخ بعد كل فاتورةٍ أو مرتجعٍ أو سند</summary>
    public sealed class Settlement
    {
        private readonly IInvoiceRepository<SalesInvoice, SalesInvoiceLine> _sales;
        private readonly IInvoiceRepository<PurchaseInvoice, PurchaseInvoiceLine> _purchases;
        private readonly IReturnRepository<SalesReturn, SalesReturnLine> _salesReturns;
        private readonly IReturnRepository<PurchaseReturn, PurchaseReturnLine> _purchaseReturns;
        private readonly IVoucherRepository _vouchers;
        private readonly IDocumentLinkRepository _links;

        public Settlement(IInvoiceRepository<SalesInvoice, SalesInvoiceLine> sales, IInvoiceRepository<PurchaseInvoice, PurchaseInvoiceLine> purchases,
            IReturnRepository<SalesReturn, SalesReturnLine> salesReturns, IReturnRepository<PurchaseReturn, PurchaseReturnLine> purchaseReturns,
            IVoucherRepository vouchers, IDocumentLinkRepository links)
        {
            _sales = sales; _purchases = purchases; _salesReturns = salesReturns; _purchaseReturns = purchaseReturns;
            _vouchers = vouchers; _links = links;
        }

        public void Recalculate(PrimeDbContext db, PartyKind kind, int? partyId)
        {
            if (partyId is not int id) return;
            if (kind == PartyKind.Customer) Run(db, _sales, _salesReturns, "SalesInvoices", "SalesReturns", VoucherKind.Receipt, id);
            else if (kind == PartyKind.Supplier) Run(db, _purchases, _purchaseReturns, "PurchaseInvoices", "PurchaseReturns", VoucherKind.Payment, id);
        }

        /// <summary>فواتيرٌ لم يُحسب سدادها قط، كالمُدخلة قبل التوزيع</summary>
        public void RecalculateUnsettled(PrimeDbContext db)
        {
            foreach (var id in _sales.UnsettledParties()) Recalculate(db, PartyKind.Customer, id);
            foreach (var id in _purchases.UnsettledParties()) Recalculate(db, PartyKind.Supplier, id);
        }

        /// <summary>الفاتورة المسمّاة في السند فاتورةٌ لهذا الطرف</summary>
        public Result CheckNamed(PartyKind kind, int partyId, IEnumerable<string> invoiceNos)
        {
            var known = (kind == PartyKind.Customer ? _sales.OfParty(partyId).Select(i => i.InvoiceNo) : _purchases.OfParty(partyId).Select(i => i.InvoiceNo)).ToHashSet();
            var unknown = invoiceNos.Where(n => !string.IsNullOrWhiteSpace(n) && !known.Contains(n)).ToList();
            return unknown.Count == 0 ? Result.Ok()
                : Result.Fail(LocalizationService.Get("Str.Voucher.InvoiceNotOfParty", string.Join("، ", unknown)), ErrorCode.ValidationFailed);
        }

        private void Run<TInvoice, TLine, TReturn, TReturnLine>(PrimeDbContext db, IInvoiceRepository<TInvoice, TLine> invoices,
            IReturnRepository<TReturn, TReturnLine> returns, string invoiceType, string returnType, VoucherKind voucherKind, int partyId)
            where TInvoice : InvoiceBase where TReturn : ReturnBase where TReturnLine : ReturnLineBase
        {
            var open = invoices.OfParty(partyId, db);
            var remaining = open.ToDictionary(i => i.Id, i => i.NetTotal);
            var oldestFirst = open.Select(i => i.Id).ToList();
            var idOf = open.GroupBy(i => i.InvoiceNo).ToDictionary(g => g.Key, g => g.First().Id);
            var noOf = open.ToDictionary(i => i.Id, i => i.InvoiceNo);

            var vouchers = _vouchers.OfParty(voucherKind, partyId, db);
            var named = _vouchers.AllocationsOf(vouchers.Select(v => v.Id), db).Where(a => !a.IsAuto)
                .ToLookup(a => a.VoucherId, a => (InvoiceId: idOf.GetValueOrDefault(a.InvoiceNo ?? ""), a.Amount));

            var credits = returns.OfParty(partyId, db)
                .Select(r => (r.ReturnDate, Order: 0, r.Id, r.NetTotal, Named: ReturnedTo(r.Id, returns, invoiceType, returnType, db), VoucherId: 0))
                .Concat(vouchers.Select(v => (ReturnDate: v.VoucherDate, Order: 1, v.Id, NetTotal: v.Amount, Named: named[v.Id].Where(n => n.InvoiceId != 0).ToList(), VoucherId: v.Id)))
                .OrderBy(c => c.ReturnDate).ThenBy(c => c.Order).ThenBy(c => c.Id);

            var autos = vouchers.ToDictionary(v => v.Id, _ => new List<VoucherAllocation>());
            foreach (var credit in credits)
            {
                var applied = SettlementCalc.Allocate(credit.NetTotal, credit.Named, oldestFirst, remaining);
                if (credit.VoucherId == 0) continue;
                autos[credit.VoucherId].AddRange(applied.Where(a => a.Auto).Select(a => new VoucherAllocation
                {
                    InvoiceType = invoiceType, InvoiceNo = noOf[a.InvoiceId], Amount = a.Amount
                }));
            }

            invoices.SetSettlement(db, open.Select(i => (i.Id, i.NetTotal - remaining[i.Id], remaining[i.Id])));
            foreach (var (voucherId, allocations) in autos) _vouchers.ReplaceAutoAllocations(db, voucherId, allocations);
        }

        /// <summary>المرتجع المسحوب من فاتورة يُنقص فاتورته نفسها بصافي سطوره</summary>
        private List<(int InvoiceId, decimal Amount)> ReturnedTo<TReturn, TReturnLine>(int returnId, IReturnRepository<TReturn, TReturnLine> returns,
            string invoiceType, string returnType, PrimeDbContext db) where TReturnLine : ReturnLineBase
        {
            var links = _links.GetByTarget(returnType, returnId, db).Where(l => l.SourceType == invoiceType).ToList();
            if (links.Count == 0) return new();
            var net = returns.GetLines(returnId, db).ToDictionary(l => l.Id, l => l.NetAmount);
            return links.GroupBy(l => l.SourceId).Select(g => (g.Key, g.Sum(l => net.GetValueOrDefault(l.TargetLineId)))).ToList();
        }
    }
}
