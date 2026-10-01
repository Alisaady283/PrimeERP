using System;
using System.Collections.Generic;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان CycleDocument</summary>
    public class CycleDocument : BaseModel
    {
        public string   DocNo    { get; set; }
        public DateTime DocDate  { get; set; }
        public int?     PartyId  { get; set; }
        public string   Notes    { get; set; }

        public List<CycleDocumentLine> Lines { get; set; } = new();
    }

    public class CycleDocumentLine : IProductLine
    {
        public int     Id          { get; set; }
        public int     DocumentId  { get; set; }
        public int     LineNo      { get; set; }
        public int     ProductId   { get; set; }
        public string  ProductCode { get; set; }
        public string  ProductName { get; set; }
        public decimal Qty         { get; set; }
        public decimal UnitPrice   { get; set; }
        public string  Notes       { get; set; }
    }
}
