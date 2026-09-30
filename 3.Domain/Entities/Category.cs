namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان Category</summary>
    public class Category
    {
        public int    Id        { get; set; }
        public string Name      { get; set; }
        public int?   ParentId  { get; set; }
        public string ParentName { get; set; }
        public string ModuleKey { get; set; }
        public bool   IsActive  { get; set; } = true;
        public string Notes     { get; set; }

        public string AccountCode { get; set; }

        public string DepreciationAccountCode { get; set; }
    }
}
