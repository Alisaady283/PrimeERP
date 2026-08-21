using System;
using System.Collections.Generic;
using PrimeERP.Core.Common;

namespace PrimeERP.Services.Accounting.DTOs
{
    public class FiscalYearDto
    {
        public int       Id             { get; set; }
        public string    Name           { get; set; }
        public DateTime  StartDate      { get; set; }
        public DateTime  EndDate        { get; set; }
        public bool      IsClosed       { get; set; }
        public DateTime? ClosedAt       { get; set; }
        public string    ClosedBy       { get; set; }
        public int?      ClosingEntryId { get; set; }
        public bool      IsCurrent      { get; set; }
        public StatusVariant StatusVariant { get; set; }
        public List<FiscalPeriodDto> Periods { get; set; } = new();
    }

    public class FiscalPeriodDto
    {
        public int       Id           { get; set; }
        public int       FiscalYearId { get; set; }
        public int       PeriodNo     { get; set; }
        public string    Name         { get; set; }
        public DateTime  StartDate    { get; set; }
        public DateTime  EndDate      { get; set; }
        public bool      IsClosed     { get; set; }
        public DateTime? ClosedAt     { get; set; }
        public string    ClosedBy     { get; set; }
        public StatusVariant StatusVariant { get; set; }
    }
}
