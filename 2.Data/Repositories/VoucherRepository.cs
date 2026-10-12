using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع Voucher</summary>
    public interface IVoucherRepository
    {
        Voucher GetById(int id, PrimeDbContext db = null);
        List<VoucherAllocation> GetAllocations(int voucherId, PrimeDbContext db = null);
        (List<Voucher> Items, int Total) GetPaged(VoucherKind kind, int page, int pageSize, string searchText, bool sortDescending);
        int InsertHeader(PrimeDbContext db, Voucher v);
        void InsertAllocation(PrimeDbContext db, int voucherId, VoucherAllocation a);
        void SetLinks(PrimeDbContext db, int voucherId, int? journalEntryId, int? chequeId);
        void Delete(PrimeDbContext db, int id);
        List<Voucher> OfParty(VoucherKind kind, int partyId, PrimeDbContext db = null);
        List<VoucherAllocation> AllocationsOf(IEnumerable<int> voucherIds, PrimeDbContext db = null);
        void ReplaceAutoAllocations(PrimeDbContext db, int voucherId, IEnumerable<VoucherAllocation> allocations);
        List<(DateTime Date, string VoucherNo, string InvoiceNo, decimal Amount)> PaymentsOf(VoucherKind kind, IEnumerable<string> invoiceNos);
    }

    public class VoucherRepository : RepositoryBase<Voucher>, IVoucherRepository
    {
        protected override string TableName => "Vouchers";


        private const string Allocations = "VoucherAllocations";


        public List<VoucherAllocation> GetAllocations(int voucherId, PrimeDbContext db = null) =>
            FetchOf<VoucherAllocation>(Allocations,
                q => q.Where(a => a.VoucherId == voucherId).OrderBy(a => a.LineNo), db);

        public (List<Voucher> Items, int Total) GetPaged(VoucherKind kind, int page, int pageSize,
                                                         string searchText, bool sortDescending)
        {
            IQueryable<Voucher> Shape(IQueryable<Voucher> rows)
            {
                var q = rows.Where(v => v.Kind == kind);
                if (!string.IsNullOrWhiteSpace(searchText))
                    q = q.Where(v => EF.Functions.Like(v.VoucherNo, $"%{searchText}%")
                                  || EF.Functions.Like(v.Reference, $"%{searchText}%"));
                return q;
            }

            return Page(page, pageSize, Shape, DocumentOrder(v => v.VoucherDate, sortDescending, v => v.VoucherNo));
        }

        public int InsertHeader(PrimeDbContext db, Voucher v) => Add(v, db);

        public void InsertAllocation(PrimeDbContext db, int voucherId, VoucherAllocation a) =>
            Write(db =>
            {
                a.VoucherId = voucherId;
                SetOf<VoucherAllocation>(db, Allocations).Add(a);
                return 0;
            }, db);

        public void SetLinks(PrimeDbContext db, int voucherId, int? journalEntryId, int? chequeId) =>
            Edit(v => v.Id == voucherId, row =>
            {
                row.JournalEntryId = journalEntryId;
                row.ChequeId = chequeId;
            }, db);

        public List<Voucher> OfParty(VoucherKind kind, int partyId, PrimeDbContext db = null) =>
            Fetch(q => q.Where(v => v.Kind == kind && v.PartyId == partyId).OrderBy(v => v.VoucherDate).ThenBy(v => v.Id), db);

        public List<VoucherAllocation> AllocationsOf(IEnumerable<int> voucherIds, PrimeDbContext db = null)
        {
            var ids = voucherIds.Distinct().ToList();
            return FetchOf<VoucherAllocation>(Allocations, q => q.Where(a => ids.Contains(a.VoucherId)).OrderBy(a => a.LineNo), db);
        }

        public void ReplaceAutoAllocations(PrimeDbContext db, int voucherId, IEnumerable<VoucherAllocation> allocations)
        {
            RemoveIn<VoucherAllocation>(Allocations, a => a.VoucherId == voucherId && a.IsAuto, db);
            var next = GetAllocations(voucherId, db).Select(a => a.LineNo).DefaultIfEmpty(0).Max();
            foreach (var allocation in allocations)
            {
                allocation.IsAuto = true;
                allocation.LineNo = ++next;
                InsertAllocation(db, voucherId, allocation);
            }
        }

        public List<(DateTime Date, string VoucherNo, string InvoiceNo, decimal Amount)> PaymentsOf(VoucherKind kind, IEnumerable<string> invoiceNos)
        {
            var numbers = invoiceNos.Distinct().ToList();
            return Scope(null, ctx => RowsOf<VoucherAllocation>(ctx, Allocations).AsNoTracking()
                .Where(a => numbers.Contains(a.InvoiceNo))
                .Join(Rows(ctx).AsNoTracking().Where(v => v.Kind == kind), a => a.VoucherId, v => v.Id, (a, v) => new { v.VoucherDate, v.VoucherNo, a.InvoiceNo, a.Amount })
                .OrderBy(x => x.VoucherDate)
                .AsEnumerable().Select(x => (x.VoucherDate, x.VoucherNo, x.InvoiceNo, x.Amount)).ToList());
        }

        public void Delete(PrimeDbContext db, int id)
        {
            RemoveIn<VoucherAllocation>(Allocations, a => a.VoucherId == id, db);

            Remove(v => v.Id == id, db);
        }
    }
}
