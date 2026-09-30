using PrimeERP.Domain.Calculations;
using PrimeERP.Platform.Localization;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Helpers;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Documents
{
    /// <summary>حسابات البيع والشراء</summary>
    public sealed record TradeAccounts(string Main, string Vat, string Withholding, string Cogs, string Inventory)
    {
        public static Result<TradeAccounts> Sales(ISettingsProvider settings, LineAmounts totals)
        {
            var accounts = new TradeAccounts(
                settings.Get(SettingKeys.Accounts.Sales, ""),
                settings.Get(SettingKeys.Accounts.VATOutput, ""),
                settings.Get(SettingKeys.Accounts.WithholdingReceivable, ""),
                settings.Get(SettingKeys.Accounts.COGS, ""),
                settings.Get(SettingKeys.Accounts.Inventory, ""));

            if (Missing(accounts.Main) || Missing(accounts.Cogs) || Missing(accounts.Inventory))
                return Refused(LocalizationService.Get("Str.Trade.SalesAccountsMissing"));
            if (totals.Vat > 0 && Missing(accounts.Vat))
                return Refused(LocalizationService.Get("Str.Trade.VatOutputMissing"));
            if (totals.Withholding > 0 && Missing(accounts.Withholding))
                return Refused(LocalizationService.Get("Str.Trade.WithholdingReceivableMissing"));

            return Result.Ok(accounts);
        }

        /// <summary>حساب المرتجعات أو المبيعات</summary>
        public static Result<TradeAccounts> SalesReturns(ISettingsProvider settings, LineAmounts totals) =>
            Sales(settings, totals).Then(accounts => Result.Ok(settings.Get(SettingKeys.Accounts.SalesReturns, "") is { Length: > 0 } returns
                ? accounts with { Main = returns }
                : accounts));

        public static Result<TradeAccounts> Purchases(ISettingsProvider settings, LineAmounts totals)
        {
            var inventory = settings.Get(SettingKeys.Accounts.Inventory, "");
            var accounts = new TradeAccounts(
                inventory,
                settings.Get(SettingKeys.Accounts.VATInput, ""),
                settings.Get(SettingKeys.Accounts.WithholdingPayable, ""),
                "",
                inventory);

            if (Missing(accounts.Inventory))
                return Refused(LocalizationService.Get("Str.Trade.InventoryMissing"));
            if (totals.Vat > 0 && Missing(accounts.Vat))
                return Refused(LocalizationService.Get("Str.Trade.VatInputMissing"));
            if (totals.Withholding > 0 && Missing(accounts.Withholding))
                return Refused(LocalizationService.Get("Str.Trade.WithholdingPayableMissing"));

            return Result.Ok(accounts);
        }

        private static bool Missing(string code) => string.IsNullOrWhiteSpace(code);

        private static Result<TradeAccounts> Refused(string message) =>
            Result.Fail<TradeAccounts>(message, ErrorCode.ValidationFailed);

    }
}
