using System;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان FiscalPeriod</summary>
    public class FiscalPeriod
    {
        public int       Id           { get; set; }
        public int       FiscalYearId { get; set; }
        public int       PeriodNo     { get; set; }
        public string    Name         { get; set; }
        public string    StartDate    { get; set; }
        public string    EndDate      { get; set; }
        public bool      IsClosed     { get; set; }
        public DateTime? ClosedAt     { get; set; }
        public string    ClosedBy     { get; set; }
        public long      RowVersion   { get; set; }
    }
}
