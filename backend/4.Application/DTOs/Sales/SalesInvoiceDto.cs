using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Documents;

namespace PrimeERP.Application.DTOs.Sales
{
    /// <summary>بيانات فاتورة البيع</summary>
    public class SalesInvoiceDto
    {
        public int      Id           { get; set; }
        public string   InvoiceNo    { get; set; }
        public DateTime InvoiceDate  { get; set; }
        public int      CustomerId   { get; set; }
        public string   CustomerName { get; set; }
        public int      WarehouseId  { get; set; }
        public string   WarehouseName { get; set; }
        public decimal  SubTotal     { get; set; }
        public decimal  DiscountAmount    { get; set; }
        public decimal  VatAmount    { get; set; }
        public decimal  WithholdingAmount    { get; set; }
        public decimal  NetTotal     { get; set; }
        public decimal  PaidAmount      { get; set; }
        public decimal  RemainingAmount { get; set; }
        public DateTime? DueDate        { get; set; }
        public string   StatusText   { get; set; }
        public PrimeERP.Domain.Results.StatusVariant StatusVariant { get; set; }
        public DateTime CreatedAt    { get; set; }
    }

    public class SalesInvoiceDetailDto : SalesInvoiceDto, ISourceDocument
    {
        public List<SalesInvoiceLineDto> Lines { get; set; } = new();

        /// <summary>رمز الفاتورة الإلكترونية المقبولة — يُطبع QR</summary>
        public string EInvoiceQr { get; set; }

        string   ISourceDocument.DocNo     => InvoiceNo;
        DateTime ISourceDocument.DocDate   => InvoiceDate;
        string   ISourceDocument.PartyName => CustomerName;
        IEnumerable<ISourceLine> ISourceDocument.Lines => Lines;
    }

    public class SalesInvoiceLineDto : PrimeERP.Application.DTOs.Documents.TradeLineDto { }

    public class CreateSalesInvoiceLineDto : PrimeERP.Application.DTOs.Documents.CreateTradeLineDto { }

    public class CreateSalesInvoiceDto
    {
        public int      Id          { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.Today;
        public int      CustomerId  { get; set; }
        public int      WarehouseId { get; set; }
        public string   Notes       { get; set; }
        public List<CreateSalesInvoiceLineDto> Lines { get; set; } = new();

        /// <summary>تكتبه نقطة البيع وحدها، فلا يُقرأ من الطلب</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public int?     PosShiftId  { get; set; }
    }

    public class SalesInvoiceFilter
    {
        public string SearchText     { get; set; }
        public int?   CustomerId     { get; set; }
        public string SortBy         { get; set; } = "InvoiceDate";
        public bool   SortDescending { get; set; } = true;
    }
}
