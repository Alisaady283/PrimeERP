using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان Supplier</summary>
    public class Supplier : PartyBase
    {
        public int?         AccountId    { get; set; }
        public SupplierType SupplierType { get; set; } = SupplierType.Local;
    }
}
