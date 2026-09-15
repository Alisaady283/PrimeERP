using System;

namespace PrimeERP.Application.DTOs.HR
{
    /// <summary>بدلٌ أو خصمٌ على موظف — شكلٌ واحد لشاشتين، يفترقان في خدمتهما لا في حقولهما.</summary>
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
        public string   Notes        { get; set; }
        public DateTime CreatedAt    { get; set; }
    }

    public class CreateEmployeeMovementDto
    {
        public int      Id           { get; set; }
        public string   EmployeeCode { get; set; }
        public DateTime Date         { get; set; } = DateTime.Today;
        public int      Month        { get; set; } = DateTime.Today.Month;
        public int      Year         { get; set; } = DateTime.Today.Year;
        public string   Reason       { get; set; }
        public decimal  Amount       { get; set; }
        public string   Notes        { get; set; }
    }

    public class EmployeeMovementFilter
    {
        public string SearchText     { get; set; }
        public int?   EmployeeId     { get; set; }
        public string SortBy         { get; set; } = "Date";
        public bool   SortDescending { get; set; } = true;
    }

    public class AttendanceDto
    {
        public int      Id            { get; set; }
        public int      EmployeeId    { get; set; }
        public string   EmployeeCode  { get; set; }
        public string   EmployeeName  { get; set; }
        public DateTime Date          { get; set; }
        public string   CheckIn       { get; set; }
        public string   CheckOut      { get; set; }
        public decimal  OvertimeHours { get; set; }
        public bool     IsAbsent      { get; set; }
        public string   StatusText    { get; set; }
        public Domain.Results.StatusVariant StatusVariant { get; set; }
        public string   Notes         { get; set; }
        public DateTime CreatedAt     { get; set; }
    }

    public class CreateAttendanceDto
    {
        public int      Id            { get; set; }
        public string   EmployeeCode  { get; set; }
        public DateTime Date          { get; set; } = DateTime.Today;

        /// <summary>نصّاً بصيغة HH:mm — تُحوَّل إلى دقائق في الخدمة، والفارغ غيابٌ بلا توقيت.</summary>
        public string   CheckIn       { get; set; }
        public string   CheckOut      { get; set; }
        public decimal  OvertimeHours { get; set; }
        public bool     IsAbsent      { get; set; }
        public string   Notes         { get; set; }
    }

    public class AttendanceFilter
    {
        public string SearchText     { get; set; }
        public int?   EmployeeId     { get; set; }
        public string SortBy         { get; set; } = "Date";
        public bool   SortDescending { get; set; } = true;
    }
}
