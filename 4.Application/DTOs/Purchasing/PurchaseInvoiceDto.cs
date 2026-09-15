using System;
using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.Purchasing
{
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

    public class PurchaseInvoiceDetailDto : PurchaseInvoiceDto
    {
        public List<PurchaseInvoiceLineDto> Lines { get; set; } = new();
    }

    public class PurchaseInvoiceLineDto
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

    public class CreatePurchaseInvoiceLineDto : PrimeERP.Application.DTOs.Documents.IPullableLine
    {
        public int     LineNo      { get; set; }
        public string  ProductCode { get; set; }
        public decimal Qty         { get; set; }
        public decimal UnitPrice   { get; set; }
        public decimal DiscountPercent  { get; set; }
        public decimal VatPercent  { get; set; }
        public decimal WithholdingPercent  { get; set; }
        public string  Notes       { get; set; }

        public string  SourceType   { get; set; }
        public int     SourceId     { get; set; }
        public string  SourceNo     { get; set; }
        public int     SourceLineId { get; set; }
    }

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
