namespace PrimeERP.Application.DTOs.Assets
{
    /// <summary>مرشّح الأصول</summary>
    public class AssetFilter
    {
        public string SearchText     { get; set; }
        public bool?  IsActive       { get; set; }
        public int?   CategoryId     { get; set; }
        public string SortBy         { get; set; } = "Name";
        public bool   SortDescending { get; set; }
    }
}
