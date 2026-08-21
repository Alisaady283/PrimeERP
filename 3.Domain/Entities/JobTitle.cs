using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class JobTitle : BaseModel
    {
        public string Name     { get; set; }
        public string NameEn   { get; set; }
        public bool   IsActive { get; set; } = true;
    }
}
