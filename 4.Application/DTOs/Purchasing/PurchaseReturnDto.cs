using System;
using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.Purchasing
{
    /// <summary>بيانات مرتجع الشراء</summary>
    public class PurchaseReturnDto
    {
        public int      Id           { get; set; }
        public string   ReturnNo     { get; set; }
        public DateTime ReturnDate   { get; set; }
        public int      SupplierId   { get; set; }
        public string   SupplierName { get; set; }
        public int      WarehouseId  { get; set; }
        public decimal  SubTotal     { get; set; }
        public decimal  DiscountAmount    { get; set; }
        public decimal  VatAmount    { get; set; }
        public decimal  WithholdingAmount    { get; set; }
        public decimal  NetTotal     { get; set; }
        public DateTime CreatedAt    { get; set; }
    }

    public class PurchaseReturnDetailDto : PurchaseReturnDto
    {
        public List<PurchaseReturnLineDto> Lines { get; set; } = new();
    }

    public class PurchaseReturnLineDto
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

    public class CreatePurchaseReturnLineDto : PrimeERP.Application.DTOs.Documents.IPullableLine
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

    public class CreatePurchaseReturnDto
    {
        public int      Id          { get; set; }
        public DateTime ReturnDate  { get; set; } = DateTime.Today;
        public int      SupplierId  { get; set; }
        public int      WarehouseId { get; set; }
        public string   Notes       { get; set; }
        public List<CreatePurchaseReturnLineDto> Lines { get; set; } = new();
    }

    public class PurchaseReturnFilter
    {
        public string SearchText     { get; set; }
        public int?   SupplierId     { get; set; }
        public string SortBy         { get; set; } = "ReturnDate";
        public bool   SortDescending { get; set; } = true;
    }
}
