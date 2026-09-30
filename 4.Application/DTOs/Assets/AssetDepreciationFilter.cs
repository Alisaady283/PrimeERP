namespace PrimeERP.Application.DTOs.Assets
{
    /// <summary>مرشّح أقساط الإهلاك</summary>
    public class AssetDepreciationFilter
    {
        public string SearchText     { get; set; }
        public int?   AssetId        { get; set; }
        public string SortBy         { get; set; } = "PeriodDate";
        public bool   SortDescending { get; set; } = true;
    }
}
