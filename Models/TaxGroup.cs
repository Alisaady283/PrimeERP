using PrimeERP.Models.Common;

namespace PrimeERP.Models
{
    public class TaxGroup : BaseModel
    {
        public string  Name         { get; set; }
        public decimal Rate         { get; set; }
        public bool    IsInclusive  { get; set; }
        public int?    TaxAccountId { get; set; }
    }
}
