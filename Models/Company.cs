using PrimeERP.Models.Common;

namespace PrimeERP.Models
{
    public class Company : BaseModel
    {
        public string Name            { get; set; }
        public string NameEn          { get; set; }
        public string TaxNumber       { get; set; }
        public string CommercialRegNo { get; set; }
        public string Address         { get; set; }
        public string Phone           { get; set; }
        public string Email           { get; set; }
        public string Logo            { get; set; }
        public int?   BaseCurrencyId  { get; set; }
        public string FiscalYearStart { get; set; }
    }
}
