using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Domain.Calculations;
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

            return AccountOf.Required(accounts.Main, "Str.Trade.SalesAccountsMissing")
                .Then(() => AccountOf.Required(accounts.Cogs, "Str.Trade.SalesAccountsMissing"))
                .Then(() => AccountOf.Required(accounts.Inventory, "Str.Trade.SalesAccountsMissing"))
                .Then(() => totals.Vat > 0 ? AccountOf.Required(accounts.Vat, "Str.Trade.VatOutputMissing") : Result.Ok())
                .Then(() => totals.Withholding > 0 ? AccountOf.Required(accounts.Withholding, "Str.Trade.WithholdingReceivableMissing") : Result.Ok())
                .Then(() => Result.Ok(accounts));
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

            return AccountOf.Required(accounts.Inventory, "Str.Trade.InventoryMissing")
                .Then(() => totals.Vat > 0 ? AccountOf.Required(accounts.Vat, "Str.Trade.VatInputMissing") : Result.Ok())
                .Then(() => totals.Withholding > 0 ? AccountOf.Required(accounts.Withholding, "Str.Trade.WithholdingPayableMissing") : Result.Ok())
                .Then(() => Result.Ok(accounts));
        }
    }
}
