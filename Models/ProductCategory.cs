using PrimeERP.Models.Common;

namespace PrimeERP.Models
{
    public class ProductCategory : BaseModel
    {
        public string Name     { get; set; }
        public string NameEn   { get; set; }
        public int?   ParentId { get; set; }
        public bool   IsActive { get; set; } = true;
    }
}
