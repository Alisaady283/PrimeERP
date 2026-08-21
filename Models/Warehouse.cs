using PrimeERP.Models.Common;

namespace PrimeERP.Models
{
    public class Warehouse : BaseModel
    {
        public string Code        { get; set; }
        public string Name        { get; set; }
        public string Location    { get; set; }
        public string ManagerName { get; set; }
        public bool   IsActive    { get; set; } = true;
    }
}
