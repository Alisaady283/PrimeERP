using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories.Base
{
    /// <summary>أساس مستودع StockAdjustment</summary>
    public abstract class StockAdjustmentRepositoryBase : RepositoryBase<StockAdjustment>
    {
        private readonly string _headerTable, _lineTable;

        protected StockAdjustmentRepositoryBase(string headerTable, string lineTable)
        {
            _headerTable = headerTable;
            _lineTable = lineTable;
        }

        protected override string TableName => _headerTable;
        protected override string LineTable => _lineTable;


        public void DeleteDocument(PrimeDbContext db, int id)
        {
            Write(db =>          // السطور أولاً: النموذج بلا علاقة، فترتيب الحذف يدويّ
            {
                SetOf<StockAdjustmentLine>(db, _lineTable).RemoveRange(RowsOf<StockAdjustmentLine>(db, _lineTable).Where(l => l.DocumentId == id));
                return 0;
            }, db);

            Write(db =>
            {
                var head = Rows(db).AsTracking().FirstOrDefault(d => d.Id == id);
                if (head != null) SetOf(db).Remove(head);
                return 0;
            }, db);
        }


        public List<StockAdjustmentLine> GetLines(int documentId, PrimeDbContext db = null) =>
            FetchOf<StockAdjustmentLine>(_lineTable,
                q => q.Where(l => l.DocumentId == documentId).OrderBy(l => l.LineNo), db);

        public (List<StockAdjustment> Items, int Total) GetPaged(int page, int pageSize, string searchText,
                                                                 string sortColumn, bool sortDescending)
        {
            IQueryable<StockAdjustment> Shape(IQueryable<StockAdjustment> rows) =>
                string.IsNullOrWhiteSpace(searchText)
                    ? rows
                    : rows.Where(d => EF.Functions.Like(d.DocNo, $"%{searchText}%"));

            return Page(page, pageSize, Shape, Order(sortColumn, sortDescending));
        }

        private static Func<IQueryable<StockAdjustment>, IOrderedQueryable<StockAdjustment>> Order(string column, bool descending) =>
            column == "DocNo" ? DocumentOrder(d => d.DocNo,        descending, d => d.DocNo)
                              : DocumentOrder(d => d.MovementDate, descending, d => d.DocNo);

        public int InsertHeader(PrimeDbContext db, StockAdjustment doc) => Add(doc, db);

        public int InsertLine(PrimeDbContext db, int documentId, StockAdjustmentLine line)
        {
            line.DocumentId = documentId;
            Write(db => { SetOf<StockAdjustmentLine>(db, _lineTable).Add(line); return 0; }, db);
            return line.Id;
        }
    }
}
