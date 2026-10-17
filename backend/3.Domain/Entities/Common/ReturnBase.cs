using System;

namespace PrimeERP.Domain.Entities.Common
{
    /// <summary>ما يتقاسمه مرتجعا البيع والشراء</summary>
    public abstract class ReturnBase : BaseModel
    {
        public string   ReturnNo          { get; set; }
        public DateTime ReturnDate        { get; set; }
        public int      WarehouseId       { get; set; }
        public decimal  SubTotal          { get; set; }
        public decimal  DiscountAmount    { get; set; }
        public decimal  VatAmount         { get; set; }
        public decimal  WithholdingAmount { get; set; }
        public decimal  NetTotal          { get; set; }
        public int?     JournalEntryId    { get; set; }
        public string   Notes             { get; set; }
    }

    public abstract class ReturnLineBase : DocumentLineBase
    {
        public int ReturnId { get; set; }
    }
}
