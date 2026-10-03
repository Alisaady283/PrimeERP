using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Documents;

namespace PrimeERP.Application.DTOs.Purchasing
{
    /// <summary>بيانات فاتورة الشراء</summary>
    public class PurchaseInvoiceDto
    {
        public int      Id            { get; set; }
        public string   InvoiceNo     { get; set; }
        public DateTime InvoiceDate   { get; set; }
        public int      SupplierId    { get; set; }
        public string   SupplierName  { get; set; }
        public int      WarehouseId   { get; set; }
        public decimal  SubTotal      { get; set; }
        public decimal  DiscountAmount     { get; set; }
        public decimal  VatAmount     { get; set; }
        public decimal  WithholdingAmount     { get; set; }
        public decimal  NetTotal      { get; set; }
        public string   StatusText    { get; set; }
        public DateTime CreatedAt     { get; set; }
    }

    public class PurchaseInvoiceDetailDto : PurchaseInvoiceDto, ISourceDocument
    {
        public List<PurchaseInvoiceLineDto> Lines { get; set; } = new();

        string   ISourceDocument.DocNo     => InvoiceNo;
        DateTime ISourceDocument.DocDate   => InvoiceDate;
        string   ISourceDocument.PartyName => SupplierName;
        IEnumerable<ISourceLine> ISourceDocument.Lines => Lines;
    }

    public class PurchaseInvoiceLineDto : PrimeERP.Application.DTOs.Documents.TradeLineDto { }

    public class CreatePurchaseInvoiceLineDto : PrimeERP.Application.DTOs.Documents.CreateTradeLineDto { }

    public class CreatePurchaseInvoiceDto
    {
        public int      Id          { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.Today;
        public int      SupplierId  { get; set; }
        public int      WarehouseId { get; set; }
        public string   Notes       { get; set; }
        public List<CreatePurchaseInvoiceLineDto> Lines { get; set; } = new();
    }

    public class PurchaseInvoiceFilter
    {
        public string SearchText     { get; set; }
        public int?   SupplierId     { get; set; }
        public string SortBy         { get; set; } = "InvoiceDate";
        public bool   SortDescending { get; set; } = true;
    }
}
