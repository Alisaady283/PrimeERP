using System;
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
        public string   Notes      { get; set; }

        public string   EmployeeName { get; set; }
        public string   EmployeeCode { get; set; }
    }

    public class EmployeeAllowance : EmployeeMovement { }

    public class EmployeeDeduction : EmployeeMovement { }

    /// <summary>موضع الساعات الإضافية</summary>
    public class Attendance : BaseModel, IEmployeeLine
    {
        public int       EmployeeId    { get; set; }
        public DateTime  Date          { get; set; } = DateTime.Today;
        public TimeSpan? CheckIn       { get; set; }
        public TimeSpan? CheckOut      { get; set; }

        public decimal   OvertimeHours { get; set; }

        public bool      IsAbsent      { get; set; }
        public string    Notes         { get; set; }

        public string    EmployeeName  { get; set; }
        public string    EmployeeCode  { get; set; }
    }
}
