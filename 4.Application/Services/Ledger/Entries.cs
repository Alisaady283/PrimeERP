using PrimeERP.Domain.Calculations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>قلب القيد لكل مستدعٍ</summary>
    public sealed class Entries
    {
        public const string ClosingSource = "YearClosing";

        private readonly IJournalRepository _journal;
        private readonly IAccountRepository _accounts;
        private readonly INumberSequenceService _numbers;
        private readonly ISettingsProvider _settings;
        private readonly PeriodGate _periods;
        private readonly Guards _guards;
        private readonly AccountBalances _balances;

        public Entries(IJournalRepository journal, IAccountRepository accounts, INumberSequenceService numbers,
            ISettingsProvider settings, PeriodGate periods, Guards guards, AccountBalances balances)
        {
            _journal = journal;
            _accounts = accounts;
            _numbers = numbers;
            _settings = settings;
            _periods = periods;
            _guards = guards;
            _balances = balances;
        }

        public static string NormalizeSource(string source) => string.IsNullOrWhiteSpace(source) ? "Manual" : source;

        public bool IsOpen(DateTime date) => _periods.IsOpen(date);

        /// <summary>شكل القيد</summary>
        public static readonly Field<JournalEntry>[] Shape =
        {
            new(x => x.EntryDate, "Str.EntryDate", Required: true, Format: FieldFormat.Date),
            new(x => x.Lines, "", Name: "Lines", Must: e => Valid(e).Count >= 2, Message: "Str.Rule.MinJournalLines", Args: _ => new object[] { 2 }),
            new(x => x.Lines, "", Name: "Lines", Must: e => !Valid(e).Any(l => l.Debit > 0 && l.Credit > 0), Message: "Str.Rule.DebitAndCredit",
                Args: e => new object[] { Valid(e).First(l => l.Debit > 0 && l.Credit > 0).AccountCode }),
            new(x => x.Lines, "", Name: "Balance", Must: e => Valid(e).Sum(l => l.Debit) == Valid(e).Sum(l => l.Credit), Message: "Str.Rule.Unbalanced",
                Args: e => new object[] { Valid(e).Sum(l => l.Debit), Valid(e).Sum(l => l.Credit) }),
            new(x => x.Lines, "", Name: "Balance", Must: e => Valid(e).Any(l => l.Debit != 0 || l.Credit != 0), Message: "Str.Rule.ZeroEntry"),
        };

        private static List<JournalLine> Valid(JournalEntry e) => e.Lines.Where(l => !string.IsNullOrEmpty(l.AccountCode)).ToList();

        /// <summary>شكل القيد وأسماء حساباته</summary>
        public Result<Dictionary<string, string>> Validate(List<CreateJournalLineDto> lines, DateTime date, PrimeDbContext db = null)
        {
            var model = new JournalEntry
            {
                EntryDate = date.ToString("yyyy-MM-dd"),
                Lines = lines.Select(l => new JournalLine { AccountCode = l.AccountCode, Debit = l.Debit, Credit = l.Credit }).ToList()
            };
            var result = Check.Fields(model, Shape);
            var names = new Dictionary<string, string>();
            if (result.IsValid) CheckAccounts(result, Valid(model), names, db);

            return result.IsValid
                ? Result.Ok(names)
                : Result.Fail(result.Errors.Values, ErrorCode.ValidationFailed).As<Dictionary<string, string>>();
        }

        /// <summary>حساب السطر ورقيٌّ نشط</summary>
        private void CheckAccounts(ValidationResult result, List<JournalLine> lines, Dictionary<string, string> names, PrimeDbContext db)
        {
            var codes = lines.Select(l => l.AccountCode).Distinct().ToList();
            var found = _accounts.GetByCodes(codes, db).ToDictionary(a => a.Code);
            foreach (var code in codes)
            {
                var account = found.GetValueOrDefault(code);
                if (Guards.EntryRefusal(account) is { } refusal) result.AddError("AccountCode", LocalizationService.Get(refusal, code));
                else names[code] = account.Name;
            }

            if (_settings.Get(SettingKeys.Financial.AllowDuplicateAccountInEntry, false)) return;
            var duplicate = lines.GroupBy(l => l.AccountCode).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null) result.AddError("AccountCode", LocalizationService.Get("Str.Journal.AccountDuplicated", duplicate.Key));
        }

        /// <summary>قيدٌ جديد غير مُرحَّل</summary>
        public Result<(int Id, string EntryNo)> Create(PrimeDbContext db, CreateJournalDto dto)
        {
            if (NormalizeSource(dto.Source) != ClosingSource && !IsOpen(dto.EntryDate)) return Closed().As<(int, string)>();

            var accounts = Validate(dto.Lines, dto.EntryDate, db);
            if (accounts.IsFailure) return accounts.As<(int, string)>();

            var funds = _guards.CashStaysPositive(Effects(dto.Lines, 1), db);
            if (funds.IsFailure) return funds.As<(int, string)>();

            var entryNo = _numbers.Next(db, _settings.Get(SettingKeys.Documents.JournalPrefix, "JE"));
            var id = _journal.InsertHeader(db, new JournalEntry
            {
                EntryNo = entryNo, EntryDate = dto.EntryDate.ToString("yyyy-MM-dd"),
                Description = dto.Description, Source = NormalizeSource(dto.Source)
            });
            WriteLines(db, id, dto.Lines, accounts.Value);
            return Result.Ok((id, entryNo));
        }

        /// <summary>قيدٌ جديد مُرحَّل</summary>
        public Result<(int Id, string EntryNo)> CreatePosted(PrimeDbContext db, CreateJournalDto dto, string user) =>
            Create(db, dto).Then(entry =>
            {
                MarkPosted(db, entry.Id, user);
                return Result.Ok(entry);
            });

        /// <summary>استبدال سطور قيدٍ قائم</summary>
        public void Replace(PrimeDbContext db, CreateJournalDto dto, Dictionary<string, string> accountNames)
        {
            var touched = _journal.GetLines(dto.Id, db).Select(l => l.AccountCode).Concat(dto.Lines.Select(l => l.AccountCode)).Distinct().ToList();

            _journal.DeleteLines(dto.Id, db);
            WriteLines(db, dto.Id, dto.Lines, accountNames);
            _journal.UpdateEntry(db, dto.Id, dto.EntryDate.ToString("yyyy-MM-dd"), dto.Description);
            Recalculate(db, touched);
        }

        public void MarkPosted(PrimeDbContext db, int id, string user)
        {
            _journal.SetPosted(db, id, DateTime.Now, user);
            Recalculate(db, _journal.GetLines(id, db).Select(l => l.AccountCode));
        }

        public void MarkUnposted(PrimeDbContext db, int id)
        {
            _journal.SetUnposted(db, id);
            Recalculate(db, _journal.GetLines(id, db).Select(l => l.AccountCode));
        }

        public void Delete(PrimeDbContext db, int id)
        {
            var codes = _journal.GetLines(id, db).Select(l => l.AccountCode).ToList();
            _journal.DeleteLines(id, db);
            _journal.DeleteHeader(id, db);
            Recalculate(db, codes);
        }

        /// <summary>عكسه مسموح</summary>
        public Result CanRemove(int id)
        {
            var entry = _journal.GetById(id);
            if (entry == null) return Result.Ok();

            return Open(ParseDate(entry.EntryDate))
                .Then(() => entry.IsPosted ? _guards.CashStaysPositive(Effects(_journal.GetLines(id), -1)) : Result.Ok());
        }

        /// <summary>تعديله مسموح، وأسماء حساباته</summary>
        public Result<Dictionary<string, string>> CanUpdate(JournalEntry existing, CreateJournalDto dto, string ownerSource)
        {
            var accounts = Owned(existing.Source, ownerSource, LocalizationService.Get("Str.Action.EditVerb"))
                .Then(() => CanReplace(existing.Id, dto.Lines))
                .Then(() => Validate(dto.Lines, dto.EntryDate));
            if (accounts.IsFailure) return accounts;

            return Open(ParseDate(existing.EntryDate)).Then(() => Open(dto.EntryDate)).Then(() => accounts);
        }

        /// <summary>حذفه مسموح</summary>
        public Result CanDelete(JournalEntry entry, string ownerSource) =>
            Owned(entry.Source, ownerSource, LocalizationService.Get("Str.Action.DeleteVerb")).Then(() => CanRemove(entry.Id));

        /// <summary>ترحيله مسموح</summary>
        public Result CanPost(JournalEntry entry) =>
            Postable(entry).Then(() => _guards.CashStaysPositive(Effects(_journal.GetLines(entry.Id), 1)));

        /// <summary>إلغاء ترحيله مسموح</summary>
        public Result CanUnpost(JournalEntry entry)
        {
            if (!entry.IsPosted) return Result.Fail(Text("NotPostedCannotUnpost"), ErrorCode.ValidationFailed);
            if (entry.Source == ClosingSource) return Result.Fail(Text("ClosingEntryCannotUnpost"), ErrorCode.ValidationFailed);

            return Open(ParseDate(entry.EntryDate))
                .Then(() => _guards.CashStaysPositive(Effects(_journal.GetLines(entry.Id), -1)));
        }

        private Result CanReplace(int id, IEnumerable<CreateJournalLineDto> lines) =>
            _journal.GetById(id)?.IsPosted == true
                ? _guards.CashStaysPositive(Effects(_journal.GetLines(id), -1).Concat(Effects(lines, 1)))
                : Result.Ok();

        /// <summary>مصادر القيد اليدوي</summary>
        private static readonly string[] ManualSources = { "Manual", "يدوي", "" };

        /// <summary>القيد ملك مصدره</summary>
        private static Result Owned(string entrySource, string ownerSource, string action)
        {
            var source = entrySource ?? "";
            var owned = ownerSource == null ? ManualSources.Contains(source) : source == ownerSource;

            return owned
                ? Result.Ok()
                : Result.Fail(LocalizationService.Get("Str.Journal.OwnedBySource", action, entrySource), ErrorCode.ValidationFailed);
        }

        /// <summary>سبب رفض ترحيله إن وُجد</summary>
        private Result Postable(JournalEntry entry)
        {
            if (entry == null) return Result.Fail(Text("NotFound"), ErrorCode.NotFound);
            if (entry.IsPosted) return Result.Fail(Text("AlreadyPosted"), ErrorCode.ValidationFailed);
            if (entry.TotalDebit != entry.TotalCredit)
                return Result.Fail(LocalizationService.Get("Str.Journal.NotBalanced", entry.TotalDebit - entry.TotalCredit), ErrorCode.ValidationFailed);
            var open = Open(ParseDate(entry.EntryDate));
            if (open.IsFailure) return open;

            var lines = _journal.GetLines(entry.Id)
                .Select(l => new CreateJournalLineDto { AccountCode = l.AccountCode, Debit = l.Debit, Credit = l.Credit }).ToList();
            var accounts = Validate(lines, ParseDate(entry.EntryDate));
            return accounts.IsSuccess ? Result.Ok() : accounts;
        }

        public static DateTime ParseDate(string date) => DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        private void WriteLines(PrimeDbContext db, int id, List<CreateJournalLineDto> lines, Dictionary<string, string> accountNames)
        {
            var lineNo = 1;
            foreach (var l in lines)
                _journal.InsertLine(db, id, lineNo++, new JournalLine
                {
                    AccountCode = l.AccountCode, AccountName = accountNames.GetValueOrDefault(l.AccountCode),
                    Debit = l.Debit, Credit = l.Credit, Notes = l.Notes
                });
            _journal.UpdateTotals(db, id, lines.Sum(x => x.Debit), lines.Sum(x => x.Credit));
        }

        private void Recalculate(PrimeDbContext db, IEnumerable<string> codes)
        {
            foreach (var code in codes.Distinct()) _balances.Refresh(db, code);
        }

        private static IEnumerable<(string Code, decimal Delta)> Effects(IEnumerable<JournalLine> lines, int sign) =>
            lines.Select(l => (l.AccountCode, sign * (l.Debit - l.Credit)));

        private static IEnumerable<(string Code, decimal Delta)> Effects(IEnumerable<CreateJournalLineDto> lines, int sign) =>
            lines.Select(l => (l.AccountCode, sign * (l.Debit - l.Credit)));

        private static string Text(string key) => LocalizationService.Get("Str.Journal." + key);

        private Result Open(DateTime date) => IsOpen(date) ? Result.Ok() : Closed();

        private static Result Closed() => Result.Fail(Text("PeriodClosed"), ErrorCode.ValidationFailed);


    }
}
