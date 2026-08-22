using PrimeERP.Data.Repositories;
using System;
using PrimeERP.Domain.Rules;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Settings;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Data.Seeders
{
    /// <summary>يزرع السنة المالية الحالية عند أول تشغيل — لا يفعل شيئاً لو توجد سنة مالية بالفعل.</summary>
    public static class FiscalYearSeeder
    {
        public static void Seed(IFiscalPeriodRepository fiscalPeriods)
        {
            if (fiscalPeriods.GetAllYears().Count > 0) return;

            var startMonth = 1;
            var setting = SettingRepository.GetByKey(SettingKeys.Financial.FiscalYearStartMonth);
            if (setting != null && int.TryParse(setting.Value, out var configured) && configured is >= 1 and <= 12)
                startMonth = configured;

            var start = FiscalPeriodCalculator.YearStartContaining(DateTime.Today, startMonth);
            var end = FiscalPeriodCalculator.EndOfYear(start);
            var name = FiscalPeriodCalculator.DefaultYearName(start, end);

            Db.RunTransaction((conn, tx) =>
            {
                var yearId = fiscalPeriods.InsertYear(conn, tx, new FiscalYear
                {
                    Name      = name,
                    StartDate = start.ToString("yyyy-MM-dd"),
                    EndDate   = end.ToString("yyyy-MM-dd")
                });

                foreach (var p in FiscalPeriodCalculator.SplitPeriods(start, end, 12))
                {
                    fiscalPeriods.InsertPeriod(conn, tx, new FiscalPeriod
                    {
                        FiscalYearId = yearId,
                        PeriodNo     = p.PeriodNo,
                        Name         = $"الفترة {p.PeriodNo}",
                        StartDate    = p.Start.ToString("yyyy-MM-dd"),
                        EndDate      = p.End.ToString("yyyy-MM-dd")
                    });
                }

                fiscalPeriods.SetCurrentYear(conn, tx, yearId);
            });
        }
    }
}
