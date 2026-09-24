using System;
using System.Collections.Generic;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان StockAdjustment</summary>
    public class StockAdjustment : BaseModel
    {
        public string   DocNo         { get; set; }
        public DateTime MovementDate  { get; set; }
        public int      WarehouseId   { get; set; }
        public string   Notes         { get; set; }

        public List<StockAdjustmentLine> Lines { get; set; } = new();
    }

    public class StockAdjustmentLine
    {
        public int     Id          { get; set; }
        public int     DocumentId  { get; set; }
        public int     LineNo      { get; set; }
        public int     ProductId   { get; set; }
        public string  ProductCode { get; set; }
        public string  ProductName { get; set; }
        public decimal Qty         { get; set; }
        public decimal UnitCost    { get; set; }
        public string  Notes       { get; set; }
    }
}
