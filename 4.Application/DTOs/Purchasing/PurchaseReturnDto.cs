using System;
using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.Purchasing
{
    public class PurchaseReturnDto
    {
        public int      Id           { get; set; }
        public string   ReturnNo     { get; set; }
        public DateTime ReturnDate   { get; set; }
        public int      SupplierId   { get; set; }
        public string   SupplierName { get; set; }
        public int      WarehouseId  { get; set; }
        public decimal  SubTotal     { get; set; }
        public decimal  TaxAmount    { get; set; }
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
        public decimal TaxPercent  { get; set; }
        public decimal LineTotal   { get; set; }
        public string  Notes       { get; set; }
    }

    public class CreatePurchaseReturnLineDto
    {
        public int     LineNo      { get; set; }
        public string  ProductCode { get; set; }
        public decimal Qty         { get; set; }
        public decimal UnitPrice   { get; set; }
        public decimal TaxPercent  { get; set; }
        public string  Notes       { get; set; }
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
