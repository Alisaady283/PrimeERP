namespace PrimeERP.Application.DTOs.Assets
{
    /// <summary>مرشّح استبعاد الأصول</summary>
    public class AssetDisposalFilter
    {
        public string SearchText     { get; set; }
        public int?   AssetId        { get; set; }
        public string SortBy         { get; set; } = "DisposalDate";
        public bool   SortDescending { get; set; } = true;
    }
}
