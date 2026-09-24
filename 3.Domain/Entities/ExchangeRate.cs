using System;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان ExchangeRate</summary>
    public class ExchangeRate : BaseModel
    {
        public int      CurrencyId { get; set; }
        public DateTime Date       { get; set; }
        public decimal  Rate       { get; set; }
    }
}
