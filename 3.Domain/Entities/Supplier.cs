using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class Supplier : BaseModel
    {
        public string  Code            { get; set; }
        public string  Name            { get; set; }
        public string  NameEn          { get; set; }
        public string  Phone           { get; set; }
        public string  Phone2          { get; set; }
        public string  Email           { get; set; }
        public string  Address         { get; set; }
        public string  City            { get; set; }
        public string  Country         { get; set; }
        public string  TaxNumber       { get; set; }
        public string  CommercialRegNo { get; set; }
        public int?    AccountId       { get; set; }
        public string  AccountCode     { get; set; }
        public decimal CreditLimit     { get; set; }
        public int     PaymentTermDays { get; set; }
        public decimal Balance         { get; set; }
        public int?    CurrencyId      { get; set; }
        public int?    CategoryId      { get; set; }
        public SupplierType SupplierType { get; set; } = SupplierType.Local;
        public string  Notes           { get; set; }
        public bool    IsActive        { get; set; } = true;
    }
}
