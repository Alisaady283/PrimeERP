using System;
using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.Sales
{
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

    public class SalesInvoiceLineDto
    {
        public int     LineNo      { get; set; }
        public string  ProductCode { get; set; }
        public string  ProductName { get; set; }
        public decimal Qty         { get; set; }
        public decimal UnitPrice   { get; set; }
        public decimal DiscountPercent  { get; set; }
        public decimal DiscountAmount  { get; set; }
        public decimal VatPercent  { get; set; }
        public decimal VatAmount  { get; set; }
        public decimal WithholdingPercent  { get; set; }
        public decimal WithholdingAmount  { get; set; }
        public decimal LineTotal   { get; set; }
        public decimal NetAmount   { get; set; }
        public string  Notes       { get; set; }
    }

    public class CreateSalesInvoiceLineDto
    {
        public int     LineNo      { get; set; }
        public string  ProductCode { get; set; }
        public decimal Qty         { get; set; }
        public decimal UnitPrice   { get; set; }
        public decimal DiscountPercent  { get; set; }
        public decimal VatPercent  { get; set; }
        public decimal WithholdingPercent  { get; set; }
        public string  Notes       { get; set; }
    }

    // نوع واحد لكل من الإنشاء والتعديل مثل CreateJournalDto — Update مرفوضة دائماً هنا فعلياً (الفاتورة
    // تُرحَّل فوراً عند الإنشاء)، Id يبقى بلا استخدام فعلي.
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
