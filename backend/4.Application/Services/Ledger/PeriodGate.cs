using System;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>التاريخ في فترةٍ مفتوحة</summary>
    public sealed class PeriodGate
    {
        private readonly IFiscalPeriodRepository _periods;
        private readonly ISettingsProvider _settings;

        public PeriodGate(IFiscalPeriodRepository periods, ISettingsProvider settings)
        {
            _periods = periods;
            _settings = settings;
        }

        public bool IsOpen(DateTime date)
        {
            var period = _periods.GetPeriodContaining(date.ToString("yyyy-MM-dd"));
            if (period == null) return !_settings.Get(SettingKeys.Financial.RequireFiscalPeriod, false);
            if (period.IsClosed) return false;

            var year = _periods.GetYearById(period.FiscalYearId);
            return year == null || !year.IsClosed;
        }
    }
}
