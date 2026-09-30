using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Validation;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Platform.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Domain.Contracts;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Domain.Entities;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Platform.Audit;
using AuditAction = PrimeERP.Domain.Enums.AuditAction;

namespace PrimeERP.Application.Legacy.Accounting
{
    /// <summary>المالك الوحيد لمنطق السنوات/الفترات المالية</summary>
    public class FiscalPeriodService : ServiceBase, IFiscalPeriodService
    {
        protected override string PermissionPrefix => "Settings";
        protected override string StringPrefix => "Str.Fiscal";
        protected override string EntityName => "FiscalYears";

        private readonly IAccountRepository _accounts;
        private readonly IFiscalPeriodRepository _fiscalPeriods;
        private readonly Entries _entries;
        private readonly IJournalRepository _ledger;

        public FiscalPeriodService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IAccountRepository accounts, IFiscalPeriodRepository fiscalPeriods, Entries entries, IJournalRepository ledger)
            : base(permissions, settings, localization, audit)
        {
            _accounts = accounts;
            _fiscalPeriods = fiscalPeriods;
            _entries = entries;
            _ledger = ledger;
        }


        public Result<FiscalYearDto> GetCurrentYear()
        {
            var year = _fiscalPeriods.GetCurrentYear();
            if (year == null)
                return Result.Fail<FiscalYearDto>(Msg("NoCurrentYear"), ErrorCode.NotFound);

            return Result.Ok(ToYearDto(year, includePeriods: true));
        }

        public Result<FiscalPeriodDto> GetCurrentPeriod() => GetPeriodFor(DateTime.Today);

        public Result<FiscalPeriodDto> GetPeriodFor(DateTime date)
        {
            var period = _fiscalPeriods.GetPeriodContaining(date.ToString("yyyy-MM-dd"));
            if (period == null)
                return Result.Fail<FiscalPeriodDto>(Msg("NoPeriodForDate"), ErrorCode.NotFound);

            return Result.Ok(ToPeriodDto(period));
        }

        public bool IsOpen(DateTime date) => _entries.IsOpen(date);

        public Result<List<FiscalYearDto>> GetAllYears() =>
            Result.Ok(_fiscalPeriods.GetAllYears().Select(y => ToYearDto(y, includePeriods: false)).ToList());

        public Result<List<FiscalPeriodDto>> GetPeriods(int yearId) =>
            Result.Ok(_fiscalPeriods.GetPeriods(yearId).Select(ToPeriodDto).ToList());

        public Result<List<FiscalPeriodDto>> GetOpenPeriods()
        {
            var open = _fiscalPeriods.GetAllPeriods()
                .Where(p => !p.IsClosed)
                .OrderBy(p => p.StartDate)
                .Select(ToPeriodDto)
                .ToList();

            return Result.Ok(open);
        }


        public Result<FiscalYearDto> CreateYear(DateTime start, int periodsCount = 12, string name = null)
        {
            if (!Can("Edit")) return FailDenied<FiscalYearDto>();

            var count = Check.Valid(periodsCount,
                new Field<int>(x => x, "", Name: "PeriodsCount", Must: n => n is 1 or 4 or 6 or 12, Message: "Str.Fiscal.InvalidPeriodsCount"));
            if (count.IsFailure) return count.As<FiscalYearDto>();

            start = start.Date;
            var end = FiscalPeriodCalc.EndOfYear(start);

            if (_fiscalPeriods.AnyYearOverlapping(start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"), null))
                return Result.Fail<FiscalYearDto>(Msg("OverlappingYear"), ErrorCode.Conflict);

            var configuredStartMonth = Setting(SettingKeys.Financial.FiscalYearStartMonth, 1);
            var monthMismatch = start.Month != configuredStartMonth;

            name ??= FiscalPeriodCalc.DefaultYearName(start, end);

            var periods = FiscalPeriodCalc.SplitPeriods(start, end, periodsCount);
            foreach (var period in periods) period.Name = Msg("PeriodName", period.PeriodNo);

            var isFirstYear = _fiscalPeriods.GetAllYears().Count == 0;

            var yearId = Tx(db =>
            {
                var id = _fiscalPeriods.InsertYear(db, new FiscalYear { Name = name, StartDate = start.ToString("yyyy-MM-dd"), EndDate = end.ToString("yyyy-MM-dd") });

                foreach (var p in periods)
                {
                    p.FiscalYearId = id;
                    _fiscalPeriods.InsertPeriod(db, p);
                }

                if (isFirstYear)
                    _fiscalPeriods.SetCurrentYear(db, id);

                return id;
            });

            var details = monthMismatch
                ? Msg("YearCreatedMismatch", name, start.Month, configuredStartMonth)
                : Msg("YearCreatedLog", name);
            Audit.Log("FiscalYears", yearId, AuditAction.Insert, details: details);

            var created = _fiscalPeriods.GetYearById(yearId);
            return Result.Ok(ToYearDto(created, includePeriods: true));
        }

        public Result SetCurrent(int yearId)
        {
            if (!Can("Edit")) return FailDenied();

            var year = _fiscalPeriods.GetYearById(yearId);
            if (year == null)
                return Result.Fail(Msg("YearNotFound"), ErrorCode.NotFound);

            if (year.IsClosed)
                return Result.Fail(Msg("YearIsClosed"), ErrorCode.ValidationFailed);

            Tx(db => _fiscalPeriods.SetCurrentYear(db, yearId));

            Audit.Log("FiscalYears", yearId, AuditAction.Update, details: Msg("YearSetCurrent", year.Name));
            return Result.Ok();
        }

        public Result ClosePeriod(int periodId)
        {
            if (!Can("ClosePeriod")) return FailDenied();

            var period = _fiscalPeriods.GetPeriodById(periodId);
            if (period == null)
                return Result.Fail(Msg("PeriodNotFound"), ErrorCode.NotFound);

            if (period.IsClosed)
                return Result.Fail(Msg("PeriodAlreadyClosed"), ErrorCode.ValidationFailed);

            if (_fiscalPeriods.GetPeriods(period.FiscalYearId).Any(p => p.PeriodNo < period.PeriodNo && !p.IsClosed))
                return Result.Fail(Msg("PreviousPeriodOpen"), ErrorCode.ValidationFailed);

            if (_ledger.CountUnpostedBetween(ParseDate(period.StartDate), ParseDate(period.EndDate)) > 0)
                return Result.Fail(Msg("HasUnpostedEntries"), ErrorCode.ValidationFailed);

            Tx(db => _fiscalPeriods.SetPeriodClosed(db, periodId, DateTime.Now, CurrentUser));

            Audit.Log("FiscalPeriods", periodId, AuditAction.Update, details: Msg("PeriodClosedLog", period.Name));
            return Result.Ok();
        }

        public Result ReopenPeriod(int periodId)
        {
            if (!Can("ReopenPeriod")) return FailDenied();

            var period = _fiscalPeriods.GetPeriodById(periodId);
            if (period == null)
                return Result.Fail(Msg("PeriodNotFound"), ErrorCode.NotFound);

            if (!period.IsClosed)
                return Result.Fail(Msg("PeriodAlreadyOpen"), ErrorCode.ValidationFailed);

            if (_fiscalPeriods.GetYearById(period.FiscalYearId)?.IsClosed == true)
                return Result.Fail(Msg("YearIsClosed"), ErrorCode.ValidationFailed);

            if (_fiscalPeriods.GetPeriods(period.FiscalYearId).Any(p => p.PeriodNo > period.PeriodNo && p.IsClosed))
                return Result.Fail(Msg("NextPeriodClosed"), ErrorCode.ValidationFailed);

            Tx(db => _fiscalPeriods.SetPeriodReopened(db, periodId));

            Audit.Log("FiscalPeriods", periodId, AuditAction.Update, details: Msg("PeriodReopenedLog", period.Name));
            return Result.Ok();
        }

        public Result CloseYear(int yearId)
        {
            if (!Can("CloseYear")) return FailDenied();

            var year = _fiscalPeriods.GetYearById(yearId);
            if (year == null)
                return Result.Fail(Msg("YearNotFound"), ErrorCode.NotFound);

            if (year.IsClosed)
                return Result.Fail(Msg("YearIsClosed"), ErrorCode.ValidationFailed);

            var periods = _fiscalPeriods.GetPeriods(yearId);
            if (periods.Count == 0 || periods.Any(p => !p.IsClosed))
                return Result.Fail(Msg("ClosePeriodsFirst"), ErrorCode.ValidationFailed);

            var retainedCode = Setting(SettingKeys.Accounts.RetainedEarnings, "");
            if (string.IsNullOrWhiteSpace(retainedCode))
                return Result.Fail(Msg("RetainedEarningsNotConfigured"), ErrorCode.Unexpected);

            var nominal = _accounts.Find(null, null, (int)AccountType.Revenue, leafOnly: true, includeInactive: false)
                .Concat(_accounts.Find(null, null, (int)AccountType.Expense, leafOnly: true, includeInactive: false));

            // حركة السنة وحدها
            var sums = _ledger.GetAccountSums(ParseDate(year.StartDate), ParseDate(year.EndDate), postedOnly: true)
                              .ToDictionary(s => s.AccountCode, s => s.SumDebit - s.SumCredit);
            var lines = new JournalLines()
                .Close(nominal.Select(a => (a.Code, sums.GetValueOrDefault(a.Code))), retainedCode)
                .ToList();
            var retainedLines = lines.Where(l => l.AccountCode == retainedCode).ToList();
            var (lossToRetained, profitToRetained) = (retainedLines.Sum(l => l.Debit), retainedLines.Sum(l => l.Credit));

            var closed = Commit(db =>
            {
                int? entryId = lines.Count > 0
                    ? Posting.Entry(_entries, db, ParseDate(year.EndDate), Msg("ClosingEntry", year.Name), "YearClosing", lines)
                    : null;

                _fiscalPeriods.SetYearClosed(db, yearId, DateTime.Now, CurrentUser, entryId);
                return Result.Ok(entryId);
            });
            if (closed.IsFailure) return closed;
            var closingEntryId = closed.Value;

            Audit.Log("FiscalYears", yearId, AuditAction.Update, details: Msg("YearClosedLog", year.Name, profitToRetained - lossToRetained, closingEntryId));
            return Result.Ok();
        }

        public Result ReopenYear(int yearId)
        {
            if (!Can("ReopenYear")) return FailDenied();

            var year = _fiscalPeriods.GetYearById(yearId);
            if (year == null)
                return Result.Fail(Msg("YearNotFound"), ErrorCode.NotFound);

            if (!year.IsClosed)
                return Result.Fail(Msg("YearAlreadyOpen"), ErrorCode.ValidationFailed);

            var reopened = Commit(db =>
            {
                Posting.Reverse(_entries, db, year.ClosingEntryId);
                _fiscalPeriods.SetYearReopened(db, yearId);
                return Result.Ok();
            });
            if (reopened.IsFailure) return reopened;

            Audit.Log("FiscalYears", yearId, AuditAction.Update, details: Msg("YearReopenedLog", year.Name, year.ClosingEntryId));
            return Result.Ok();
        }

        public Result<int> CountUnpostedInPeriod(int periodId)
        {
            var period = _fiscalPeriods.GetPeriodById(periodId);
            if (period == null)
                return Result.Fail<int>(Msg("PeriodNotFound"), ErrorCode.NotFound);

            return Result.Ok(_ledger.CountUnpostedBetween(ParseDate(period.StartDate), ParseDate(period.EndDate)));
        }


        private static DateTime ParseDate(string date) => Entries.ParseDate(date);

        private FiscalYearDto ToYearDto(FiscalYear y, bool includePeriods)
        {
            var dto = Rows.Copy(y, new FiscalYearDto(), to =>
            {
                to.StartDate = ParseDate(y.StartDate);
                to.EndDate = ParseDate(y.EndDate);
                to.StatusVariant = Closed(y.IsClosed);
            });

            if (includePeriods)
                dto.Periods = _fiscalPeriods.GetPeriods(y.Id).Select(ToPeriodDto).ToList();

            return dto;
        }

        private static FiscalPeriodDto ToPeriodDto(FiscalPeriod p) => Rows.Copy<FiscalPeriodDto>(p, new(), to =>
        {
            to.StartDate = ParseDate(p.StartDate);
            to.EndDate = ParseDate(p.EndDate);
            to.StatusVariant = Closed(p.IsClosed);
        });

        private static StatusVariant Closed(bool isClosed) =>
            Rows.State((isClosed, StatusVariant.Neutral, "Str.Fiscal.Status.Closed"), (true, StatusVariant.Success, "Str.Fiscal.Status.Open")).Variant;
    }
}
