using System;
using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.HR
{
    /// <summary>بدلٌ أو خصمٌ على موظف</summary>
    public class EmployeeMovementDto
    {
        public int      Id           { get; set; }
        public int      EmployeeId   { get; set; }
        public string   EmployeeCode { get; set; }
        public string   EmployeeName { get; set; }
        public DateTime Date         { get; set; }
        public int      Month        { get; set; }
        public int      Year         { get; set; }
        public string   Reason       { get; set; }
        public decimal  Amount       { get; set; }
        public int?     TypeId       { get; set; }
        public string   TypeName     { get; set; }
        public int?     TreasuryId   { get; set; }
        public string   TreasuryName { get; set; }
        public string   Notes        { get; set; }
        public DateTime CreatedAt    { get; set; }
    }

    public class CreateEmployeeMovementDto
    {
        public int      Id           { get; set; }
        public string   EmployeeCode { get; set; }
        public DateTime Date         { get; set; } = DateTime.Today;
        public string   Reason       { get; set; }
        public decimal  Amount       { get; set; }
        public int?     TypeId       { get; set; }
        public int?     TreasuryId   { get; set; }
        public string   Notes        { get; set; }
    }

    public class EmployeeMovementFilter
    {
        public string SearchText     { get; set; }
        public int?   EmployeeId     { get; set; }
        public string SortBy         { get; set; } = "Date";
        public bool   SortDescending { get; set; } = true;
    }

    public class AttendanceDayDto
    {
        public int      Id      { get; set; }
        public DateTime Date    { get; set; }
        public int      Present { get; set; }
        public int      Absent  { get; set; }
        public int      Leave   { get; set; }
        public int      Mission { get; set; }
        public int      Holiday { get; set; }
        public string   Notes   { get; set; }
    }

    public class AttendanceFilter
    {
        public string SearchText { get; set; }
    }

    public class AttendanceSheetDto
    {
        public int      Id    { get; set; }
        public DateTime Date  { get; set; } = DateTime.Today;
        public string   Notes { get; set; }
        public List<AttendanceLineDto> Lines { get; set; } = new();
    }

    public class AttendanceLineDto
    {
        public int    LineNo         { get; set; }
        public string EmployeeCode   { get; set; }
        public string EmployeeName   { get; set; }
        public string DepartmentName { get; set; }
        public int    Status         { get; set; }
        public int?   LeaveTypeId    { get; set; }
        public string CheckIn        { get; set; }
        public string CheckOut       { get; set; }
        public string Late           { get; set; }
        public string Overtime       { get; set; }
        public string Notes          { get; set; }
    }
}
