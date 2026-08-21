using PrimeERP.Models.Common;

namespace PrimeERP.Models
{
    public class Department : BaseModel
    {
        public string Name      { get; set; }
        public string NameEn    { get; set; }
        public int?   ManagerId { get; set; }
        public bool   IsActive  { get; set; } = true;
    }
}
