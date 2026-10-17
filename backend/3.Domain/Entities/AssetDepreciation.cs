using System;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>قسط إهلاكٍ واحد</summary>
    public class AssetDepreciation : BaseModel
    {
        public int      AssetId        { get; set; }
        public string   AssetCode      { get; set; }
        public string   AssetName      { get; set; }

        public DateTime PeriodDate     { get; set; }
        public decimal  Amount         { get; set; }
        public int?     JournalEntryId { get; set; }
        public string   Notes          { get; set; }
    }
}
