using PrimeERP.Platform.Localization;
using PrimeERP.Application.Services;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Domain.Entities;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Platform.Audit;
using AuditAction = PrimeERP.Domain.Enums.AuditAction;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Services.Accounting
{
    /// <summary>
    /// المالك الوحيد لمنطق قيود اليومية — Repository تحته CRUD صرف فقط. الأرصدة دائماً بإعادة حساب من القيود
    /// المرحّلة (IAccountService.RecalculateBalance) لا زيادة/نقص تراكمي — يمنع الانحراف التراكمي مع الوقت.
    ///
    /// نسخ (conn,tx) من Create/Post/Unpost/Delete "رفيعة" عمداً: بلا تحقق صلاحية (المستدعي — FiscalPeriodService
    /// حالياً — تحقق صلاحيته الخاصة بالفعل)، وبلا أي قراءة عبر اتصال منفصل (Db.Query/JournalRepository بلا
    /// (conn,tx) تُعلِّق/deadlock على SQLite من داخل معاملة خارجية مفتوحة على نفس الخيط — خطأ حقيقي اكتُشف
    /// أثناء بناء هذه الخدمة، مُصلَح بإضافة نسخ (conn,tx) لكل قراءة تُستخدم هنا: DbHelper.Query،
    /// _journal.GetById/GetLines/GetPostedLinesForAccount، NumberSequenceService.Next،
    /// AccountService.RecalculateBalance — راجع تعليقاتها).
    /// </summary>
    public class JournalService : ServiceBase, IJournalService
    {
        protected override string PermissionPrefix => "Journal";
        protected override string StringPrefix => "Str.Journal";
        protected override string EntityName => "JournalEntries";

        private readonly IAccountService _accounts;
        private readonly IFiscalPeriodService _fiscalPeriods;
        private readonly INumberSequenceService _numbers;
        private readonly IJournalRepository _journal;
        private readonly IAccountRepository _accountRepo;

        public JournalService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            IAccountService accounts, IFiscalPeriodService fiscalPeriods, INumberSequenceService numbers,
            IJournalRepository journal, IAccountRepository accountRepo)
            : base(permissions, settings, localization, audit)
        {
            _accounts = accounts;
            _fiscalPeriods = fiscalPeriods;
            _numbers = numbers;
            _journal = journal;
            _accountRepo = accountRepo;
        }

        // ===================== القراءة =====================

        public Result<PagedResult<JournalEntryDto>> GetPaged(int page, int pageSize, JournalFilter filter = null)
        {
            if (!Can("View")) return FailDenied<PagedResult<JournalEntryDto>>();

            filter ??= new JournalFilter();

            var (items, total) = _journal.GetPaged(
                page, pageSize,
                filter.SearchText, filter.DateFrom, filter.DateTo, filter.Source,
                filter.IsPosted, filter.AccountCode, filter.MinAmount, filter.MaxAmount,
                filter.SortBy, filter.SortDescending);

            var lineCounts = _journal.GetLineCounts(items.Select(e => e.Id));
            var dtos = items.Select(e => ToDto(e, lineCounts.GetValueOrDefault(e.Id))).ToList();

            return Result.Ok(new PagedResult<JournalEntryDto> { Items = dtos, TotalCount = total, Page = page, PageSize = pageSize });
        }

        public Result<JournalEntryDetailDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<JournalEntryDetailDto>();

            var entry = _journal.GetById(id);
            if (entry == null)
                return Result.Fail<JournalEntryDetailDto>("القيد غير موجود", ErrorCode.NotFound);

            return Result.Ok(ToDetailDto(entry, _journal.GetLines(id)));
        }

        public Result<JournalEntryDto> GetByEntryNo(string entryNo)
        {
            if (!Can("View")) return FailDenied<JournalEntryDto>();

            var entry = _journal.GetByEntryNo(entryNo);
            if (entry == null)
                return Result.Fail<JournalEntryDto>("القيد غير موجود", ErrorCode.NotFound);

            return Result.Ok(ToDto(entry, _journal.GetLines(entry.Id).Count));
        }

        // ===================== الإنشاء =====================

        public Result<JournalEntryDto> Create(CreateJournalDto dto)
        {
            if (!Can("Create")) return FailDenied<JournalEntryDto>();

            var shape = ValidateShape(dto);
            if (!shape.IsSuccess) return Result.Fail<JournalEntryDto>(shape.ErrorMessage, shape.ErrorCode);

            var accounts = ValidateAndResolveAccounts(dto.Lines);
            if (!accounts.IsSuccess) return Result.Fail<JournalEntryDto>(accounts.ErrorMessage, accounts.ErrorCode);

            if (!_fiscalPeriods.IsOpen(dto.EntryDate))
                return Result.Fail<JournalEntryDto>(Msg("PeriodClosed"), ErrorCode.ValidationFailed);

            var prefix = Setting(SettingKeys.Documents.JournalPrefix, "JE");
            var entryNo = _numbers.Next(prefix);

            var newId = Db.RunTransaction((conn, tx) => InsertEntryWithLines(conn, tx, entryNo, dto, accounts.Value));

            Audit.Log(EntityName, newId, AuditAction.Insert, details: $"إنشاء قيد {entryNo} — {dto.Lines.Count} سطر");

            return Result.Ok(BuildDto(newId, entryNo, dto));
        }

        /// <summary>
        /// بمعاملة خارجية — يخدم FiscalPeriodService.CloseYear حالياً، ومستندات F.4 لاحقاً (عبر بديل IPostable
        /// الذي يُبنى في F.4 بعد وجود جداوله فعلياً — راجع MIGRATION_INVENTORY.md). بلا تحقق صلاحية (المستدعي
        /// تحقق صلاحيته الخاصة) وبلا تحقق فترة مفتوحة عمداً: قيد الإقفال السنوي يُنشأ بتاريخ سنة أُقفلت فتراتها
        /// بالفعل، فتحقق IsOpen سيرفضه دائماً — التحقق الحقيقي هنا هو صحة السطور/الحسابات فقط.
        /// </summary>
        public Result<JournalEntryDto> Create(DbConnection conn, DbTransaction tx, CreateJournalDto dto)
        {
            var shape = ValidateShape(dto);
            if (!shape.IsSuccess) return Result.Fail<JournalEntryDto>(shape.ErrorMessage, shape.ErrorCode);

            // نسخة قراءة مصغّرة تعمل عبر نفس (conn,tx) — لا ValidateAndResolveAccounts العادية (تفتح اتصالاً
            // جديداً عبر IAccountService.GetByCode، يُعلِّق/deadlock من داخل معاملة خارجية قائمة).
            var accounts = ValidateAccountsForTransaction(conn, tx, dto.Lines);
            if (!accounts.IsSuccess) return Result.Fail<JournalEntryDto>(accounts.ErrorMessage, accounts.ErrorCode);

            var prefix = Setting(SettingKeys.Documents.JournalPrefix, "JE");
            var entryNo = _numbers.Next(conn, tx, prefix);

            var newId = InsertEntryWithLines(conn, tx, entryNo, dto, accounts.Value);
            return Result.Ok(BuildDto(newId, entryNo, dto));
        }

        // ===================== التعديل والحذف =====================

        public Result Update(CreateJournalDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var existing = _journal.GetById(dto.Id);
            if (existing == null)
                return Result.Fail("القيد غير موجود", ErrorCode.NotFound);

            if (existing.IsPosted)
                return Result.Fail(Msg("PostedCannotEdit"), ErrorCode.ValidationFailed);

            var editable = EnsureManualSource(existing.Source, "تعديل");
            if (editable.IsFailure) return editable;

            var shape = ValidateShape(dto);
            if (!shape.IsSuccess) return shape;

            var accounts = ValidateAndResolveAccounts(dto.Lines);
            if (!accounts.IsSuccess) return Result.Fail(accounts.ErrorMessage, accounts.ErrorCode);

            // الفترة مفتوحة للتاريخ القديم والجديد معاً — نقل قيد من/إلى فترة مقفلة ممنوع بنفس القدر.
            if (!_fiscalPeriods.IsOpen(ParseDate(existing.EntryDate)))
                return Result.Fail(Msg("PeriodClosed"), ErrorCode.ValidationFailed);
            if (!_fiscalPeriods.IsOpen(dto.EntryDate))
                return Result.Fail(Msg("PeriodClosed"), ErrorCode.ValidationFailed);

            var totalDebit = dto.Lines.Sum(l => l.Debit);
            var totalCredit = dto.Lines.Sum(l => l.Credit);

            Db.RunTransaction((conn, tx) =>
            {
                _journal.DeleteLines(dto.Id, conn, tx);

                int lineNo = 1;
                foreach (var l in dto.Lines)
                    _journal.InsertLine(conn, tx, dto.Id, lineNo++, new JournalLine
                    {
                        AccountCode = l.AccountCode,
                        AccountName = accounts.Value.GetValueOrDefault(l.AccountCode),
                        Debit = l.Debit, Credit = l.Credit, Notes = l.Notes
                    });

                // EntryNo لا يتغيّر أبداً — فقط التاريخ/البيان/السطور/الإجماليات.
                _journal.UpdateEntry(conn, tx, dto.Id, dto.EntryDate.ToString("yyyy-MM-dd"), dto.Description);
                _journal.UpdateTotals(conn, tx, dto.Id, totalDebit, totalCredit);
            });

            Audit.Log(EntityName, dto.Id, AuditAction.Update, details: $"تعديل قيد {existing.EntryNo}");
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var entry = _journal.GetById(id);
            if (entry == null)
                return Result.Fail("القيد غير موجود", ErrorCode.NotFound);

            if (entry.IsPosted)
                return Result.Fail(Msg("PostedCannotDelete"), ErrorCode.ValidationFailed);

            var deletable = EnsureManualSource(entry.Source, "حذف");
            if (deletable.IsFailure) return deletable;

            if (!_fiscalPeriods.IsOpen(ParseDate(entry.EntryDate)))
                return Result.Fail(Msg("PeriodClosed"), ErrorCode.ValidationFailed);

            Db.RunTransaction((conn, tx) => Delete(conn, tx, id));

            Audit.Log(EntityName, id, AuditAction.Delete, details: $"حذف قيد {entry.EntryNo}");
            return Result.Ok();
        }

        /// <summary>بمعاملة خارجية — يخدم FiscalPeriodService.ReopenYear (يحذف قيد الإقفال بعد إلغاء ترحيله). بلا تحقق: المستدعي تحقق الحالة بنفسه (year.IsClosed) قبل الوصول لهنا.</summary>
        public Result Delete(DbConnection conn, DbTransaction tx, int id)
        {
            _journal.DeleteLines(id, conn, tx);
            _journal.DeleteHeader(id, conn, tx);
            return Result.Ok();
        }

        // ===================== الترحيل =====================

        public Result Post(int id)
        {
            if (!Can("Post")) return FailDenied();

            var entry = _journal.GetById(id);
            var (error, code) = ValidatePostable(entry);
            if (error != null) return Result.Fail(error, code);

            var result = Db.RunTransaction((conn, tx) => Post(conn, tx, id));
            if (!result.IsSuccess) return result;

            Audit.Log(EntityName, id, AuditAction.Update, details: $"ترحيل قيد {entry.EntryNo}");
            return Result.Ok();
        }

        /// <summary>بمعاملة خارجية — يخدم FiscalPeriodService.CloseYear (ترحيل قيد الإقفال فور إنشائه). بلا تحقق صلاحية/حالة: المستدعي بنى القيد للتو ويعرف أنه قابل للترحيل.</summary>
        public Result Post(DbConnection conn, DbTransaction tx, int id)
        {
            var lines = _journal.GetLines(id, conn, tx);
            _journal.SetPosted(conn, tx, id, DateTime.Now, CurrentUser);

            foreach (var code in lines.Select(l => l.AccountCode).Distinct())
                _accounts.RecalculateBalance(conn, tx, code);

            return Result.Ok();
        }

        public Result Unpost(int id)
        {
            if (!Can("Unpost")) return FailDenied();

            var entry = _journal.GetById(id);
            if (entry == null)
                return Result.Fail("القيد غير موجود", ErrorCode.NotFound);

            if (!entry.IsPosted)
                return Result.Fail(Msg("NotPostedCannotUnpost"), ErrorCode.ValidationFailed);

            if (entry.Source == ClosingEntrySource)
                return Result.Fail(Msg("ClosingEntryCannotUnpost"), ErrorCode.ValidationFailed);

            if (!_fiscalPeriods.IsOpen(ParseDate(entry.EntryDate)))
                return Result.Fail(Msg("PeriodClosed"), ErrorCode.ValidationFailed);

            var result = Db.RunTransaction((conn, tx) => Unpost(conn, tx, id));
            if (!result.IsSuccess) return result;

            Audit.Log(EntityName, id, AuditAction.Update, details: $"إلغاء ترحيل قيد {entry.EntryNo}");
            return Result.Ok();
        }

        /// <summary>بمعاملة خارجية — يخدم FiscalPeriodService.ReopenYear. بلا تحقق: المستدعي تحقق الحالة (year.IsClosed) بنفسه.</summary>
        public Result Unpost(DbConnection conn, DbTransaction tx, int id)
        {
            var lines = _journal.GetLines(id, conn, tx);
            _journal.SetUnposted(conn, tx, id);

            foreach (var code in lines.Select(l => l.AccountCode).Distinct())
                _accounts.RecalculateBalance(conn, tx, code);

            return Result.Ok();
        }

        /// <summary>
        /// معاملة واحدة، الكل أو لا شيء: يتحقق من كل القيود المطلوبة أولاً بلا أي كتابة؛ لو فشل واحد على الأقل
        /// لا يُفتح Db.RunTransaction إطلاقاً (لا شيء يُرحَّل، SuccessCount=0)، والفشول موصوفة في Failures.
        /// النتيجة دائماً Result.Ok(batch) — "نجاح" الاستدعاء يعني "التقرير جاهز"، لا أن كل عنصر رُحِّل.
        /// </summary>
        public Result<JournalBatchResult> PostBatch(List<int> ids)
        {
            if (!Can("Post")) return FailDenied<JournalBatchResult>();

            var batch = new JournalBatchResult();
            var toPost = new List<int>();

            foreach (var id in ids)
            {
                var entry = _journal.GetById(id);
                var (error, _) = ValidatePostable(entry);
                if (error != null) batch.Failures.Add((id, error));
                else toPost.Add(id);
            }

            batch.FailedCount = batch.Failures.Count;
            if (batch.FailedCount > 0)
                return Result.Ok(batch);

            Db.RunTransaction((conn, tx) =>
            {
                foreach (var id in toPost)
                    Post(conn, tx, id);
            });

            batch.SuccessCount = toPost.Count;
            Audit.Log(EntityName, 0, AuditAction.Update, details: $"ترحيل دفعي: {batch.SuccessCount} قيد");

            return Result.Ok(batch);
        }

        // ===================== التقارير =====================

        public Result<List<TrialBalanceLine>> GetTrialBalance(DateTime from, DateTime to, bool includeZero = false, bool postedOnly = true)
        {
            if (!Can("View")) return FailDenied<List<TrialBalanceLine>>();

            var leaves = _accounts.GetLeaves();
            if (!leaves.IsSuccess)
                return Result.Fail<List<TrialBalanceLine>>(leaves.ErrorMessage, leaves.ErrorCode);

            // استعلامان مجمَّعان فقط (لا حلقة استعلامات على الحسابات): مرة منذ البداية حتى قبل from يوماً
            // للرصيد الافتتاحي، ومرة بين from وto لحركة الفترة.
            var openingSums = _journal.GetAccountSums(null, from.AddDays(-1), postedOnly).ToDictionary(x => x.AccountCode);
            var periodSums  = _journal.GetAccountSums(from, to, postedOnly).ToDictionary(x => x.AccountCode);

            var result = new List<TrialBalanceLine>();
            decimal totalClosingDebit = 0, totalClosingCredit = 0;

            foreach (var account in leaves.Value)
            {
                openingSums.TryGetValue(account.Code, out var opening);
                periodSums.TryGetValue(account.Code, out var period);

                var openingBalance = opening.SumDebit - opening.SumCredit;
                var closingBalance = openingBalance + (period.SumDebit - period.SumCredit);

                if (!includeZero && openingBalance == 0 && period.SumDebit == 0 && period.SumCredit == 0 && closingBalance == 0)
                    continue;

                // تقسيم رصيد بالإشارة (Balance = مدين - دائن) لعمودي مدين/دائن للعرض — صيغة عامة واحدة بلا
                // تفرّع حسب نوع الحساب: رصيد موجب يظهر في عمود المدين، سالب في عمود الدائن، دائماً، بصرف
                // النظر عن كون الحساب "طبيعته مدينة" (أصول/مصروفات) أو "دائنة" (خصوم/حقوق ملكية/إيرادات) —
                // "الطبيعة" تصف أيّ عمود يُتوقَّع أن يكون غير صفري لحساب سليم، لا تُغيّر صيغة التقسيم نفسها.
                var line = new TrialBalanceLine
                {
                    Code    = account.Code,
                    Name    = account.Name,
                    Level   = account.Level,
                    Type    = account.Type,
                    IsLeaf  = account.IsLeaf,
                    OpeningDebit  = Math.Max(openingBalance, 0),
                    OpeningCredit = Math.Max(-openingBalance, 0),
                    PeriodDebit   = period.SumDebit,
                    PeriodCredit  = period.SumCredit,
                    ClosingDebit  = Math.Max(closingBalance, 0),
                    ClosingCredit = Math.Max(-closingBalance, 0)
                };

                result.Add(line);
                totalClosingDebit  += line.ClosingDebit;
                totalClosingCredit += line.ClosingCredit;
            }

            if (totalClosingDebit != totalClosingCredit)
                return Result.Fail<List<TrialBalanceLine>>(
                    $"{Msg("TrialBalanceMismatch")} ({totalClosingDebit - totalClosingCredit:N2})", ErrorCode.Unexpected);

            return Result.Ok(result);
        }

        public Result<int> CountUnpostedBetween(DateTime from, DateTime to) =>
            Result.Ok(_journal.CountUnpostedBetween(from, to));

        // ===================== أدوات داخلية =====================

        /// <summary>قيمة Source المستخدَمة لقيود الإقفال السنوي — يمنع إلغاء ترحيلها إلا عبر FiscalPeriodService.ReopenYear. يجب أن تطابق القيمة التي تضبطها FiscalPeriodService.CloseYear بالضبط.</summary>
        private const string ClosingEntrySource = "YearClosing";

        private static Result ValidateShape(CreateJournalDto dto)
        {
            var entryModel = new JournalEntry
            {
                EntryDate = dto.EntryDate.ToString("yyyy-MM-dd"),
                Lines = dto.Lines.Select(l => new JournalLine { AccountCode = l.AccountCode, Debit = l.Debit, Credit = l.Credit }).ToList()
            };

            var validation = new JournalValidator().Validate(entryModel);
            return validation.IsValid ? Result.Ok() : Result.Fail(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);
        }

        /// <summary>لمسارات خارج أي معاملة قائمة (Create(dto) العادية، Update، ValidatePostable) — عبر IAccountService الكامل (صلاحية + تحقق).</summary>
        private Result<Dictionary<string, string>> ValidateAndResolveAccounts(List<CreateJournalLineDto> lines)
        {
            var resolved = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                if (resolved.ContainsKey(line.AccountCode)) continue;

                var account = _accounts.GetByCode(line.AccountCode);
                if (!account.IsSuccess)
                    return Result.Fail<Dictionary<string, string>>($"{Msg("AccountNotFound")}: {line.AccountCode}", ErrorCode.ValidationFailed);

                if (!account.Value.IsLeaf)
                    return Result.Fail<Dictionary<string, string>>($"{Msg("AccountNotLeaf")}: {line.AccountCode}", ErrorCode.ValidationFailed);

                if (!account.Value.IsActive)
                    return Result.Fail<Dictionary<string, string>>($"{Msg("AccountInactive")}: {line.AccountCode}", ErrorCode.ValidationFailed);

                resolved[line.AccountCode] = account.Value.Name;
            }

            var allowDuplicates = Setting(SettingKeys.Financial.AllowDuplicateAccountInEntry, false);
            if (!allowDuplicates)
            {
                var duplicate = lines.GroupBy(l => l.AccountCode).FirstOrDefault(g => g.Count() > 1);
                if (duplicate != null)
                    return Result.Fail<Dictionary<string, string>>($"{Msg("DuplicateAccount")}: {duplicate.Key}", ErrorCode.ValidationFailed);
            }

            return Result.Ok(resolved);
        }

        /// <summary>
        /// لمسار (conn,tx) فقط — Create(conn,tx,dto) المستدعى من FiscalPeriodService.CloseYear وهي بالفعل
        /// داخل معاملتها. تحقق مصغّر عبر AccountRepository مباشرة (لا IAccountService — كل قراءاته تفتح اتصالاً
        /// منفصلاً وتُعلِّق/deadlock هنا)، بلا فحص تكرار حسابات (سطور قيد الإقفال مبنية برمجياً بلا تكرار أصلاً)
        /// وبلا صلاحية (المستدعي تحقق صلاحيته الخاصة).
        /// </summary>
        private Result<Dictionary<string, string>> ValidateAccountsForTransaction(DbConnection conn, DbTransaction tx, List<CreateJournalLineDto> lines)
        {
            var resolved = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                if (resolved.ContainsKey(line.AccountCode)) continue;

                var account = _accountRepo.GetByCode(line.AccountCode, conn, tx);
                if (account == null)
                    return Result.Fail<Dictionary<string, string>>($"{Msg("AccountNotFound")}: {line.AccountCode}", ErrorCode.ValidationFailed);

                if (!account.IsLeaf)
                    return Result.Fail<Dictionary<string, string>>($"{Msg("AccountNotLeaf")}: {line.AccountCode}", ErrorCode.ValidationFailed);

                if (!account.IsActive)
                    return Result.Fail<Dictionary<string, string>>($"{Msg("AccountInactive")}: {line.AccountCode}", ErrorCode.ValidationFailed);

                resolved[line.AccountCode] = account.Name;
            }

            return Result.Ok(resolved);
        }

        private (string Error, ErrorCode Code) ValidatePostable(JournalEntry entry)
        {
            if (entry == null) return ("القيد غير موجود", ErrorCode.NotFound);
            if (entry.IsPosted) return ("القيد مرحّل بالفعل", ErrorCode.ValidationFailed);

            if (entry.TotalDebit != entry.TotalCredit)
                return ($"{Msg("NotBalanced")} ({entry.TotalDebit - entry.TotalCredit:N2})", ErrorCode.ValidationFailed);

            if (!_fiscalPeriods.IsOpen(ParseDate(entry.EntryDate)))
                return (Msg("PeriodClosed"), ErrorCode.ValidationFailed);

            foreach (var line in _journal.GetLines(entry.Id))
            {
                var account = _accounts.GetByCode(line.AccountCode);
                if (!account.IsSuccess) return ($"{Msg("AccountNotFound")}: {line.AccountCode}", ErrorCode.ValidationFailed);
                if (!account.Value.IsLeaf) return ($"{Msg("AccountNotLeaf")}: {line.AccountCode}", ErrorCode.ValidationFailed);
                if (!account.Value.IsActive) return ($"{Msg("AccountInactive")}: {line.AccountCode}", ErrorCode.ValidationFailed);
            }

            return (null, ErrorCode.None);
        }

        private int InsertEntryWithLines(DbConnection conn, DbTransaction tx, string entryNo, CreateJournalDto dto, Dictionary<string, string> accountNames)
        {
            var entry = new JournalEntry
            {
                EntryNo     = entryNo,
                EntryDate   = dto.EntryDate.ToString("yyyy-MM-dd"),
                Description = dto.Description,
                Source      = NormalizeSource(dto.Source),
                CreatedBy   = CurrentUser
            };
            var id = _journal.InsertHeader(conn, tx, entry);

            int lineNo = 1;
            foreach (var l in dto.Lines)
                _journal.InsertLine(conn, tx, id, lineNo++, new JournalLine
                {
                    AccountCode = l.AccountCode,
                    AccountName = accountNames.GetValueOrDefault(l.AccountCode),
                    Debit = l.Debit, Credit = l.Credit, Notes = l.Notes
                });

            _journal.UpdateTotals(conn, tx, id, dto.Lines.Sum(x => x.Debit), dto.Lines.Sum(x => x.Credit));
            return id;
        }

        /// <summary>
        /// يبني JournalEntryDto من بيانات لحظة الإنشاء مباشرة — بلا إعادة قراءة من DB (خطر deadlock داخل معاملة
        /// (conn,tx) قائمة — راجع تعليق الصنف). دقيق بما يكفي؛ CreatedAt هنا تقريب لحظي لا القيمة المخزَّنة فعلياً.
        /// FiscalPeriodName عمداً null هنا (لا _fiscalPeriods.GetPeriodFor — تفتح اتصالاً جديداً، تُعلِّق نفس
        /// deadlock لو استُدعيت من Create(conn,tx,...) داخل معاملة FiscalPeriodService.CloseYear الخارجية،
        /// وهذه بالتحديد فترة تُقفَل للتو فمعرفة اسمها هنا فائدته محدودة أصلاً)؛ عرض القيد لاحقاً عبر GetById/
        /// GetPaged يحسبها بأمان (خارج أي معاملة).
        /// </summary>
        private JournalEntryDto BuildDto(int id, string entryNo, CreateJournalDto dto)
        {
            var source = NormalizeSource(dto.Source);

            return new JournalEntryDto
            {
                Id = id,
                EntryNo = entryNo,
                EntryDate = dto.EntryDate,
                Description = dto.Description,
                TotalDebit = dto.Lines.Sum(l => l.Debit),
                TotalCredit = dto.Lines.Sum(l => l.Credit),
                Source = source,
                SourceText = SourceText(source),
                IsPosted = false,
                StatusVariant = StatusVariant.Warning,
                StatusText = Msg("Status.Draft"),
                PostedAt = null,
                PostedBy = null,
                CreatedAt = DateTime.Now,
                CreatedBy = CurrentUser,
                LinesCount = dto.Lines.Count,
                FiscalPeriodName = null,
                CanEdit = true,
                CanDelete = true,
                CanPost = Can("Post"),
                CanUnpost = false
            };
        }

        private JournalEntryDto ToDto(JournalEntry e, int linesCount)
        {
            var isBalanced = e.TotalDebit == e.TotalCredit;
            var variant = e.IsPosted ? StatusVariant.Success : (isBalanced ? StatusVariant.Warning : StatusVariant.Danger);
            var statusKey = e.IsPosted ? "Posted" : (isBalanced ? "Draft" : "Unbalanced");
            var isClosingEntry = e.Source == ClosingEntrySource;
            var entryDate = ParseDate(e.EntryDate);

            return new JournalEntryDto
            {
                Id = e.Id,
                EntryNo = e.EntryNo,
                EntryDate = entryDate,
                Description = e.Description,
                TotalDebit = e.TotalDebit,
                TotalCredit = e.TotalCredit,
                Source = e.Source,
                SourceText = SourceText(e.Source),
                IsPosted = e.IsPosted,
                StatusVariant = variant,
                StatusText = Msg($"Status.{statusKey}"),
                PostedAt = e.PostedAt,
                PostedBy = e.PostedBy,
                CreatedAt = e.CreatedAt,
                CreatedBy = e.CreatedBy,
                LinesCount = linesCount,
                FiscalPeriodName = _fiscalPeriods.GetPeriodFor(entryDate) is { IsSuccess: true } periodResult ? periodResult.Value.Name : null,
                CanEdit   = Can("Edit")   && !e.IsPosted,
                CanDelete = Can("Delete") && !e.IsPosted,
                CanPost   = Can("Post")   && !e.IsPosted && isBalanced,
                CanUnpost = Can("Unpost") && e.IsPosted && !isClosingEntry
            };
        }

        private JournalEntryDetailDto ToDetailDto(JournalEntry e, List<JournalLine> lines)
        {
            var dto = ToDto(e, lines.Count);
            return new JournalEntryDetailDto
            {
                Id = dto.Id, EntryNo = dto.EntryNo, EntryDate = dto.EntryDate, Description = dto.Description,
                TotalDebit = dto.TotalDebit, TotalCredit = dto.TotalCredit, Source = dto.Source, SourceText = dto.SourceText,
                IsPosted = dto.IsPosted, StatusVariant = dto.StatusVariant, StatusText = dto.StatusText,
                PostedAt = dto.PostedAt, PostedBy = dto.PostedBy, CreatedAt = dto.CreatedAt, CreatedBy = dto.CreatedBy,
                LinesCount = dto.LinesCount, FiscalPeriodName = dto.FiscalPeriodName,
                CanEdit = dto.CanEdit, CanDelete = dto.CanDelete, CanPost = dto.CanPost, CanUnpost = dto.CanUnpost,
                Lines = lines.Select(ToLineDto).ToList()
            };
        }

        /// <summary>N+1 مقبول لحجم سطور قيد نموذجي (عشرات لا آلاف) — نفس التنازل المُوثَّق في AccountDto.HasTransactions (F.2.1).</summary>
        private JournalLineDto ToLineDto(JournalLine l)
        {
            var account = _accounts.GetByCode(l.AccountCode);
            return new JournalLineDto
            {
                Id = l.Id,
                LineNo = l.LineNo,
                AccountId = account.IsSuccess ? account.Value.Id : null,
                AccountCode = l.AccountCode,
                AccountName = l.AccountName ?? account.Value?.Name,
                AccountType = account.IsSuccess ? account.Value.Type : default,
                Debit = l.Debit,
                Credit = l.Credit,
                Notes = l.Notes
            };
        }

        private static string NormalizeSource(string source) => string.IsNullOrWhiteSpace(source) ? "Manual" : source;

        // القيد المولَّد من فاتورة/سند/شيك يُعدَّل من مستنده لا من شاشة القيود — وإلا انفصل القيد عن مصدره
        // وصار الرقمان مختلفين بلا أثر يشرح لماذا.
        private static readonly string[] ManualSources = { "Manual", "يدوي", "" };

        private static Result EnsureManualSource(string source, string action) =>
            ManualSources.Contains(source ?? "")
                ? Result.Ok()
                : Result.Fail($"لا يمكن {action} قيد مصدره «{source}» من شاشة القيود — تمّ من المستند نفسه", ErrorCode.ValidationFailed);

        private string SourceText(string source) => Msg($"Source.{NormalizeSource(source)}");

        private static DateTime ParseDate(string date) => DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
