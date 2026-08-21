using System;
using PrimeERP.Models.Common;

namespace PrimeERP.Models
{
    public class ExchangeRate : BaseModel
    {
        public int      CurrencyId { get; set; }
        public DateTime Date       { get; set; }
        public decimal  Rate       { get; set; }
    }
}
