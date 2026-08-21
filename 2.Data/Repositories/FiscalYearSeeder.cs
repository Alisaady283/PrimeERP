using System;
using PrimeERP.Domain.Rules;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Settings;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Data.Repositories
{
    /// <summary>
    /// يزرع السنة المالية الحالية تلقائياً عند أول تشغيل (12 فترة شهرية) — Repository مباشرة لا عبر
    /// FiscalPeriodService، تماماً كـ AccountRepository.SeedDefaults: يعمل عند إقلاع التطبيق قبل أي تسجيل
    /// دخول، فلا معنى لتحقق صلاحية هنا. حساب حدود السنة وتقسيم الفترات عبر PrimeERP.Domain.Rules.FiscalPeriodCalculator
    /// (دوال نقية) — نفس الحساب الذي يستخدمه FiscalPeriodService.CreateYear، لا نسخة مكرَّرة منه هنا.
    /// يقرأ SettingKeys.Financial.FiscalYearStartMonth عبر SettingRepository مباشرة (لا SettingsService) للبقاء
    /// ضمن طبقة الوصول للبيانات بلا اعتماد على طبقة الخدمات. لا يفعل شيئاً لو توجد أي سنة مالية بالفعل.
    /// </summary>
    public static class FiscalYearSeeder
    {
        public static void Seed()
        {
            if (FiscalPeriodRepository.GetAllYears().Count > 0) return;

            var startMonth = 1;
            var setting = SettingRepository.GetByKey(SettingKeys.Financial.FiscalYearStartMonth);
            if (setting != null && int.TryParse(setting.Value, out var configured) && configured is >= 1 and <= 12)
                startMonth = configured;

            var start = FiscalPeriodCalculator.YearStartContaining(DateTime.Today, startMonth);
            var end = FiscalPeriodCalculator.EndOfYear(start);
            var name = FiscalPeriodCalculator.DefaultYearName(start, end);

            Db.RunTransaction((conn, tx) =>
            {
                var yearId = FiscalPeriodRepository.InsertYear(conn, tx, new FiscalYear
                {
                    Name      = name,
                    StartDate = start.ToString("yyyy-MM-dd"),
                    EndDate   = end.ToString("yyyy-MM-dd")
                });

                foreach (var p in FiscalPeriodCalculator.SplitPeriods(start, end, 12))
                {
                    FiscalPeriodRepository.InsertPeriod(conn, tx, new FiscalPeriod
                    {
                        FiscalYearId = yearId,
                        PeriodNo     = p.PeriodNo,
                        Name         = $"الفترة {p.PeriodNo}",
                        StartDate    = p.Start.ToString("yyyy-MM-dd"),
                        EndDate      = p.End.ToString("yyyy-MM-dd")
                    });
                }

                FiscalPeriodRepository.SetCurrentYear(conn, tx, yearId);
            });
        }
    }
}
