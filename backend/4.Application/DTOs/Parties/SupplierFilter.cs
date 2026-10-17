namespace PrimeERP.Application.DTOs.Parties
{
    /// <summary>مرشّح الموردين</summary>
    public class SupplierFilter
    {
        public string SearchText      { get; set; }
        public bool?  IsActive        { get; set; }
        public bool?  HasBalance      { get; set; }
        public bool?  OverCreditLimit { get; set; }
        public int?   CategoryId      { get; set; }
        public string SortBy          { get; set; } = "Code";
        public bool   SortDescending  { get; set; }
    }
}
