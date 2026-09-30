using System;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.DTOs.Parties
{
    /// <summary>للعرض في الجداول</summary>
    public class CustomerDto : PartyDto
    {
    }

    public class CreateCustomerDto : IPartyInput
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
        public int?    CategoryId      { get; set; }
        public bool    IsActive        { get; set; } = true;
    }

    public class UpdateCustomerDto : IPartyInput
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
        public int?    CategoryId      { get; set; }
    }

    public class CustomerFilter
    {
        public string SearchText     { get; set; }
        public bool?  IsActive       { get; set; }
        public bool?  HasBalance     { get; set; }
        public bool?  OverCreditLimit{ get; set; }
        public int?   CategoryId     { get; set; }
        public string SortBy         { get; set; } = "Code";
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
