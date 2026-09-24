using System;
using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.Sales
{
    /// <summary>بيانات مرتجع البيع</summary>
    public class SalesReturnDto
    {
        public int      Id           { get; set; }
        public string   ReturnNo     { get; set; }
        public DateTime ReturnDate   { get; set; }
        public int      CustomerId   { get; set; }
        public string   CustomerName { get; set; }
        public int      WarehouseId  { get; set; }
        public decimal  SubTotal     { get; set; }
        public decimal  DiscountAmount    { get; set; }
        public decimal  VatAmount    { get; set; }
        public decimal  WithholdingAmount    { get; set; }
        public decimal  NetTotal     { get; set; }
        public DateTime CreatedAt    { get; set; }
    }

    public class SalesReturnDetailDto : SalesReturnDto
    {
        public List<SalesReturnLineDto> Lines { get; set; } = new();
    }

    public class SalesReturnLineDto
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

    public class CreateSalesReturnLineDto : PrimeERP.Application.DTOs.Documents.IPullableLine
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

    public class CreateSalesReturnDto
    {
        public int      Id          { get; set; }
        public DateTime ReturnDate  { get; set; } = DateTime.Today;
        public int      CustomerId  { get; set; }
        public int      WarehouseId { get; set; }
        public string   Notes       { get; set; }
        public List<CreateSalesReturnLineDto> Lines { get; set; } = new();
    }

    public class SalesReturnFilter
    {
        public string SearchText     { get; set; }
        public int?   CustomerId     { get; set; }
        public string SortBy         { get; set; } = "ReturnDate";
        public bool   SortDescending { get; set; } = true;
    }
}
