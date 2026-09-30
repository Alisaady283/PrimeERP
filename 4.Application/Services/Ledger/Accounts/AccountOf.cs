using PrimeERP.Domain.Entities.Common;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>حساب الكيان أو الإعداد</summary>
    public sealed class AccountOf
    {
        private readonly ITreasuryRepository _treasuries;
        private readonly PartyByKind _parties;
        private readonly ISettingsProvider _settings;

        public AccountOf(ITreasuryRepository treasuries, PartyByKind parties, ISettingsProvider settings)
        {
            _treasuries = treasuries;
            _parties = parties;
            _settings = settings;
        }

        public Result<string> Treasury(int id, string missingKey) =>
            _treasuries.GetById(id) is { } treasury
                ? Required(treasury.AccountCode, missingKey)
                : Result.Fail<string>(LocalizationService.Get("Str.Treasury.NotFound"), ErrorCode.NotFound);

        /// <summary>حساب الخزينة أو جذر نوعها</summary>
        public string TreasuryOrRoot(int? id)
        {
            var treasury = id == null ? null : _treasuries.GetById(id.Value);
            if (!string.IsNullOrWhiteSpace(treasury?.AccountCode)) return treasury.AccountCode;
            return _settings.Get(treasury?.Kind == TreasuryKind.Cash ? SettingKeys.Accounts.Cash : SettingKeys.Accounts.Bank, "");
        }

        public Result<string> Party(PartyKind kind, int id, string missingKey)
        {
            var party = _parties.Find(kind, id);
            return party == null
                ? Result.Fail<string>(LocalizationService.Get(kind == PartyKind.Customer ? "Str.Customer.NotFound" : "Str.Supplier.NotFound"), ErrorCode.NotFound)
                : Required(party.AccountCode, missingKey);
        }

        public Result<string> Setting(string key, string missingKey) => Required(_settings.Get(key, ""), missingKey);

        private static Result<string> Required(string code, string missingKey) =>
            string.IsNullOrWhiteSpace(code)
                ? Result.Fail<string>(LocalizationService.Get(missingKey), ErrorCode.ValidationFailed)
                : Result.Ok(code);
    }
}
