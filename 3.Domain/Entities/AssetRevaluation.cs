using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>إعادة تقييم أصل</summary>
    public class AssetRevaluation : BaseModel
    {
        public int      AssetId        { get; set; }
        public System.DateTime RevaluationDate { get; set; }

        public decimal  OldValue       { get; set; }
        public decimal  NewValue       { get; set; }
        public string   Notes          { get; set; }
        public int?     JournalEntryId { get; set; }

        public decimal  Difference => NewValue - OldValue;
    }
}
