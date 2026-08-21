using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PrimeERP.Core;
using PrimeERP.Core.Common;
using PrimeERP.Core.Permissions;
using PrimeERP.Core.Validation;
using PrimeERP.Database;
using PrimeERP.Models;
using PrimeERP.Services.Accounting.DTOs;
using PrimeERP.Services.Settings;
using Auditor = PrimeERP.Core.Audit.AuditLogger;
using AuditAction = PrimeERP.Core.AuditAction;
using Db = PrimeERP.Core.Database.DbHelper;

namespace PrimeERP.Services.Accounting
{
    /// <summary>
    /// المالك الوحيد لمنطق السنوات/الفترات المالية — Repository تحته CRUD صرف فقط. يعتمد IJournalService
    /// (عقد جزئي — راجع IJournalService.cs) عبر Lazy&lt;T&gt; يُحلّ عند أول استخدام فعلي لا عند إنشاء هذه
    /// الخدمة، لحل التبعية الدائرية مع F.2.3: JournalService سيحتاج IFiscalPeriodService.IsOpen لاحقاً،
    /// وFiscalPeriodService يحتاج IJournalService لإقفال السنة — لو حُلّت الاثنتان في الـ constructor فوراً
    /// سيفشل تسجيل أيّهما تُسجَّل ثانياً في ServiceLocator. Lazy يؤجّل الحل حتى لحظة الاستدعاء الفعلي
    /// (ClosePeriod/CloseYear/ReopenYear)، وحينها تكون كل الخدمات مسجَّلة بالفعل من App.xaml.cs.
    /// </summary>
    public class FiscalPeriodService : IFiscalPeriodService
    {
        public static readonly FiscalPeriodService Instance = new();

        private readonly IPermissionService _permissions = PermissionService.Instance;
        private readonly ISettingsService _settings = SettingsService.Instance;
        private readonly IAccountService _accounts = AccountService.Instance;
        private readonly Lazy<IJournalService> _journal = new(() => ServiceLocator.Get<IJournalService>());

        private static string Denied => LocalizationService.Get("Str.PermissionDenied");
        private static string CurrentUser => AppSession.Username ?? "Admin";

        // ===================== القراءة =====================
        // بلا تحقق صلاحية عمداً — استعلامات تُستدعى بكثرة من خدمات أخرى (JournalService.IsOpen عند كل قيد)،
        // لا ينبغي أن تُحجب بصلاحية Fiscal منفصلة غير معرَّفة أصلاً؛ العملية الأصلية (تسجيل قيد، عرض تقرير)
        // لها صلاحيتها الخاصة المُتحقَّق منها بالفعل. نفس منطق AccountService.CanAcceptEntries.

        public Result<FiscalYearDto> GetCurrentYear()
        {
            var year = FiscalPeriodRepository.GetCurrentYear();
            if (year == null)
                return Result.Fail<FiscalYearDto>("لا توجد سنة مالية حالية مُعرَّفة", ErrorCode.NotFound);

            return Result.Ok(ToYearDto(year, includePeriods: true));
        }

        public Result<FiscalPeriodDto> GetCurrentPeriod() => GetPeriodFor(DateTime.Today);

        public Result<FiscalPeriodDto> GetPeriodFor(DateTime date)
        {
            var period = FiscalPeriodRepository.GetPeriodContaining(date.ToString("yyyy-MM-dd"));
            if (period == null)
                return Result.Fail<FiscalPeriodDto>("لا توجد فترة مالية مُعرَّفة لهذا التاريخ", ErrorCode.NotFound);

            return Result.Ok(ToPeriodDto(period));
        }

        /// <summary>مفتوح افتراضياً بلا فترة معرَّفة (يسمح بتشغيل النظام قبل إعداد السنوات المالية)، إلا لو
        /// SettingKeys.Financial.RequireFiscalPeriod=true. فترة/سنة مقفلة = مغلق دائماً بلا استثناء.</summary>
        public bool IsOpen(DateTime date)
        {
            var period = FiscalPeriodRepository.GetPeriodContaining(date.ToString("yyyy-MM-dd"));
            if (period == null)
                return !_settings.Get(SettingKeys.Financial.RequireFiscalPeriod, false);

            if (period.IsClosed)
                return false;

            var year = FiscalPeriodRepository.GetYearById(period.FiscalYearId);
            return year == null || !year.IsClosed;
        }

        public Result<List<FiscalYearDto>> GetAllYears() =>
            Result.Ok(FiscalPeriodRepository.GetAllYears().Select(y => ToYearDto(y, includePeriods: false)).ToList());

        public Result<List<FiscalPeriodDto>> GetPeriods(int yearId) =>
            Result.Ok(FiscalPeriodRepository.GetPeriods(yearId).Select(ToPeriodDto).ToList());

        public Result<List<FiscalPeriodDto>> GetOpenPeriods()
        {
            var open = FiscalPeriodRepository.GetAllYears()
                .SelectMany(y => FiscalPeriodRepository.GetPeriods(y.Id))
                .Where(p => !p.IsClosed)
                .OrderBy(p => p.StartDate)
                .Select(ToPeriodDto)
                .ToList();

            return Result.Ok(open);
        }

        // ===================== الكتابة =====================

        public Result<FiscalYearDto> CreateYear(DateTime start, int periodsCount = 12, string name = null)
        {
            if (!_permissions.Can(PermissionKeys.Settings.Edit))
                return Result.Fail<FiscalYearDto>(Denied, ErrorCode.Unauthorized);

            if (periodsCount != 1 && periodsCount != 4 && periodsCount != 6 && periodsCount != 12)
                return Result.Fail<FiscalYearDto>(LocalizationService.Get("Str.Fiscal.InvalidPeriodsCount"), ErrorCode.ValidationFailed);

            start = start.Date;
            var end = FiscalPeriodCalculator.EndOfYear(start);

            if (FiscalPeriodRepository.AnyYearOverlapping(start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"), null))
                return Result.Fail<FiscalYearDto>(LocalizationService.Get("Str.Fiscal.OverlappingYear"), ErrorCode.Conflict);

            // عدم تطابق شهر البداية مع SettingKeys.Financial.FiscalYearStartMonth تحذير لا يمنع — يُسجَّل في
            // تفاصيل Audit فقط، السنة تُنشأ بالتاريخ المطلوب فعلياً بلا حجب.
            var configuredStartMonth = _settings.Get(SettingKeys.Financial.FiscalYearStartMonth, 1);
            var monthMismatch = start.Month != configuredStartMonth;

            name ??= FiscalPeriodCalculator.DefaultYearName(start, end);

            var periods = FiscalPeriodCalculator.SplitPeriods(start, end, periodsCount).Select(p => new FiscalPeriod
            {
                PeriodNo  = p.PeriodNo,
                Name      = $"الفترة {p.PeriodNo}",
                StartDate = p.Start.ToString("yyyy-MM-dd"),
                EndDate   = p.End.ToString("yyyy-MM-dd")
            }).ToList();

            var isFirstYear = FiscalPeriodRepository.GetAllYears().Count == 0;

            var yearId = Db.RunTransaction((conn, tx) =>
            {
                var id = FiscalPeriodRepository.InsertYear(conn, tx, new FiscalYear { Name = name, StartDate = start.ToString("yyyy-MM-dd"), EndDate = end.ToString("yyyy-MM-dd") });

                foreach (var p in periods)
                {
                    p.FiscalYearId = id;
                    FiscalPeriodRepository.InsertPeriod(conn, tx, p);
                }

                if (isFirstYear)
                    FiscalPeriodRepository.SetCurrentYear(conn, tx, id);

                return id;
            });

            var details = monthMismatch
                ? $"إنشاء سنة مالية {name} — تنبيه: شهر البداية ({start.Month}) لا يطابق الإعداد ({configuredStartMonth})"
                : $"إنشاء سنة مالية {name}";
            Auditor.Log("FiscalYears", yearId, AuditAction.Insert, details: details);

            var created = FiscalPeriodRepository.GetYearById(yearId);
            return Result.Ok(ToYearDto(created, includePeriods: true));
        }

        public Result SetCurrent(int yearId)
        {
            if (!_permissions.Can(PermissionKeys.Settings.Edit))
                return Result.Fail(Denied, ErrorCode.Unauthorized);

            var year = FiscalPeriodRepository.GetYearById(yearId);
            if (year == null)
                return Result.Fail("السنة المالية غير موجودة", ErrorCode.NotFound);

            if (year.IsClosed)
                return Result.Fail(LocalizationService.Get("Str.Fiscal.YearIsClosed"), ErrorCode.ValidationFailed);

            Db.RunTransaction((conn, tx) => FiscalPeriodRepository.SetCurrentYear(conn, tx, yearId));

            Auditor.Log("FiscalYears", yearId, AuditAction.Update, details: $"تعيين {year.Name} كسنة حالية");
            return Result.Ok();
        }

        public Result ClosePeriod(int periodId)
        {
            if (!_permissions.Can(PermissionKeys.Settings.ClosePeriod))
                return Result.Fail(Denied, ErrorCode.Unauthorized);

            var period = FiscalPeriodRepository.GetPeriodById(periodId);
            if (period == null)
                return Result.Fail("الفترة المالية غير موجودة", ErrorCode.NotFound);

            if (period.IsClosed)
                return Result.Fail("الفترة مقفلة بالفعل", ErrorCode.ValidationFailed);

            var earlierOpen = FiscalPeriodRepository.GetPeriods(period.FiscalYearId)
                .Any(p => p.PeriodNo < period.PeriodNo && !p.IsClosed);
            if (earlierOpen)
                return Result.Fail(LocalizationService.Get("Str.Fiscal.PreviousPeriodOpen"), ErrorCode.ValidationFailed);

            var unposted = _journal.Value.CountUnpostedBetween(ParseDate(period.StartDate), ParseDate(period.EndDate));
            if (!unposted.IsSuccess)
                return Result.Fail(unposted.ErrorMessage, unposted.ErrorCode);
            if (unposted.Value > 0)
                return Result.Fail(LocalizationService.Get("Str.Fiscal.HasUnpostedEntries"), ErrorCode.ValidationFailed);

            Db.RunTransaction((conn, tx) => FiscalPeriodRepository.SetPeriodClosed(conn, tx, periodId, DateTime.Now, CurrentUser));

            Auditor.Log("FiscalPeriods", periodId, AuditAction.Update, details: $"إقفال الفترة {period.Name}");
            return Result.Ok();
        }

        public Result ReopenPeriod(int periodId)
        {
            if (!_permissions.Can(PermissionKeys.Settings.ReopenPeriod))
                return Result.Fail(Denied, ErrorCode.Unauthorized);

            var period = FiscalPeriodRepository.GetPeriodById(periodId);
            if (period == null)
                return Result.Fail("الفترة المالية غير موجودة", ErrorCode.NotFound);

            if (!period.IsClosed)
                return Result.Fail("الفترة مفتوحة بالفعل", ErrorCode.ValidationFailed);

            var year = FiscalPeriodRepository.GetYearById(period.FiscalYearId);
            if (year != null && year.IsClosed)
                return Result.Fail(LocalizationService.Get("Str.Fiscal.YearIsClosed"), ErrorCode.ValidationFailed);

            var laterClosed = FiscalPeriodRepository.GetPeriods(period.FiscalYearId)
                .Any(p => p.PeriodNo > period.PeriodNo && p.IsClosed);
            if (laterClosed)
                return Result.Fail(LocalizationService.Get("Str.Fiscal.NextPeriodClosed"), ErrorCode.ValidationFailed);

            Db.RunTransaction((conn, tx) => FiscalPeriodRepository.SetPeriodReopened(conn, tx, periodId));

            Auditor.Log("FiscalPeriods", periodId, AuditAction.Update, details: $"إعادة فتح الفترة {period.Name}");
            return Result.Ok();
        }

        public Result CloseYear(int yearId)
        {
            if (!_permissions.Can(PermissionKeys.Settings.CloseYear))
                return Result.Fail(Denied, ErrorCode.Unauthorized);

            var year = FiscalPeriodRepository.GetYearById(yearId);
            if (year == null)
                return Result.Fail("السنة المالية غير موجودة", ErrorCode.NotFound);

            if (year.IsClosed)
                return Result.Fail(LocalizationService.Get("Str.Fiscal.YearIsClosed"), ErrorCode.ValidationFailed);

            var periods = FiscalPeriodRepository.GetPeriods(yearId);
            if (periods.Count == 0 || periods.Any(p => !p.IsClosed))
                return Result.Fail("يجب إقفال كل الفترات المالية لهذه السنة أولاً", ErrorCode.ValidationFailed);

            var retainedCode = _settings.Get(SettingKeys.Accounts.RetainedEarnings, "");
            if (string.IsNullOrWhiteSpace(retainedCode))
                return Result.Fail(LocalizationService.Get("Str.Fiscal.RetainedEarningsNotConfigured"), ErrorCode.Unexpected);

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
                closingEntryId = Db.RunTransaction((conn, tx) =>
                {
                    int? entryId = null;

                    if (lines.Count > 0)
                    {
                        var createResult = _journal.Value.Create(conn, tx, dto);
                        if (!createResult.IsSuccess) throw new InvalidOperationException(createResult.ErrorMessage);
                        entryId = createResult.Value.Id;

                        var postResult = _journal.Value.Post(conn, tx, entryId.Value);
                        if (!postResult.IsSuccess) throw new InvalidOperationException(postResult.ErrorMessage);
                    }

                    FiscalPeriodRepository.SetYearClosed(conn, tx, yearId, DateTime.Now, CurrentUser, entryId);
                    return entryId;
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message);
            }

            Auditor.Log("FiscalYears", yearId, AuditAction.Update, details: $"إقفال السنة المالية {year.Name} — صافي الربح: {netProfit:N2}، قيد الإقفال: {closingEntryId}");
            return Result.Ok();
        }

        public Result ReopenYear(int yearId)
        {
            if (!_permissions.Can(PermissionKeys.Settings.ReopenYear))
                return Result.Fail(Denied, ErrorCode.Unauthorized);

            var year = FiscalPeriodRepository.GetYearById(yearId);
            if (year == null)
                return Result.Fail("السنة المالية غير موجودة", ErrorCode.NotFound);

            if (!year.IsClosed)
                return Result.Fail("السنة المالية مفتوحة بالفعل", ErrorCode.ValidationFailed);

            try
            {
                Db.RunTransaction((conn, tx) =>
                {
                    if (year.ClosingEntryId.HasValue)
                    {
                        var unpostResult = _journal.Value.Unpost(conn, tx, year.ClosingEntryId.Value);
                        if (!unpostResult.IsSuccess) throw new InvalidOperationException(unpostResult.ErrorMessage);

                        var deleteResult = _journal.Value.Delete(conn, tx, year.ClosingEntryId.Value);
                        if (!deleteResult.IsSuccess) throw new InvalidOperationException(deleteResult.ErrorMessage);
                    }

                    FiscalPeriodRepository.SetYearReopened(conn, tx, yearId);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message);
            }

            Auditor.Log("FiscalYears", yearId, AuditAction.Update, details: $"إعادة فتح السنة المالية {year.Name} — حذف قيد الإقفال {year.ClosingEntryId}");
            return Result.Ok();
        }

        public Result<int> CountUnpostedInPeriod(int periodId)
        {
            var period = FiscalPeriodRepository.GetPeriodById(periodId);
            if (period == null)
                return Result.Fail<int>("الفترة المالية غير موجودة", ErrorCode.NotFound);

            return _journal.Value.CountUnpostedBetween(ParseDate(period.StartDate), ParseDate(period.EndDate));
        }

        // ===================== أدوات داخلية =====================

        /// <summary>يعكس رصيد حساب ليصفّره — رصيد دائن (سالب) يُصفَّر بسطر مدين، رصيد مدين (موجب) يُصفَّر بسطر دائن. تُستخدم لكل من الإيرادات والمصروفات معاً بلا تمييز نوع.</summary>
        private static CreateJournalLineDto ReverseLine(string accountCode, decimal balance) =>
            balance < 0
                ? new CreateJournalLineDto { AccountCode = accountCode, Debit = -balance }
                : new CreateJournalLineDto { AccountCode = accountCode, Credit = balance };

        private static DateTime ParseDate(string date) => DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static FiscalYearDto ToYearDto(FiscalYear y, bool includePeriods)
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
                dto.Periods = FiscalPeriodRepository.GetPeriods(y.Id).Select(ToPeriodDto).ToList();

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
