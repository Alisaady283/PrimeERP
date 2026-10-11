using System;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class PosShift : BaseModel
    {
        public int?      BranchId     { get; set; }
        public int       UserId       { get; set; }
        public string    UserName     { get; set; }
        public int       TreasuryId   { get; set; }
        public int       WarehouseId  { get; set; }
        public DateTime  OpenedAt     { get; set; }
        public DateTime? ClosedAt     { get; set; }
        public decimal   OpeningCash  { get; set; }
        public decimal   CashIn       { get; set; }
        public decimal   CashOut      { get; set; }
        public int       SalesCount   { get; set; }
        public int       ReturnsCount { get; set; }
        public decimal?  CountedCash  { get; set; }
        public decimal?  Difference   { get; set; }
        public int?      JournalEntryId { get; set; }
    }
}
