using PrimeERP.Domain.Results;

namespace PrimeERP.Application.DTOs.Parties
{
    /// <summary>للعرض في الجداول — لا Model خام (Customer) يخرج من الخدمة إطلاقاً.</summary>
    public class CustomerDto
    {
        public int    Id              { get; set; }
        public string Code            { get; set; }
        public string Name            { get; set; }
        public string NameEn          { get; set; }
        public string Phone           { get; set; }
        public string Phone2          { get; set; }
        public string Email           { get; set; }
        public string Address         { get; set; }
        public string City            { get; set; }
        public string TaxNumber       { get; set; }
        public string CommercialRegNo { get; set; }

        public string  AccountCode { get; set; }
        public string  AccountName { get; set; }
        public decimal Balance     { get; set; }

        public decimal CreditLimit { get; set; }

        /// <summary>قرار أعمال — تحسبه الخدمة (نُقلت من Model في فحص تسريب المنطق قبل F.2.3): CreditLimit>0 && Balance>CreditLimit.</summary>
        public bool IsOverCreditLimit { get; set; }

        /// <summary>CreditLimit - Balance — قد تكون سالبة لو متجاوز الحد.</summary>
        public decimal AvailableCredit { get; set; }

        public int    PaymentTermDays { get; set; }
        public bool   IsActive        { get; set; }
        public string Notes           { get; set; }

        public StatusVariant StatusVariant { get; set; }
        public string        StatusText    { get; set; }

        public bool CanEdit          { get; set; }
        public bool CanDelete        { get; set; }
        public bool HasTransactions  { get; set; }
    }

    public class CreateCustomerDto
    {
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
        public decimal CreditLimit     { get; set; }
        public int     PaymentTermDays { get; set; }
        public string  Notes           { get; set; }
    }

    public class UpdateCustomerDto
    {
        public int     Id              { get; set; }
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
        public decimal CreditLimit     { get; set; }
        public int     PaymentTermDays { get; set; }
        public string  Notes           { get; set; }
        public bool    IsActive        { get; set; }
    }

    public class CustomerFilter
    {
        public string SearchText     { get; set; }
        public bool?  IsActive       { get; set; }
        public bool?  HasBalance     { get; set; }
        public bool?  OverCreditLimit{ get; set; }
        public int?   CategoryId     { get; set; }
        public string SortBy         { get; set; } = "Name";
        public bool   SortDescending { get; set; }
    }

    public class CreditCheckResult
    {
        public bool    IsAllowed       { get; set; }
        public decimal CurrentBalance  { get; set; }
        public decimal CreditLimit     { get; set; }
        public decimal AvailableCredit { get; set; }
        public decimal ExceededBy      { get; set; }
    }
}
