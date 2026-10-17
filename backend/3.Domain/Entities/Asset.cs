using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان Asset</summary>
    public class Asset : BaseModel
    {
        public string   Code          { get; set; }
        public string   Name          { get; set; }
        public int?     CategoryId    { get; set; }
        public string   CategoryName  { get; set; }
        public System.DateTime? PurchaseDate { get; set; }
        public decimal  PurchaseCost  { get; set; }

        public decimal  RevaluedValue { get; set; }

        public decimal  CurrentValue  { get; set; }

        public int      UsefulLifeYears { get; set; }

        public decimal  SalvageValue    { get; set; }
        public decimal  AccumulatedDepreciation { get; set; }
        public System.DateTime? LastDepreciationDate { get; set; }
        public string   Location      { get; set; }
        public string   Notes         { get; set; }
        public bool     IsActive      { get; set; } = true;

        public string   AccountCode { get; set; }

        public string   DepreciationAccountCode { get; set; }

        public Enums.AssetAcquisition AcquisitionMethod { get; set; }

        public int?     FundingId { get; set; }

        public string   FundingAccountCode { get; set; }

        public int?     JournalEntryId { get; set; }
    }
}
