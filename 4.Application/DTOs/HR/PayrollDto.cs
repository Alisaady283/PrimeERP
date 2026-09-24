using System;
using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.HR
{
    /// <summary>بيانات مسير الرواتب</summary>
    public class PayrollDto
    {
        public int      Id              { get; set; }
        public string   PayrollNo       { get; set; }
        public DateTime PeriodStart     { get; set; }
        public DateTime PeriodEnd       { get; set; }
        public DateTime PaymentDate     { get; set; }
        public decimal  TotalBasic      { get; set; }
        public decimal  TotalAllowances { get; set; }
        public decimal  TotalDeductions { get; set; }
        public decimal  NetTotal        { get; set; }

        public bool     IsPosted        { get; set; }
        public string   StatusText      { get; set; }
        public string   Notes           { get; set; }
        public Domain.Results.StatusVariant StatusVariant { get; set; }

        public DateTime CreatedAt       { get; set; }
    }

    public class PayrollDetailDto : PayrollDto
    {
        public List<PayrollLineDto> Lines { get; set; } = new();
    }

    public class PayrollLineDto
    {
        public int     EmployeeId   { get; set; }
        public string  EmployeeName { get; set; }
        public decimal BasicSalary  { get; set; }
        public decimal Allowances   { get; set; }
        public decimal Overtime     { get; set; }
        public decimal Deductions   { get; set; }
        public decimal Advances     { get; set; }
        public decimal Insurance    { get; set; }
        public decimal Tax          { get; set; }
        public decimal NetSalary    { get; set; }
        public string  Notes        { get; set; }
    }

    public class CreatePayrollLineDto
    {
        public int     LineNo       { get; set; }
        public string  EmployeeCode { get; set; }

        public decimal BasicSalary  { get; set; }
        public decimal Allowances   { get; set; }
        public decimal Overtime     { get; set; }

        public decimal Deductions   { get; set; }
        public decimal Advances     { get; set; }
        public decimal Insurance    { get; set; }
        public decimal Tax          { get; set; }

        public decimal NetSalary    { get; set; }

        public string  Notes        { get; set; }
    }

    public class CreatePayrollDto
    {
        public int      Id          { get; set; }
        public DateTime PeriodStart { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime PeriodEnd   { get; set; } = DateTime.Today;
        public DateTime PaymentDate { get; set; } = DateTime.Today;
        public string   Notes       { get; set; }
        public List<CreatePayrollLineDto> Lines { get; set; } = new();
    }

    public class PayrollFilter
    {
        public string SearchText     { get; set; }
        public string SortBy         { get; set; } = "PaymentDate";
        public bool   SortDescending { get; set; } = true;
    }
}
