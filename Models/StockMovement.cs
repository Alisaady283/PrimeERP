using System;
using PrimeERP.Core;
using PrimeERP.Models.Common;

namespace PrimeERP.Models
{
    public class StockMovement : BaseModel
    {
        public string       MovementNo    { get; set; }
        public DateTime     MovementDate  { get; set; }
        public int          ProductId     { get; set; }
        public int          WarehouseId   { get; set; }
        public MovementType MovementType  { get; set; }
        public decimal      Qty           { get; set; }
        public decimal      UnitCost      { get; set; }
        public decimal      TotalCost     { get; set; }
        public decimal      BalanceAfter  { get; set; }
        public string       SourceDocType { get; set; }
        public int?         SourceDocId   { get; set; }
        public string       SourceDocNo   { get; set; }
        public string       Notes         { get; set; }
    }
}
