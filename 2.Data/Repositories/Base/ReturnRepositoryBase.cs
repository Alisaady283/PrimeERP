using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Data.Repositories.Base
{
    /// <summary>عقد مستودع المرتجعات</summary>
    public interface IReturnRepository<TReturn, TLine>
    {
        void DeleteDocument(PrimeDbContext db, int id);
        TReturn GetById(int id, PrimeDbContext db = null);
        List<TLine> GetLines(int returnId, PrimeDbContext db = null);
        (List<TReturn> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? partyId,
                                                  string sortColumn, bool sortDescending);

        int InsertHeader(PrimeDbContext db, TReturn document);
        int InsertLine(PrimeDbContext db, int returnId, TLine line);
        void SetJournalEntryId(PrimeDbContext db, int returnId, int journalEntryId);
    }

    /// <summary>أساس مرتجع: رأس وسطور</summary>
    public abstract class ReturnRepositoryBase<TReturn, TLine> : RepositoryBase<TReturn>
        where TReturn : ReturnBase, new()
        where TLine : ReturnLineBase, new()
    {
        protected abstract string HeaderTable { get; }
        protected abstract string LinesTable { get; }
        protected abstract string PartyColumn { get; }
        protected abstract int PartyIdOf(TReturn ret);
        protected abstract void ApplyPartyId(TReturn ret, int partyId);

        protected override string TableName => HeaderTable;
        protected override string LineTable => LinesTable;
        protected override string LineForeignKey => "ReturnId";


        public void DeleteDocument(PrimeDbContext db, int id)
        {
            RemoveIn<TLine>(LinesTable, l => l.ReturnId == id, db);

            Remove(r => r.Id == id, db);
        }


        public List<TLine> GetLines(int returnId, PrimeDbContext db = null) =>
            FetchOf<TLine>(LinesTable, q => q.Where(l => l.ReturnId == returnId).OrderBy(l => l.LineNo), db);

        public (List<TReturn> Items, int Total) GetPaged(int page, int pageSize, string searchText, int? partyId,
                                                         string sortColumn, bool sortDescending)
        {
            IQueryable<TReturn> Shape(IQueryable<TReturn> rows)
            {
                var q = rows;
                if (!string.IsNullOrWhiteSpace(searchText))
                    q = q.Where(r => EF.Functions.Like(r.ReturnNo, $"%{searchText}%"));
                if (partyId != null)
                    q = q.Where(r => EF.Property<int>(r, PartyColumn) == partyId);
                return q;
            }

            return Page(page, pageSize, Shape, Order(sortColumn, sortDescending));
        }

        private static Func<IQueryable<TReturn>, IOrderedQueryable<TReturn>> Order(string column, bool descending) => column switch
        {
            "ReturnNo" => DocumentOrder(r => r.ReturnNo,   descending, r => r.ReturnNo),
            "NetTotal" => DocumentOrder(r => r.NetTotal,   descending, r => r.ReturnNo),
            _          => DocumentOrder(r => r.ReturnDate, descending, r => r.ReturnNo),
        };

        public int InsertHeader(PrimeDbContext db, TReturn ret) => Add(ret, db);

        public int InsertLine(PrimeDbContext db, int returnId, TLine line)
        {
            line.ReturnId = returnId;
            Write(db => { SetOf<TLine>(db, LinesTable).Add(line); return 0; }, db);
            return line.Id;
        }

        public void SetJournalEntryId(PrimeDbContext db, int returnId, int journalEntryId) =>
            Set(r => r.Id == returnId, s => s.SetProperty(r => r.JournalEntryId, journalEntryId), db);
    }
}
