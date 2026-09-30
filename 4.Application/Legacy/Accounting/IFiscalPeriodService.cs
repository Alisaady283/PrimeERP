using System;
using System.Collections.Generic;
using PrimeERP.Domain.Results;
using PrimeERP.Application.DTOs.Accounting;

namespace PrimeERP.Application.Legacy.Accounting
{
    /// <summary>عقد الفترات المالية</summary>
    public interface IFiscalPeriodService
    {
        Result<FiscalYearDto>   GetCurrentYear();
        Result<FiscalPeriodDto> GetCurrentPeriod();
        Result<FiscalPeriodDto> GetPeriodFor(DateTime date);

        bool IsOpen(DateTime date);

        Result<List<FiscalYearDto>>   GetAllYears();
        Result<List<FiscalPeriodDto>> GetPeriods(int yearId);

        Result<FiscalYearDto> CreateYear(DateTime start, int periodsCount = 12, string name = null);
        Result SetCurrent(int yearId);

        Result ClosePeriod(int periodId);
        Result ReopenPeriod(int periodId);

        Result CloseYear(int yearId);

        Result ReopenYear(int yearId);

        Result<int> CountUnpostedInPeriod(int periodId);
        Result<List<FiscalPeriodDto>> GetOpenPeriods();
    }
}
