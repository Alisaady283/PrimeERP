using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories.Base
{
    /// <summary>أساس مستودع CycleDocument</summary>
    public abstract class CycleDocumentRepositoryBase : RepositoryBase<CycleDocument>, ICycleDocumentRepository
    {
        private readonly string _headerTable, _lineTable;

        protected CycleDocumentRepositoryBase(string headerTable, string lineTable)
        {
            _headerTable = headerTable;
            _lineTable = lineTable;
        }

        protected override string TableName => _headerTable;
        protected override string LineTable => _lineTable;



        public List<CycleDocumentLine> GetLines(int documentId, PrimeDbContext db = null) =>
            FetchOf<CycleDocumentLine>(_lineTable,
                q => q.Where(l => l.DocumentId == documentId).OrderBy(l => l.LineNo), db);

        public (List<CycleDocument> Items, int Total) GetPaged(int page, int pageSize, string searchText,
                                                               string sortColumn, bool sortDescending)
        {
            IQueryable<CycleDocument> Shape(IQueryable<CycleDocument> rows) =>
                string.IsNullOrWhiteSpace(searchText)
                    ? rows
                    : rows.Where(d => EF.Functions.Like(d.DocNo, $"%{searchText}%"));

            return Page(page, pageSize, Shape, Order(sortColumn, sortDescending));
        }

        private static Func<IQueryable<CycleDocument>, IOrderedQueryable<CycleDocument>> Order(string column, bool descending) =>
            column == "DocNo" ? DocumentOrder(d => d.DocNo,   descending, d => d.DocNo)
                              : DocumentOrder(d => d.DocDate, descending, d => d.DocNo);

        public int InsertHeader(PrimeDbContext db, CycleDocument doc) => Add(doc, db);

        public int InsertLine(PrimeDbContext db, int documentId, CycleDocumentLine line)
        {
            line.DocumentId = documentId;
            Write(db => { SetOf<CycleDocumentLine>(db, _lineTable).Add(line); return 0; }, db);
            return line.Id;
        }

        public void DeleteDocument(int id) => DbContextFactory.RunTransaction(db => DeleteDocument(db, id));

        public void DeleteDocument(PrimeDbContext db, int id)
        {
            Write(db =>          // السطور أولاً: النموذج بلا علاقة، فترتيب الحذف يدويّ
            {
                SetOf<CycleDocumentLine>(db, _lineTable).RemoveRange(RowsOf<CycleDocumentLine>(db, _lineTable).Where(l => l.DocumentId == id));
                return 0;
            }, db);

            Write(db =>
            {
                var head = Rows(db).AsTracking().FirstOrDefault(d => d.Id == id);
                if (head != null) SetOf(db).Remove(head);
                return 0;
            }, db);
        }
    }
}
