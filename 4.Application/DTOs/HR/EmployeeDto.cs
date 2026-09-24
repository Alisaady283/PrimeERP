using System;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.DTOs.HR
{
    /// <summary>بيانات الموظف</summary>
    public class EmployeeDto
    {
        public int      Id             { get; set; }
        public string   Code           { get; set; }
        public string   Name           { get; set; }
        public int?     DepartmentId   { get; set; }
        public string   DepartmentName { get; set; }
        public int?     JobTitleId     { get; set; }
        public string   JobTitleName   { get; set; }
        public string   Phone          { get; set; }
        public string   Email          { get; set; }
        public DateTime HireDate       { get; set; }
        public decimal  BasicSalary    { get; set; }
        public string   Notes          { get; set; }
        public bool     IsActive       { get; set; }
        public string   AccountCode    { get; set; }

        public string   StatusText     { get; set; }
        public DateTime CreatedAt      { get; set; }
        public DateTime UpdatedAt      { get; set; }
        public StatusVariant StatusVariant { get; set; }

        public bool CanEdit   { get; set; }
        public bool CanDelete { get; set; }
    }

    public class CreateEmployeeDto
    {
        public string   Name         { get; set; }
        public int?     DepartmentId { get; set; }
        public int?     JobTitleId   { get; set; }
        public string   Phone        { get; set; }
        public string   Email        { get; set; }
        public DateTime HireDate     { get; set; } = DateTime.Today;
        public decimal  BasicSalary  { get; set; }
        public string   NationalId   { get; set; }
        public string   Address      { get; set; }
        public decimal  FixedAllowances { get; set; }
        public bool     IsInsured    { get; set; }
        public DateTime? InsuranceStartDate { get; set; }
        public decimal  InsuranceAmount { get; set; }
        public string   TaxNumber    { get; set; }
        public decimal  TaxAmount    { get; set; }
        public string   Notes        { get; set; }
        public bool     IsActive     { get; set; } = true;
    }

    public class UpdateEmployeeDto
    {
        public int      Id           { get; set; }
        public string   Name         { get; set; }
        public int?     DepartmentId { get; set; }
        public int?     JobTitleId   { get; set; }
        public string   Phone        { get; set; }
        public string   Email        { get; set; }
        public DateTime HireDate     { get; set; }
        public decimal  BasicSalary  { get; set; }
        public string   Notes        { get; set; }
        public bool     IsActive     { get; set; }
    }

    public class EmployeeFilter
    {
        public string SearchText     { get; set; }
        public int?   DepartmentId   { get; set; }
        public string SortBy         { get; set; } = "Name";
        public bool   SortDescending { get; set; }
    }
}
