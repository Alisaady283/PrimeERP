using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class DeductionType : BaseModel
    {
        public string Name     { get; set; }
        public bool   IsActive { get; set; } = true;
    }
}
