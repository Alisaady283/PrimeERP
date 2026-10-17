using System;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان Employee</summary>
    public class Employee : BaseModel
    {
        public string    Code            { get; set; }
        public string    Name            { get; set; }
        public string    NameEn          { get; set; }
        public string    NationalId      { get; set; }
        public string    Phone           { get; set; }
        public string    Email           { get; set; }
        public string    Address         { get; set; }
        public int?       DepartmentId    { get; set; }
        public int?       JobTitleId      { get; set; }
        public DateTime   HireDate        { get; set; }
        public DateTime?  TerminationDate { get; set; }
        public decimal    BasicSalary     { get; set; }

        public decimal    FixedAllowances { get; set; }

        public bool       IsInsured       { get; set; }
        public DateTime?  InsuranceStartDate { get; set; }

        public decimal    InsuranceAmount { get; set; }

        public string     TaxNumber       { get; set; }

        public decimal    TaxAmount       { get; set; }

        public TimeSpan?  WorkStart       { get; set; }
        public TimeSpan?  WorkEnd         { get; set; }
        public int?       AccountId       { get; set; }

        public string     AccountCode     { get; set; }
        public string     BankAccount     { get; set; }
        public EmployeeStatus Status      { get; set; } = EmployeeStatus.Active;
        public string      Notes           { get; set; }

        public string DepartmentName { get; set; }
        public string JobTitleName   { get; set; }
    }
}
