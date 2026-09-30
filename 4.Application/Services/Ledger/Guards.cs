using PrimeERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>حرّاس الشجرة والنقدية</summary>
    public sealed class Guards
    {
        private readonly IAccountRepository _accounts;
        private readonly IJournalRepository _journal;
        private readonly ISettingsProvider _settings;

        public Guards(IAccountRepository accounts, IJournalRepository journal, ISettingsProvider settings)
        {
            _accounts = accounts;
            _journal = journal;
            _settings = settings;
        }

        public bool HasChildren(string code, PrimeDbContext db = null) => _accounts.HasChildren(code, db);

        public bool HasEntries(string code, int? exceptEntryId = null, PrimeDbContext db = null) =>
            _journal.HasLinesForAccount(code, exceptEntryId, db);

        public bool HasEntries(IEnumerable<string> codes, int? exceptEntryId = null)
        {
            var list = codes.Where(code => !string.IsNullOrWhiteSpace(code)).ToList();
            return exceptEntryId == null
                ? _journal.AccountsWithLines(list).Count > 0
                : list.Any(code => _journal.HasLinesForAccount(code, exceptEntryId));
        }

        /// <summary>سبب رفض الحساب للقيود</summary>
        public static string EntryRefusal(Account account) =>
            account == null ? "Str.Journal.AccountNotFoundCode"
            : !account.IsLeaf ? "Str.Journal.AccountIsGroup"
            : !account.IsActive ? "Str.Journal.AccountInactiveCode"
            : null;

        /// <summary>حسابٌ في إعدادات النظام</summary>
        public bool IsSystem(string code) => _settings.GetSection("Accounts").Values.Contains(code);

        /// <summary>حسابٌ تديره صفحة كيانه</summary>
        public bool IsManaged(string code) =>
            !string.IsNullOrWhiteSpace(code) &&
            new[] { AssetRoot(), _settings.Get(SettingKeys.Accounts.Inventory, "") }
                .Any(root => !string.IsNullOrWhiteSpace(root) && code.StartsWith(root, StringComparison.Ordinal));

        private string AssetRoot()
        {
            var cost = _settings.Get(SettingKeys.Accounts.FixedAssets, "");
            if (string.IsNullOrWhiteSpace(cost)) return null;
            var parent = _accounts.GetByCode(cost)?.ParentCode;
            return string.IsNullOrWhiteSpace(parent) ? cost : parent;
        }

        /// <summary>النقدية لا تنزل تحت الصفر</summary>
        public Result CashStaysPositive(IEnumerable<(string Code, decimal Delta)> effects, PrimeDbContext db = null)
        {
            var roots = new[] { _settings.Get(SettingKeys.Accounts.Cash, ""), _settings.Get(SettingKeys.Accounts.Bank, "") }
                .Where(root => !string.IsNullOrWhiteSpace(root)).ToList();

            foreach (var account in effects.GroupBy(e => e.Code))
            {
                var delta = account.Sum(e => e.Delta);
                if (delta >= 0) continue;
                if (!roots.Any(root => (account.Key ?? "").StartsWith(root, StringComparison.Ordinal))) continue;

                var (name, balance) = db == null ? StoredBalance(account.Key) : LiveBalance(account.Key, db);
                if (balance + delta < 0)
                    return Result.Fail(LocalizationService.Get("Str.Journal.CashShort", name), ErrorCode.ValidationFailed);
            }

            return Result.Ok();
        }

        private (string Name, decimal Balance) StoredBalance(string code) =>
            _accounts.GetByCode(code) is { } account ? (account.Name, account.Balance) : (code, decimal.MaxValue);

        private (string Name, decimal Balance) LiveBalance(string code, PrimeDbContext db) =>
            (code, _journal.SumPosted(code, null, null, db));
    }
}
