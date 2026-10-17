namespace PrimeERP.Application.DTOs.Common
{
    /// <summary>مرشّح الفئات</summary>
    public class CategoryFilter
    {
        public string ModuleKey       { get; set; }
        public bool   IncludeInactive { get; set; }
    }
}
