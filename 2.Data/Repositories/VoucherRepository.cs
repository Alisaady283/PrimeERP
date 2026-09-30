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

        public void Delete(PrimeDbContext db, int id)
        {
            RemoveIn<VoucherAllocation>(Allocations, a => a.VoucherId == id, db);

            Remove(v => v.Id == id, db);
        }
    }
}
