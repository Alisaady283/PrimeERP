namespace PrimeERP.Domain.Entities
{
    public class Category
    {
        public int    Id        { get; set; }
        public string Name      { get; set; }
        public int?   ParentId  { get; set; }
        public string ModuleKey { get; set; }
        public bool   IsActive  { get; set; } = true;
        public string Notes     { get; set; }
    }
}
