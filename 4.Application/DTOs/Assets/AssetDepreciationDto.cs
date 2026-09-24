using System;

namespace PrimeERP.Application.DTOs.Assets
{
    /// <summary>قسط إهلاكٍ كما يُعرَض</summary>
    public class AssetDepreciationDto
    {
        public int      Id          { get; set; }
        public int      AssetId     { get; set; }
        public string   AssetCode   { get; set; }
        public string   AssetName   { get; set; }
        public DateTime PeriodDate  { get; set; }
        public decimal  Amount      { get; set; }
        public int?     JournalEntryId { get; set; }
        public string   Notes       { get; set; }
        public DateTime CreatedAt   { get; set; }

        public bool CanEdit   { get; set; }
        public bool CanDelete { get; set; }
    }

    public class CreateAssetDepreciationDto
    {
        public int      AssetId    { get; set; }
        public DateTime PeriodDate { get; set; } = DateTime.Today;
        public decimal  Amount     { get; set; }
        public string   Notes      { get; set; }
    }

    public class UpdateAssetDepreciationDto
    {
        public int      Id         { get; set; }
        public int      AssetId    { get; set; }
        public DateTime PeriodDate { get; set; }
        public decimal  Amount     { get; set; }
        public string   Notes      { get; set; }
    }

    public class AssetDepreciationFilter
    {
        public string SearchText     { get; set; }
        public int?   AssetId        { get; set; }
        public string SortBy         { get; set; } = "PeriodDate";
        public bool   SortDescending { get; set; } = true;
    }
}
