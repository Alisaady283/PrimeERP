using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>بيع أصل واستبعاده</summary>
    public class AssetDisposal : BaseModel
    {
        public int      AssetId      { get; set; }
        public System.DateTime DisposalDate { get; set; }

        public int      TreasuryId   { get; set; }
        public decimal  SalePrice    { get; set; }

        public decimal  AssetValue   { get; set; }
        public decimal  AccumulatedDepreciation { get; set; }

        public string   Notes        { get; set; }
        public int?     JournalEntryId { get; set; }

        public decimal  BookValue  => AssetValue - AccumulatedDepreciation;

        public decimal  GainOrLoss => SalePrice - BookValue;
    }
}
