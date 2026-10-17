using System;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>السنة المالية بفتراتها</summary>
    public sealed class NewFiscalYear
    {
        private readonly IFiscalPeriodRepository _fiscalPeriods;

        public NewFiscalYear(IFiscalPeriodRepository fiscalPeriods) => _fiscalPeriods = fiscalPeriods;

        /// <summary>الأولى تصير الحاليّة</summary>
        public int Create(PrimeDbContext db, string name, DateTime start, DateTime end, int periodsCount, Func<int, string> periodName)
        {
            var first = !_fiscalPeriods.AnyYear(db);
            var id = _fiscalPeriods.InsertYear(db, new FiscalYear { Name = name, StartDate = start.ToString("yyyy-MM-dd"), EndDate = end.ToString("yyyy-MM-dd") });

            foreach (var period in FiscalPeriodCalc.SplitPeriods(start, end, periodsCount))
            {
                (period.FiscalYearId, period.Name) = (id, periodName(period.PeriodNo));
                _fiscalPeriods.InsertPeriod(db, period);
            }

            if (first) _fiscalPeriods.SetCurrentYear(db, id);
            return id;
        }
    }
}
