using System;
using System.Collections.Generic;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class PurchaseReturn : BaseModel
    {
        public string   ReturnNo    { get; set; }
        public DateTime ReturnDate  { get; set; }
        public int      SupplierId  { get; set; }
        public int      WarehouseId { get; set; }
        public decimal  SubTotal    { get; set; }
        public decimal  TaxAmount   { get; set; }
        public decimal  NetTotal    { get; set; }
        public int?     JournalEntryId { get; set; }
        public string   Notes       { get; set; }

        public List<PurchaseReturnLine> Lines { get; set; } = new();
    }

    public class PurchaseReturnLine
    {
        public int     Id          { get; set; }
        public int     ReturnId    { get; set; }
        public int     LineNo      { get; set; }
        public int     ProductId   { get; set; }
        public string  ProductCode { get; set; }
        public string  ProductName { get; set; }
        public decimal Qty         { get; set; }
        public decimal UnitPrice   { get; set; }
        public decimal TaxPercent  { get; set; }
        public decimal TaxAmount   { get; set; }
        public decimal LineTotal   { get; set; }
        public string  Notes       { get; set; }
    }
}
