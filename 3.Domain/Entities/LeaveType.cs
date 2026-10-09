using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class LeaveType : BaseModel
    {
        public string Name     { get; set; }
        public int    DaysPerYear { get; set; }
        public bool   IsPaid   { get; set; } = true;
        public bool   IsActive { get; set; } = true;
    }
}
