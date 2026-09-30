using System;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.DTOs.Parties
{
    /// <summary>عرض الطرف في الجداول</summary>
    public abstract class PartyDto
    {
        public int    Id              { get; set; }
        public string Code            { get; set; }
        public string Name            { get; set; }
        public int?     CategoryId    { get; set; }
        public string   CategoryName  { get; set; }
        public DateTime CreatedAt     { get; set; }
        public DateTime UpdatedAt     { get; set; }
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

        public decimal CreditLimit       { get; set; }
        public bool    IsOverCreditLimit { get; set; }
        public decimal AvailableCredit   { get; set; }

        public int    PaymentTermDays { get; set; }
        public bool   IsActive        { get; set; }
        public string Notes           { get; set; }

        public StatusVariant StatusVariant { get; set; }
        public string        StatusText    { get; set; }

        public bool CanEdit          { get; set; }
        public bool CanDelete        { get; set; }
        public bool HasTransactions  { get; set; }
    }

    /// <summary>حقول إدخال الطرف</summary>
    public interface IPartyInput
    {
        string  Name            { get; }
        string  NameEn          { get; }
        string  Phone           { get; }
        string  Phone2          { get; }
        string  Email           { get; }
        string  Address         { get; }
        string  City            { get; }
        string  Country         { get; }
        string  TaxNumber       { get; }
        string  CommercialRegNo { get; }
        decimal CreditLimit     { get; }
        int     PaymentTermDays { get; }
        string  Notes           { get; }
        int?    CategoryId      { get; }
        bool    IsActive        { get; }
    }
}
