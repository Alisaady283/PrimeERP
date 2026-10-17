using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>إعادة تقييم أصل</summary>
    public class AssetRevaluation : BaseModel
    {
        public int      AssetId        { get; set; }
        public string   AssetCode      { get; set; }
        public string   AssetName      { get; set; }
        public System.DateTime RevaluationDate { get; set; }

        public decimal  OldValue       { get; set; }
        public decimal  NewValue       { get; set; }
        public decimal  SalvageValue   { get; set; }
        public int      UsefulLifeYears { get; set; }
        public decimal  Difference     { get; set; }
        public string   KindName       { get; set; }
        public string   Notes          { get; set; }
        public int?     JournalEntryId { get; set; }

    }
}
