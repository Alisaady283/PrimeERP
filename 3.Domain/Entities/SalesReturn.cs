using System.Collections.Generic;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان SalesReturn</summary>
    public class SalesReturn : ReturnBase
    {
        public int CustomerId { get; set; }

        public List<SalesReturnLine> Lines { get; set; } = new();
    }

    public class SalesReturnLine : ReturnLineBase
    {
    }
}
