using System;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Domain.Calculations;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Ledger
{
    public static class PayrollSettings
    {
        public static PayrollRules Rules(ISettingsProvider settings) => new(
            settings.Get(SettingKeys.HR.DailyWageDays, 30),
            settings.Get(SettingKeys.HR.OvertimeRate, 1.5m), Clock(settings, SettingKeys.HR.OvertimeMinimum),
            settings.Get(SettingKeys.HR.LateRate, 1m), Clock(settings, SettingKeys.HR.LateMinimum),
            Clock(settings, SettingKeys.HR.WorkStart), Clock(settings, SettingKeys.HR.WorkEnd),
            settings.Get(SettingKeys.HR.PayrollStartDay, 1));

        private static TimeSpan Clock(ISettingsProvider settings, string key) =>
            (TimeSpan)Rows.To(settings.Get<string>(key), typeof(TimeSpan));
    }
}
