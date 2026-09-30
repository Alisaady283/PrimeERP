using System;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.DTOs.Parties
{
    /// <summary>بيانات المورد</summary>
    public class SupplierDto : PartyDto
    {
        public SupplierType SupplierType { get; set; }
    }

    public class CreateSupplierDto : IPartyInput
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
        public SupplierType SupplierType { get; set; } = SupplierType.Local;
        public string  Notes           { get; set; }
        public int?    CategoryId      { get; set; }
        public bool    IsActive        { get; set; } = true;
    }

    public class UpdateSupplierDto : IPartyInput
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
        public SupplierType SupplierType { get; set; }
        public string  Notes           { get; set; }
        public bool    IsActive        { get; set; }
        public int?    CategoryId      { get; set; }
    }

    public class SupplierFilter
    {
        public string SearchText      { get; set; }
        public bool?  IsActive        { get; set; }
        public bool?  HasBalance      { get; set; }
        public bool?  OverCreditLimit { get; set; }
        public int?   CategoryId      { get; set; }
        public string SortBy          { get; set; } = "Code";
        public bool   SortDescending  { get; set; }
    }
}
