using System;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class HeldSale : BaseModel
    {
        public int?     BranchId  { get; set; }
        public string   Label     { get; set; }
        public string   Payload   { get; set; }
        public DateTime HeldAt    { get; set; }
    }
}
