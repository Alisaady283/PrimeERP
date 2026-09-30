using System;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;


namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع CycleDocument</summary>
    public interface ICycleDocumentRepository
    {
        CycleDocument GetById(int id, PrimeDbContext db = null);
        List<CycleDocumentLine> GetLines(int documentId, PrimeDbContext db = null);
        Dictionary<int, (decimal Qty, decimal Total)> Totals(IEnumerable<int> documentIds);
        (List<CycleDocument> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending);
        int InsertHeader(PrimeDbContext db, CycleDocument doc);
        int InsertLine(PrimeDbContext db, int documentId, CycleDocumentLine line);
        void DeleteDocument(int id);
        void DeleteDocument(PrimeDbContext db, int id);
    }

    public interface IPurchaseRequestRepository : ICycleDocumentRepository { }
    public interface IPurchaseOrderRepository : ICycleDocumentRepository { }
    public interface IQuotationRepository : ICycleDocumentRepository { }
    public interface ISalesOrderRepository : ICycleDocumentRepository { }


    public class PurchaseRequestRepository : CycleDocumentRepositoryBase, IPurchaseRequestRepository
    {
        public PurchaseRequestRepository() : base("PurchaseRequestDocuments", "PurchaseRequestLines") { }
    }

    public class PurchaseOrderRepository : CycleDocumentRepositoryBase, IPurchaseOrderRepository
    {
        public PurchaseOrderRepository() : base("PurchaseOrderDocuments", "PurchaseOrderLines") { }
    }

    public class QuotationRepository : CycleDocumentRepositoryBase, IQuotationRepository
    {
        public QuotationRepository() : base("QuotationDocuments", "QuotationLines") { }
    }

    public class SalesOrderRepository : CycleDocumentRepositoryBase, ISalesOrderRepository
    {
        public SalesOrderRepository() : base("SalesOrderDocuments", "SalesOrderLines") { }
    }
}
