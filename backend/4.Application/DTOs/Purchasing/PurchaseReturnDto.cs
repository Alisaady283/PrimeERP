using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Documents;

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

    public class PurchaseReturnDetailDto : PurchaseReturnDto, ISourceDocument
    {
        public List<PurchaseReturnLineDto> Lines { get; set; } = new();

        string   ISourceDocument.DocNo     => ReturnNo;
        DateTime ISourceDocument.DocDate   => ReturnDate;
        string   ISourceDocument.PartyName => SupplierName;
        IEnumerable<ISourceLine> ISourceDocument.Lines => Lines;
    }

    public class PurchaseReturnLineDto : PrimeERP.Application.DTOs.Documents.TradeLineDto { }

    public class CreatePurchaseReturnLineDto : PrimeERP.Application.DTOs.Documents.CreateTradeLineDto { }

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
