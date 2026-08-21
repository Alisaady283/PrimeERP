using System;
using System.Collections.Generic;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class Payroll : BaseModel
    {
        public string   PayrollNo       { get; set; }
        public DateTime PeriodStart     { get; set; }
        public DateTime PeriodEnd       { get; set; }
        public DateTime PaymentDate     { get; set; }
        public decimal  TotalBasic      { get; set; }
        public decimal  TotalAllowances { get; set; }
        public decimal  TotalDeductions { get; set; }
        public decimal  NetTotal        { get; set; }
        public bool     IsPosted        { get; set; }
        public int?     JournalEntryId  { get; set; }
        public string   Notes           { get; set; }

        public List<PayrollLine> Lines { get; set; } = new();
    }
}
