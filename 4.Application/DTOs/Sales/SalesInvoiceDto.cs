using System;
using System.Collections.Generic;

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
        public string   StatusText   { get; set; }
        public DateTime CreatedAt    { get; set; }
    }

    public class SalesInvoiceDetailDto : SalesInvoiceDto
    {
        public List<SalesInvoiceLineDto> Lines { get; set; } = new();
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
    }

    public class SalesInvoiceFilter
    {
        public string SearchText     { get; set; }
        public int?   CustomerId     { get; set; }
        public string SortBy         { get; set; } = "InvoiceDate";
        public bool   SortDescending { get; set; } = true;
    }
}
