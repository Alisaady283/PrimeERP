using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class Branch : BaseModel
    {
        public string Code        { get; set; }
        public string Name        { get; set; }
        public int?   WarehouseId { get; set; }
        public int?   TreasuryId  { get; set; }
        public bool   IsActive    { get; set; } = true;
    }
}
