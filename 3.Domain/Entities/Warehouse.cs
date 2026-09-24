using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان Warehouse</summary>
    public class Warehouse : BaseModel
    {
        public string Code        { get; set; }
        public string Name        { get; set; }
        public string Location    { get; set; }
        public string ManagerName { get; set; }
        public bool   IsActive    { get; set; } = true;
    }
}
