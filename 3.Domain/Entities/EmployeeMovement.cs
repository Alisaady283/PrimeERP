using System;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>البدل والخصم سواء — يفترقان في جدولهما لا في شكلهما.</summary>
    public abstract class EmployeeMovement : BaseModel
    {
        public int      EmployeeId { get; set; }

        public DateTime Date       { get; set; } = DateTime.Today;

        /// <summary>شهر الاستحقاق — به يلتقط المسير الحركة لا بتاريخ تسجيلها.</summary>
        public int      Month      { get; set; }
        public int      Year       { get; set; }
        public string   Reason     { get; set; }
        public decimal  Amount     { get; set; }
        public string   Notes      { get; set; }

        // يُملآن بربطٍ في الاستعلام — ليسا عمودين.
        public string   EmployeeName { get; set; }
        public string   EmployeeCode { get; set; }
    }

    public class EmployeeAllowance : EmployeeMovement { }

    public class EmployeeDeduction : EmployeeMovement { }

    /// <summary>الساعات الإضافية تُسجَّل هنا لا في شاشةٍ مستقلّة.</summary>
    public class Attendance : BaseModel
    {
        public int       EmployeeId    { get; set; }
        public DateTime  Date          { get; set; } = DateTime.Today;
        public TimeSpan? CheckIn       { get; set; }
        public TimeSpan? CheckOut      { get; set; }

        public decimal   OvertimeHours { get; set; }

        /// <summary>يُحتسب خصماً بأجر يوم.</summary>
        public bool      IsAbsent      { get; set; }
        public string    Notes         { get; set; }

        public string    EmployeeName  { get; set; }
        public string    EmployeeCode  { get; set; }
    }
}
