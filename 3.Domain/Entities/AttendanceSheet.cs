using System;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class AttendanceSheet : BaseModel
    {
        public DateTime Date  { get; set; } = DateTime.Today;
        public string   Notes { get; set; }
    }
}
