using PrimeERP.Models.Common;

namespace PrimeERP.Models
{
    public class JobTitle : BaseModel
    {
        public string Name     { get; set; }
        public string NameEn   { get; set; }
        public bool   IsActive { get; set; } = true;
    }
}
