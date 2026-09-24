using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع StockTransfer</summary>
    public interface IStockTransferRepository
    {
        void DeleteDocument(PrimeDbContext db, int id);
        StockTransferDocument GetById(int id, PrimeDbContext db = null);
        List<StockTransferLine> GetLines(int documentId, PrimeDbContext db = null);
        (List<StockTransferDocument> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending);
        int InsertHeader(PrimeDbContext db, StockTransferDocument doc);
        void InsertLine(PrimeDbContext db, int documentId, StockTransferLine line);
    }

    public class StockTransferRepository : RepositoryBase<StockTransferDocument>, IStockTransferRepository
    {
        protected override string TableName => "StockTransferDocuments";


        protected override string LineTable => "StockTransferLines";
        protected override string LineForeignKey => "DocumentId";

        public void DeleteDocument(PrimeDbContext db, int id)
        {
            Write(db =>          // السطور أولاً: النموذج بلا علاقة، فترتيب الحذف يدويّ
            {
                SetOf<StockTransferLine>(db, LineTable)
                    .RemoveRange(RowsOf<StockTransferLine>(db, LineTable).Where(l => l.DocumentId == id));
                return 0;
            }, db);

            Write(db =>
            {
                var head = Rows(db).AsTracking().FirstOrDefault(d => d.Id == id);
                if (head != null) SetOf(db).Remove(head);
                return 0;
            }, db);
        }


        public List<StockTransferLine> GetLines(int documentId, PrimeDbContext db = null) =>
            FetchOf<StockTransferLine>(LineTable,
                q => q.Where(l => l.DocumentId == documentId).OrderBy(l => l.LineNo), db);

        public (List<StockTransferDocument> Items, int Total) GetPaged(int page, int pageSize, string searchText,
                                                                       string sortColumn, bool sortDescending)
        {
            IQueryable<StockTransferDocument> Shape(IQueryable<StockTransferDocument> rows) =>
                string.IsNullOrWhiteSpace(searchText)
                    ? rows
                    : rows.Where(d => EF.Functions.Like(d.DocNo, $"%{searchText}%"));

            return Page(page, pageSize, Shape,
                sortColumn == "DocNo" ? DocumentOrder(d => d.DocNo,        sortDescending, d => d.DocNo)
                                      : DocumentOrder(d => d.MovementDate, sortDescending, d => d.DocNo));
        }

        public int InsertHeader(PrimeDbContext db, StockTransferDocument doc) => Add(doc, db);

        public void InsertLine(PrimeDbContext db, int documentId, StockTransferLine line)
        {
            line.DocumentId = documentId;
            Write(db => { SetOf<StockTransferLine>(db, LineTable).Add(line); return 0; }, db);
        }
    }
}
