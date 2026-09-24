using PrimeERP.Platform.Localization;
using PrimeERP.Application.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Domain.Rules;
using PrimeERP.Domain.Contracts;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Domain.Entities;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Platform.Audit;
using AuditAction = PrimeERP.Domain.Enums.AuditAction;

namespace PrimeERP.Application.Services.Accounting
{
    /// <summary>المالك الوحيد لمنطق السنوات/الفترات المالية</summary>
    public class FiscalPeriodService : ServiceBase, IFiscalPeriodService
    {
        protected override string PermissionPrefix => "Settings";
        protected override string StringPrefix => "Str.Fiscal";
        protected override string EntityName => "FiscalYears";

        private readonly IAccountService _accounts;
        private readonly IFiscalPeriodRepository _fiscalPeriods;
        private readonly Lazy<IJournalService> _journal;

        public FiscalPeriodService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IAccountService accounts, IFiscalPeriodRepository fiscalPeriods, Lazy<IJournalService> journal)
            : base(permissions, settings, localization, audit)
        {
            _accounts = accounts;
            _fiscalPeriods = fiscalPeriods;
            _journal = journal;
        }


        public Result<FiscalYearDto> GetCurrentYear()
        {
            var year = _fiscalPeriods.GetCurrentYear();
            if (year == null)
                return Result.Fail<FiscalYearDto>("لا توجد سنة مالية حالية مُعرَّفة", ErrorCode.NotFound);

            return Result.Ok(ToYearDto(year, includePeriods: true));
        }

        public Result<FiscalPeriodDto> GetCurrentPeriod() => GetPeriodFor(DateTime.Today);

        public Result<FiscalPeriodDto> GetPeriodFor(DateTime date)
        {
            var period = _fiscalPeriods.GetPeriodContaining(date.ToString("yyyy-MM-dd"));
            if (period == null)
                return Result.Fail<FiscalPeriodDto>("لا توجد فترة مالية مُعرَّفة لهذا التاريخ", ErrorCode.NotFound);

            return Result.Ok(ToPeriodDto(period));
        }

        public bool IsOpen(DateTime date)
        {
            var period = _fiscalPeriods.GetPeriodContaining(date.ToString("yyyy-MM-dd"));
            if (period == null)
                return !Setting(SettingKeys.Financial.RequireFiscalPeriod, false);

            if (period.IsClosed)
                return false;

            var year = _fiscalPeriods.GetYearById(period.FiscalYearId);
            return year == null || !year.IsClosed;
        }

        public Result<List<FiscalYearDto>> GetAllYears() =>
            Result.Ok(_fiscalPeriods.GetAllYears().Select(y => ToYearDto(y, includePeriods: false)).ToList());

        public Result<List<FiscalPeriodDto>> GetPeriods(int yearId) =>
            Result.Ok(_fiscalPeriods.GetPeriods(yearId).Select(ToPeriodDto).ToList());

        public Result<List<FiscalPeriodDto>> GetOpenPeriods()
        {
            var open = _fiscalPeriods.GetAllYears()
                .SelectMany(y => _fiscalPeriods.GetPeriods(y.Id))
                .Where(p => !p.IsClosed)
                .OrderBy(p => p.StartDate)
                .Select(ToPeriodDto)
                .ToList();

            return Result.Ok(open);
        }


        public Result<FiscalYearDto> CreateYear(DateTime start, int periodsCount = 12, string name = null)
        {
            if (!Can("Edit")) return FailDenied<FiscalYearDto>();

            if (periodsCount != 1 && periodsCount != 4 && periodsCount != 6 && periodsCount != 12)
                return Result.Fail<FiscalYearDto>(Msg("InvalidPeriodsCount"), ErrorCode.ValidationFailed);

            start = start.Date;
            var end = FiscalPeriodCalculator.EndOfYear(start);

            if (_fiscalPeriods.AnyYearOverlapping(start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"), null))
                return Result.Fail<FiscalYearDto>(Msg("OverlappingYear"), ErrorCode.Conflict);

            var configuredStartMonth = Setting(SettingKeys.Financial.FiscalYearStartMonth, 1);
            var monthMismatch = start.Month != configuredStartMonth;

            name ??= FiscalPeriodCalculator.DefaultYearName(start, end);

            var periods = FiscalPeriodCalculator.SplitPeriods(start, end, periodsCount).Select(p => new FiscalPeriod
            {
                PeriodNo  = p.PeriodNo,
                Name      = $"الفترة {p.PeriodNo}",
                StartDate = p.Start.ToString("yyyy-MM-dd"),
                EndDate   = p.End.ToString("yyyy-MM-dd")
            }).ToList();

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
                ? $"إنشاء سنة مالية {name} — تنبيه: شهر البداية ({start.Month}) لا يطابق الإعداد ({configuredStartMonth})"
                : $"إنشاء سنة مالية {name}";
            Audit.Log("FiscalYears", yearId, AuditAction.Insert, details: details);

            var created = _fiscalPeriods.GetYearById(yearId);
            return Result.Ok(ToYearDto(created, includePeriods: true));
        }

        public Result SetCurrent(int yearId)
        {
            if (!Can("Edit")) return FailDenied();

            var year = _fiscalPeriods.GetYearById(yearId);
            if (year == null)
                return Result.Fail("السنة المالية غير موجودة", ErrorCode.NotFound);

            if (year.IsClosed)
                return Result.Fail(Msg("YearIsClosed"), ErrorCode.ValidationFailed);

            Tx(db => _fiscalPeriods.SetCurrentYear(db, yearId));

            Audit.Log("FiscalYears", yearId, AuditAction.Update, details: $"تعيين {year.Name} كسنة حالية");
            return Result.Ok();
        }

        public Result ClosePeriod(int periodId)
        {
            if (!Can("ClosePeriod")) return FailDenied();

            var period = _fiscalPeriods.GetPeriodById(periodId);
            if (period == null)
                return Result.Fail("الفترة المالية غير موجودة", ErrorCode.NotFound);

            if (period.IsClosed)
                return Result.Fail("الفترة مقفلة بالفعل", ErrorCode.ValidationFailed);

            var earlierOpen = _fiscalPeriods.GetPeriods(period.FiscalYearId)
                .Any(p => p.PeriodNo < period.PeriodNo && !p.IsClosed);
            if (earlierOpen)
                return Result.Fail(Msg("PreviousPeriodOpen"), ErrorCode.ValidationFailed);

            var unposted = _journal.Value.CountUnpostedBetween(ParseDate(period.StartDate), ParseDate(period.EndDate));
            if (!unposted.IsSuccess)
                return Result.Fail(unposted.ErrorMessage, unposted.ErrorCode);
            if (unposted.Value > 0)
                return Result.Fail(Msg("HasUnpostedEntries"), ErrorCode.ValidationFailed);

            Tx(db => _fiscalPeriods.SetPeriodClosed(db, periodId, DateTime.Now, CurrentUser));

            Audit.Log("FiscalPeriods", periodId, AuditAction.Update, details: $"إقفال الفترة {period.Name}");
            return Result.Ok();
        }

        public Result ReopenPeriod(int periodId)
        {
            if (!Can("ReopenPeriod")) return FailDenied();

            var period = _fiscalPeriods.GetPeriodById(periodId);
            if (period == null)
                return Result.Fail("الفترة المالية غير موجودة", ErrorCode.NotFound);

            if (!period.IsClosed)
                return Result.Fail("الفترة مفتوحة بالفعل", ErrorCode.ValidationFailed);

            var year = _fiscalPeriods.GetYearById(period.FiscalYearId);
            if (year != null && year.IsClosed)
                return Result.Fail(Msg("YearIsClosed"), ErrorCode.ValidationFailed);

            var laterClosed = _fiscalPeriods.GetPeriods(period.FiscalYearId)
                .Any(p => p.PeriodNo > period.PeriodNo && p.IsClosed);
            if (laterClosed)
                return Result.Fail(Msg("NextPeriodClosed"), ErrorCode.ValidationFailed);

            Tx(db => _fiscalPeriods.SetPeriodReopened(db, periodId));

            Audit.Log("FiscalPeriods", periodId, AuditAction.Update, details: $"إعادة فتح الفترة {period.Name}");
            return Result.Ok();
        }

        public Result CloseYear(int yearId)
        {
            if (!Can("CloseYear")) return FailDenied();

            var year = _fiscalPeriods.GetYearById(yearId);
            if (year == null)
                return Result.Fail("السنة المالية غير موجودة", ErrorCode.NotFound);

            if (year.IsClosed)
                return Result.Fail(Msg("YearIsClosed"), ErrorCode.ValidationFailed);

            var periods = _fiscalPeriods.GetPeriods(yearId);
            if (periods.Count == 0 || periods.Any(p => !p.IsClosed))
                return Result.Fail("يجب إقفال كل الفترات المالية لهذه السنة أولاً", ErrorCode.ValidationFailed);

            var retainedCode = Setting(SettingKeys.Accounts.RetainedEarnings, "");
            if (string.IsNullOrWhiteSpace(retainedCode))
                return Result.Fail(Msg("RetainedEarningsNotConfigured"), ErrorCode.Unexpected);

            var revenueLeaves = _accounts.GetLeaves(AccountType.Revenue);
            if (!revenueLeaves.IsSuccess) return Result.Fail(revenueLeaves.ErrorMessage, revenueLeaves.ErrorCode);

            var expenseLeaves = _accounts.GetLeaves(AccountType.Expense);
            if (!expenseLeaves.IsSuccess) return Result.Fail(expenseLeaves.ErrorMessage, expenseLeaves.ErrorCode);

            var lines = new List<CreateJournalLineDto>();
            foreach (var a in revenueLeaves.Value.Concat(expenseLeaves.Value).Where(a => a.Balance != 0))
                lines.Add(ReverseLine(a.Code, a.Balance));

            var totalRevenue = -revenueLeaves.Value.Sum(a => a.Balance);
            var totalExpense = expenseLeaves.Value.Sum(a => a.Balance);
            var netProfit = totalRevenue - totalExpense;

            if (netProfit > 0)
                lines.Add(new CreateJournalLineDto { AccountCode = retainedCode, Credit = netProfit });
            else if (netProfit < 0)
                lines.Add(new CreateJournalLineDto { AccountCode = retainedCode, Debit = -netProfit });

            for (int i = 0; i < lines.Count; i++)
                lines[i].LineNo = i + 1;

            var dto = new CreateJournalDto
            {
                EntryDate   = ParseDate(year.EndDate),
                Description = $"قيد إقفال السنة المالية {year.Name}",
                Source      = "YearClosing",
                Lines       = lines
            };

            int? closingEntryId;
            try
            {
                closingEntryId = Tx(db =>
                {
                    int? entryId = null;

                    if (lines.Count > 0)
                    {
                        var createResult = _journal.Value.Create(db, dto);
                        if (!createResult.IsSuccess) throw new InvalidOperationException(createResult.ErrorMessage);
                        entryId = createResult.Value.Id;

                        var postResult = _journal.Value.Post(db, entryId.Value);
                        if (!postResult.IsSuccess) throw new InvalidOperationException(postResult.ErrorMessage);
                    }

                    _fiscalPeriods.SetYearClosed(db, yearId, DateTime.Now, CurrentUser, entryId);
                    return entryId;
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message);
            }

            Audit.Log("FiscalYears", yearId, AuditAction.Update, details: $"إقفال السنة المالية {year.Name} — صافي الربح: {netProfit:N2}، قيد الإقفال: {closingEntryId}");
            return Result.Ok();
        }

        public Result ReopenYear(int yearId)
        {
            if (!Can("ReopenYear")) return FailDenied();

            var year = _fiscalPeriods.GetYearById(yearId);
            if (year == null)
                return Result.Fail("السنة المالية غير موجودة", ErrorCode.NotFound);

            if (!year.IsClosed)
                return Result.Fail("السنة المالية مفتوحة بالفعل", ErrorCode.ValidationFailed);

            try
            {
                Tx(db =>
                {
                    if (year.ClosingEntryId.HasValue)
                    {
                        var unpostResult = _journal.Value.Unpost(db, year.ClosingEntryId.Value);
                        if (!unpostResult.IsSuccess) throw new InvalidOperationException(unpostResult.ErrorMessage);

                        var deleteResult = _journal.Value.Delete(db, year.ClosingEntryId.Value);
                        if (!deleteResult.IsSuccess) throw new InvalidOperationException(deleteResult.ErrorMessage);
                    }

                    _fiscalPeriods.SetYearReopened(db, yearId);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message);
            }

            Audit.Log("FiscalYears", yearId, AuditAction.Update, details: $"إعادة فتح السنة المالية {year.Name} — حذف قيد الإقفال {year.ClosingEntryId}");
            return Result.Ok();
        }

        public Result<int> CountUnpostedInPeriod(int periodId)
        {
            var period = _fiscalPeriods.GetPeriodById(periodId);
            if (period == null)
                return Result.Fail<int>("الفترة المالية غير موجودة", ErrorCode.NotFound);

            return _journal.Value.CountUnpostedBetween(ParseDate(period.StartDate), ParseDate(period.EndDate));
        }


        private static CreateJournalLineDto ReverseLine(string accountCode, decimal balance) =>
            balance < 0
                ? new CreateJournalLineDto { AccountCode = accountCode, Debit = -balance }
                : new CreateJournalLineDto { AccountCode = accountCode, Credit = balance };

        private static DateTime ParseDate(string date) => DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        private FiscalYearDto ToYearDto(FiscalYear y, bool includePeriods)
        {
            var dto = new FiscalYearDto
            {
                Id             = y.Id,
                Name           = y.Name,
                StartDate      = ParseDate(y.StartDate),
                EndDate        = ParseDate(y.EndDate),
                IsClosed       = y.IsClosed,
                ClosedAt       = y.ClosedAt,
                ClosedBy       = y.ClosedBy,
                ClosingEntryId = y.ClosingEntryId,
                IsCurrent      = y.IsCurrent,
                StatusVariant  = y.IsClosed ? StatusVariant.Neutral : StatusVariant.Success
            };

            if (includePeriods)
                dto.Periods = _fiscalPeriods.GetPeriods(y.Id).Select(ToPeriodDto).ToList();

            return dto;
        }

        private static FiscalPeriodDto ToPeriodDto(FiscalPeriod p) => new()
        {
            Id            = p.Id,
            FiscalYearId  = p.FiscalYearId,
            PeriodNo      = p.PeriodNo,
            Name          = p.Name,
            StartDate     = ParseDate(p.StartDate),
            EndDate       = ParseDate(p.EndDate),
            IsClosed      = p.IsClosed,
            ClosedAt      = p.ClosedAt,
            ClosedBy      = p.ClosedBy,
            StatusVariant = p.IsClosed ? StatusVariant.Neutral : StatusVariant.Success
        };
    }
}
