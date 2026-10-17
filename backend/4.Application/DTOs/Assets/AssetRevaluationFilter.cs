namespace PrimeERP.Application.DTOs.Assets
{
    /// <summary>مرشّح إعادة التقييم</summary>
    public class AssetRevaluationFilter
    {
        public string SearchText     { get; set; }
        public int?   AssetId        { get; set; }
        public string SortBy         { get; set; } = "RevaluationDate";
        public bool   SortDescending { get; set; } = true;
    }
}
