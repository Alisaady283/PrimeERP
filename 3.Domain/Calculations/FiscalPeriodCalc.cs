using PrimeERP.Domain.Entities;
using System.Linq;
using System;
using System.Collections.Generic;

namespace PrimeERP.Domain.Calculations
{
    /// <summary>حسابات تواريخ السنة/الفترات المالية</summary>
    public static class FiscalPeriodCalc
    {
        public static DateTime EndOfYear(DateTime start) => start.AddYears(1).AddDays(-1);

        /// <summary>فترات السنة بتواريخها</summary>
        public static List<FiscalPeriod> SplitPeriods(DateTime start, DateTime end, int periodsCount)
        {
            int monthsPerPeriod = 12 / periodsCount;
            var result = new List<FiscalPeriod>();

            for (int i = 0; i < periodsCount; i++)
            {
                var periodStart = start.AddMonths(i * monthsPerPeriod);
                var periodEnd = i == periodsCount - 1 ? end : start.AddMonths((i + 1) * monthsPerPeriod).AddDays(-1);
                result.Add(new FiscalPeriod
                {
                    PeriodNo = i + 1, StartDate = periodStart.ToString("yyyy-MM-dd"), EndDate = periodEnd.ToString("yyyy-MM-dd")
                });
            }

            return result;
        }

        public static string DefaultYearName(DateTime start, DateTime end) =>
            start.Year == end.Year ? start.Year.ToString() : $"{start.Year}/{end.Year}";

        public static DateTime YearStartContaining(DateTime referenceDate, int startMonth)
        {
            var yearNum = referenceDate.Month >= startMonth ? referenceDate.Year : referenceDate.Year - 1;
            return new DateTime(yearNum, startMonth, 1);
        }
    }
}
