using System;
using System.Collections.Generic;
using PrimeERP.Domain.Results;
using PrimeERP.Application.DTOs.Accounting;

namespace PrimeERP.Application.Services.Accounting
{
    public interface IFiscalPeriodService
    {
        Result<FiscalYearDto>   GetCurrentYear();
        Result<FiscalPeriodDto> GetCurrentPeriod();
        Result<FiscalPeriodDto> GetPeriodFor(DateTime date);

        /// <summary>بلا Result عمداً — تُستدعى بكثرة من JournalService عند كل قيد. غياب فترة معرَّفة لتاريخ ما = مفتوح افتراضياً (يسمح بتشغيل النظام قبل إعداد السنوات المالية)، إلا لو SettingKeys.Financial.RequireFiscalPeriod=true.</summary>
        bool IsOpen(DateTime date);

        Result<List<FiscalYearDto>>   GetAllYears();
        Result<List<FiscalPeriodDto>> GetPeriods(int yearId);

        Result<FiscalYearDto> CreateYear(DateTime start, int periodsCount = 12, string name = null);
        Result SetCurrent(int yearId);

        Result ClosePeriod(int periodId);
        Result ReopenPeriod(int periodId);

        /// <summary>الأثقل في النظام — تبني وترحّل قيد الإقفال تلقائياً عبر IJournalService، معاملة واحدة.</summary>
        Result CloseYear(int yearId);

        /// <summary>خطير — يحذف قيد الإقفال ويعيد فتح سنة كاملة. تأكيد على مستوى الواجهة إلزامي قبل الاستدعاء.</summary>
        Result ReopenYear(int yearId);

        Result<int> CountUnpostedInPeriod(int periodId);
        Result<List<FiscalPeriodDto>> GetOpenPeriods();
    }
}
