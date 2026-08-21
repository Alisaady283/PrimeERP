using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class TaxGroup : BaseModel
    {
        public string  Name         { get; set; }
        public decimal Rate         { get; set; }
        public bool    IsInclusive  { get; set; }
        public int?    TaxAccountId { get; set; }
    }
}
