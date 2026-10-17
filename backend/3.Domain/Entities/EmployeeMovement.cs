using System;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>البدل والخصم سواء</summary>
    public abstract class EmployeeMovement : BaseModel, IEmployeeLine
    {
        public int      EmployeeId { get; set; }

        public DateTime Date       { get; set; } = DateTime.Today;

        public int      Month      { get; set; }
        public int      Year       { get; set; }
        public string   Reason     { get; set; }
        public decimal  Amount     { get; set; }
        public int?     TypeId     { get; set; }
        public int?     PayrollId  { get; set; }
        public string   Notes      { get; set; }

        public string   TypeName     { get; set; }
        public string   EmployeeName { get; set; }
        public string   EmployeeCode { get; set; }
    }

    public class EmployeeAllowance : EmployeeMovement { }

    public class EmployeeDeduction : EmployeeMovement { }

    public class EmployeeAdvance : EmployeeMovement
    {
        public int?   TreasuryId     { get; set; }
        public int?   JournalEntryId { get; set; }
        public string TreasuryName   { get; set; }
    }

    public class Attendance : BaseModel, IEmployeeLine
    {
        public int       SheetId         { get; set; }
        public int       EmployeeId      { get; set; }
        public DateTime  Date            { get; set; } = DateTime.Today;
        public AttendanceStatus Status   { get; set; } = AttendanceStatus.Present;
        public int?      LeaveTypeId     { get; set; }
        public TimeSpan? CheckIn         { get; set; }
        public TimeSpan? CheckOut        { get; set; }
        public int       LateMinutes     { get; set; }
        public int       OvertimeMinutes { get; set; }
        public string    Notes           { get; set; }

        public string    EmployeeName  { get; set; }
        public string    EmployeeCode  { get; set; }
    }
}
