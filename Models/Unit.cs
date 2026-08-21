using PrimeERP.Models.Common;

namespace PrimeERP.Models
{
    public class Unit : BaseModel
    {
        public string Name     { get; set; }
        public string NameEn   { get; set; }
        public string Symbol   { get; set; }
        public bool   IsActive { get; set; } = true;
    }
}
