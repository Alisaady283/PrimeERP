using System.Collections;
using System.Collections.Generic;

namespace PrimeERP.Application.Reporting
{
    /// <summary>مخرَج التقرير</summary>
    public class ReportData
    {
        public required IEnumerable Rows { get; init; }
        public Dictionary<string, string> Totals { get; init; } = new();
    }
}
