namespace PrimeERP.Application.DTOs.Inventory
{
    /// <summary>مرشّح الأصناف</summary>
    public class ProductFilter
    {
        public string SearchText     { get; set; }
        public bool?  IsActive       { get; set; }
        public int?   CategoryId     { get; set; }
        public string SortBy         { get; set; } = "Name";
        public bool   SortDescending { get; set; }
    }
}
