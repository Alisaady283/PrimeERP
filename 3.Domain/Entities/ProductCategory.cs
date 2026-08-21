using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class ProductCategory : BaseModel
    {
        public string Name     { get; set; }
        public string NameEn   { get; set; }
        public int?   ParentId { get; set; }
        public bool   IsActive { get; set; } = true;
    }
}
