using PrimeERP.Data.Core;
using PrimeERP.Platform.Localization;
using PrimeERP.Application.Services;
using System;
using System.Collections.Generic;
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

namespace PrimeERP.Application.Services.Accounting
{
    /// <summary>المالك الوحيد لمنطق قيود اليومية</summary>
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


        public Result<JournalEntryDto> Create(CreateJournalDto dto)
        {
            if (!Can("Create")) return FailDenied<JournalEntryDto>();

            var accounts = ValidateEntry(dto.Lines, dto.EntryDate);
            if (!accounts.IsSuccess) return Result.Fail<JournalEntryDto>(accounts.ErrorMessage, accounts.ErrorCode);

            if (!_fiscalPeriods.IsOpen(dto.EntryDate))
                return Result.Fail<JournalEntryDto>(Msg("PeriodClosed"), ErrorCode.ValidationFailed);

            var funds = EnsureAffordable(dto.Lines);
            if (funds.IsFailure) return Result.Fail<JournalEntryDto>(funds.ErrorMessage, funds.ErrorCode);

            var prefix = Setting(SettingKeys.Documents.JournalPrefix, "JE");
            var entryNo = _numbers.Next(prefix);

            var newId = Tx(db =>
            {
                var id = InsertEntryWithLines(db, entryNo, dto, accounts.Value);
                Post(db, id);
                return id;
            });

            Audit.Log(EntityName, newId, AuditAction.Insert, details: $"إنشاء قيد {entryNo} — {dto.Lines.Count} سطر");

            return Result.Ok(BuildDto(newId, entryNo, dto));
        }

        public Result<JournalEntryDto> Create(PrimeDbContext db, CreateJournalDto dto)
        {
            var accounts = ValidateEntry(dto.Lines, dto.EntryDate, db);
            if (!accounts.IsSuccess) return Result.Fail<JournalEntryDto>(accounts.ErrorMessage, accounts.ErrorCode);

            var funds = EnsureCashStaysPositive(
                Effects(dto.Lines.Select(l => (l.AccountCode, l.Debit, l.Credit)), 1), db);
            if (funds.IsFailure) return Result.Fail<JournalEntryDto>(funds.ErrorMessage, funds.ErrorCode);

            var prefix = Setting(SettingKeys.Documents.JournalPrefix, "JE");
            var entryNo = _numbers.Next(db, prefix);

            var newId = InsertEntryWithLines(db, entryNo, dto, accounts.Value);
            return Result.Ok(BuildDto(newId, entryNo, dto));
        }


        public Result Update(CreateJournalDto dto) => UpdateOwned(dto, null);

        public Result UpdateOwned(CreateJournalDto dto, string ownerSource)
        {
            if (!Can("Edit")) return FailDenied();

            var existing = _journal.GetById(dto.Id);
            if (existing == null)
                return Result.Fail("القيد غير موجود", ErrorCode.NotFound);

            var editable = EnsureOwnedSource(existing.Source, ownerSource, "تعديل");
            if (editable.IsFailure) return editable;

            var funds = EnsureReplaceable(dto.Id, dto.Lines);
            if (funds.IsFailure) return funds;

            var accounts = ValidateEntry(dto.Lines, dto.EntryDate);
            if (!accounts.IsSuccess) return Result.Fail(accounts.ErrorMessage, accounts.ErrorCode);

            if (!_fiscalPeriods.IsOpen(ParseDate(existing.EntryDate)))
                return Result.Fail(Msg("PeriodClosed"), ErrorCode.ValidationFailed);
            if (!_fiscalPeriods.IsOpen(dto.EntryDate))
                return Result.Fail(Msg("PeriodClosed"), ErrorCode.ValidationFailed);

            var totalDebit = dto.Lines.Sum(l => l.Debit);
            var totalCredit = dto.Lines.Sum(l => l.Credit);

            Tx(db =>
            {
                var touched = _journal.GetLines(dto.Id, db).Select(l => l.AccountCode)
                    .Concat(dto.Lines.Select(l => l.AccountCode)).Distinct().ToList();

                _journal.DeleteLines(dto.Id, db);

                int lineNo = 1;
                foreach (var l in dto.Lines)
                    _journal.InsertLine(db, dto.Id, lineNo++, new JournalLine
                    {
                        AccountCode = l.AccountCode,
                        AccountName = accounts.Value.GetValueOrDefault(l.AccountCode),
                        Debit = l.Debit, Credit = l.Credit, Notes = l.Notes
                    });

                _journal.UpdateEntry(db, dto.Id, dto.EntryDate.ToString("yyyy-MM-dd"), dto.Description);
                _journal.UpdateTotals(db, dto.Id, totalDebit, totalCredit);

                foreach (var code in touched)
                    _accounts.RecalculateBalance(db, code);
            });

            Audit.Log(EntityName, dto.Id, AuditAction.Update, details: $"تعديل قيد {existing.EntryNo}");
            return Result.Ok();
        }

        public Result Delete(int id) => DeleteOwned(id, null);

        public Result DeleteOwned(int id, string ownerSource)
        {
            if (!Can("Delete")) return FailDenied();

            var entry = _journal.GetById(id);
            if (entry == null)
                return Result.Fail("القيد غير موجود", ErrorCode.NotFound);

            var deletable = EnsureOwnedSource(entry.Source, ownerSource, "حذف");
            if (deletable.IsFailure) return deletable;

            if (!_fiscalPeriods.IsOpen(ParseDate(entry.EntryDate)))
                return Result.Fail(Msg("PeriodClosed"), ErrorCode.ValidationFailed);

            var funds = EnsureRemovable(id);
            if (funds.IsFailure) return funds;

            Tx(db => Delete(db, id));

            Audit.Log(EntityName, id, AuditAction.Delete, details: $"حذف قيد {entry.EntryNo}");
            return Result.Ok();
        }

        public Result Delete(PrimeDbContext db, int id)
        {
            var codes = _journal.GetLines(id, db).Select(l => l.AccountCode).Distinct().ToList();

            _journal.DeleteLines(id, db);
            _journal.DeleteHeader(id, db);

            foreach (var code in codes)
                _accounts.RecalculateBalance(db, code);

            return Result.Ok();
        }



        public Result EnsureRemovable(int entryId) =>
            IsPosted(entryId)
                ? EnsureCashStaysPositive(Effects(_journal.GetLines(entryId), -1))
                : Result.Ok();

        private bool IsPosted(int entryId) => _journal.GetById(entryId)?.IsPosted == true;

        public Result EnsureAffordable(IEnumerable<CreateJournalLineDto> lines) =>
            EnsureCashStaysPositive(Effects(lines.Select(l => (l.AccountCode, l.Debit, l.Credit)), 1));

        public Result EnsureReplaceable(int entryId, IEnumerable<CreateJournalLineDto> lines) =>
            IsPosted(entryId)
                ? EnsureCashStaysPositive(
                    Effects(_journal.GetLines(entryId), -1)
                        .Concat(Effects(lines.Select(l => (l.AccountCode, l.Debit, l.Credit)), 1)))
                : Result.Ok();

        private static IEnumerable<(string Code, decimal Delta)> Effects(IEnumerable<JournalLine> lines, int sign) =>
            Effects(lines.Select(l => (l.AccountCode, l.Debit, l.Credit)), sign);

        private static IEnumerable<(string Code, decimal Delta)> Effects(
            IEnumerable<(string Code, decimal Debit, decimal Credit)> lines, int sign) =>
            lines.Select(l => (l.Code, sign * (l.Debit - l.Credit)));

        private Result EnsureCashStaysPositive(IEnumerable<(string Code, decimal Delta)> effects,
            PrimeDbContext db = null)
        {
            var roots = new[] { Setting(SettingKeys.Accounts.Cash, ""), Setting(SettingKeys.Accounts.Bank, "") }
                .Where(root => !string.IsNullOrWhiteSpace(root)).ToList();

            foreach (var account in effects.GroupBy(e => e.Code))
            {
                var delta = account.Sum(e => e.Delta);
                if (delta >= 0) continue;
                if (!roots.Any(root => (account.Key ?? "").StartsWith(root, StringComparison.Ordinal))) continue;

                var (name, balance) = db == null ? StoredBalance(account.Key) : LiveBalance(account.Key, db);

                if (balance + delta < 0)
                    return Result.Fail($"رصيد «{name}» لا يكفي", ErrorCode.ValidationFailed);
            }

            return Result.Ok();
        }

        private (string Name, decimal Balance) StoredBalance(string code)
        {
            var account = _accounts.GetByCode(code);
            return account.IsSuccess ? (account.Value.Name, account.Value.Balance) : (code, decimal.MaxValue);
        }

        private (string Name, decimal Balance) LiveBalance(string code, PrimeDbContext db) =>
            (code, _journal.GetPostedLinesForAccount(code, null, null, db).Sum(l => l.Debit - l.Credit));


        public Result Post(int id)
        {
            if (!Can("Post")) return FailDenied();

            var entry = _journal.GetById(id);
            var (error, code) = ValidatePostable(entry);
            if (error != null) return Result.Fail(error, code);

            var funds = EnsureAffordable(_journal.GetLines(id)
                .Select(l => new CreateJournalLineDto { AccountCode = l.AccountCode, Debit = l.Debit, Credit = l.Credit }));
            if (funds.IsFailure) return funds;

            var result = Tx(db => Post(db, id));
            if (!result.IsSuccess) return result;

            Audit.Log(EntityName, id, AuditAction.Update, details: $"ترحيل قيد {entry.EntryNo}");
            return Result.Ok();
        }

        public Result Post(PrimeDbContext db, int id)
        {
            var lines = _journal.GetLines(id, db);
            _journal.SetPosted(db, id, DateTime.Now, CurrentUser);

            foreach (var code in lines.Select(l => l.AccountCode).Distinct())
                _accounts.RecalculateBalance(db, code);

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

            var result = Tx(db => Unpost(db, id));
            if (!result.IsSuccess) return result;

            Audit.Log(EntityName, id, AuditAction.Update, details: $"إلغاء ترحيل قيد {entry.EntryNo}");
            return Result.Ok();
        }

        public Result Unpost(PrimeDbContext db, int id)
        {
            var lines = _journal.GetLines(id, db);
            _journal.SetUnposted(db, id);

            foreach (var code in lines.Select(l => l.AccountCode).Distinct())
                _accounts.RecalculateBalance(db, code);

            return Result.Ok();
        }

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

            Tx(db =>
            {
                foreach (var id in toPost)
                    Post(db, id);
            });

            batch.SuccessCount = toPost.Count;
            Audit.Log(EntityName, 0, AuditAction.Update, details: $"ترحيل دفعي: {batch.SuccessCount} قيد");

            return Result.Ok(batch);
        }


        public Result<List<TrialBalanceLine>> GetTrialBalance(DateTime from, DateTime to, bool includeZero = false, bool postedOnly = true)
        {
            if (!Can("View")) return FailDenied<List<TrialBalanceLine>>();

            var leaves = _accounts.GetLeaves();
            if (!leaves.IsSuccess)
                return Result.Fail<List<TrialBalanceLine>>(leaves.ErrorMessage, leaves.ErrorCode);

            var all = _accounts.GetPaged(1, 100000);
            var namesByCode = all.IsSuccess
                ? all.Value.Items.GroupBy(a => a.Code).ToDictionary(g => g.Key, g => g.First().Name)
                : new Dictionary<string, string>();

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

                var line = new TrialBalanceLine
                {
                    Code    = account.Code,
                    Name    = account.Name,
                    ParentCode = account.ParentCode,
                    ParentName = account.ParentCode != null && namesByCode.TryGetValue(account.ParentCode, out var parentName)
                                 ? parentName : account.ParentCode,
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


        private const string ClosingEntrySource = "YearClosing";

        /// <summary>شكل القيد وحساباته معاً</summary>
        private Result<Dictionary<string, string>> ValidateEntry(List<CreateJournalLineDto> lines, DateTime entryDate,
            PrimeDbContext db = null)
        {
            var model = new JournalEntry
            {
                EntryDate = entryDate.ToString("yyyy-MM-dd"),
                Lines = lines.Select(l => new JournalLine
                {
                    AccountCode = l.AccountCode, Debit = l.Debit, Credit = l.Credit
                }).ToList()
            };

            var validator = new JournalValidator(_accountRepo,
                Setting(SettingKeys.Financial.AllowDuplicateAccountInEntry, false), db);

            var check = Check(validator, model);
            return check.IsFailure ? check.As<Dictionary<string, string>>() : Result.Ok(validator.AccountNames);
        }

        private (string Error, ErrorCode Code) ValidatePostable(JournalEntry entry)
        {
            if (entry == null) return ("القيد غير موجود", ErrorCode.NotFound);
            if (entry.IsPosted) return ("القيد مرحّل بالفعل", ErrorCode.ValidationFailed);

            if (entry.TotalDebit != entry.TotalCredit)
                return ($"{Msg("NotBalanced")} ({entry.TotalDebit - entry.TotalCredit:N2})", ErrorCode.ValidationFailed);

            if (!_fiscalPeriods.IsOpen(ParseDate(entry.EntryDate)))
                return (Msg("PeriodClosed"), ErrorCode.ValidationFailed);

            var lines = _journal.GetLines(entry.Id);
            var accounts = ValidateEntry(
                lines.Select(l => new CreateJournalLineDto
                {
                    AccountCode = l.AccountCode, Debit = l.Debit, Credit = l.Credit
                }).ToList(),
                ParseDate(entry.EntryDate));

            return accounts.IsSuccess ? (null, ErrorCode.None) : (accounts.ErrorMessage, accounts.ErrorCode);
        }

        private int InsertEntryWithLines(PrimeDbContext db, string entryNo, CreateJournalDto dto, Dictionary<string, string> accountNames)
        {
            var entry = new JournalEntry
            {
                EntryNo     = entryNo,
                EntryDate   = dto.EntryDate.ToString("yyyy-MM-dd"),
                Description = dto.Description,
                Source      = NormalizeSource(dto.Source),
                CreatedBy   = CurrentUser
            };
            var id = _journal.InsertHeader(db, entry);

            int lineNo = 1;
            foreach (var l in dto.Lines)
                _journal.InsertLine(db, id, lineNo++, new JournalLine
                {
                    AccountCode = l.AccountCode,
                    AccountName = accountNames.GetValueOrDefault(l.AccountCode),
                    Debit = l.Debit, Credit = l.Credit, Notes = l.Notes
                });

            _journal.UpdateTotals(db, id, dto.Lines.Sum(x => x.Debit), dto.Lines.Sum(x => x.Credit));
            return id;
        }

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

        private static readonly string[] ManualSources = { "Manual", "يدوي", "" };

        private static Result EnsureOwnedSource(string entrySource, string ownerSource, string action)
        {
            var source = entrySource ?? "";
            var owned = ownerSource == null ? ManualSources.Contains(source) : source == ownerSource;

            return owned
                ? Result.Ok()
                : Result.Fail($"لا يمكن {action} قيد مصدره «{entrySource}» من هنا — تمّ من المستند نفسه", ErrorCode.ValidationFailed);
        }

        private string SourceText(string source) => Msg($"Source.{NormalizeSource(source)}");

        private static DateTime ParseDate(string date) => DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
