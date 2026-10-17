using System.Collections.Generic;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان PurchaseReturn</summary>
    public class PurchaseReturn : ReturnBase
    {
        public int SupplierId { get; set; }

        public List<PurchaseReturnLine> Lines { get; set; } = new();
    }

    public class PurchaseReturnLine : ReturnLineBase
    {
    }
}
