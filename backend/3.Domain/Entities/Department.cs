using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان Department</summary>
    public class Department : BaseModel
    {
        public string Name      { get; set; }
        public string NameEn    { get; set; }
        public int?   ManagerId { get; set; }
        public bool   IsActive  { get; set; } = true;
    }
}
