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
            RemoveIn<StockAdjustmentLine>(_lineTable, l => l.DocumentId == id, db);

            Remove(d => d.Id == id, db);
        }


        public List<StockAdjustmentLine> GetLines(int documentId, PrimeDbContext db = null) =>
            FetchOf<StockAdjustmentLine>(_lineTable,
                q => q.Where(l => l.DocumentId == documentId).OrderBy(l => l.LineNo), db);

        public Dictionary<int, decimal> TotalQty(IEnumerable<int> documentIds)
        {
            var ids = documentIds.Distinct().ToList();
            return Scope(null, ctx => RowsOf<StockAdjustmentLine>(ctx, _lineTable).AsNoTracking()
                .Where(l => ids.Contains(l.DocumentId))
                .GroupBy(l => l.DocumentId)
                .Select(g => new { g.Key, Qty = g.Sum(l => l.Qty) })
                .ToDictionary(x => x.Key, x => x.Qty));
        }

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
