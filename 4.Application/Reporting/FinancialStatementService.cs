using PrimeERP.Domain.Calculations;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Audit;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Reporting;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Data.Repositories;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Settings;
using F = PrimeERP.Application.Reporting.FinancialStatementFactory;

namespace PrimeERP.Application.Reporting
{
    /// <summary>القوائم المالية الثلاث</summary>
    public interface IFinancialStatementService
    {
        Result<ReportData> TrialBalance(DateTime from, DateTime to);
        Result<ReportData> IncomeStatement(DateTime from, DateTime to);
        Result<ReportData> BalanceSheet(DateTime asOf);
        Result<ReportData> CashFlow(DateTime from, DateTime to);
    }

    public class FinancialStatementService : ReportServiceBase, IFinancialStatementService
    {
        private readonly IJournalService _journal;
        private readonly ISettingsService _settingsService;
        private readonly IAccountRepository _accounts;
        private readonly IJournalRepository _journalRows;

        public FinancialStatementService(IJournalService journal, ISettingsService settingsService, IAccountRepository accounts,
            IJournalRepository journalRows, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit)
        : base(permissions, settings, localization, audit)
        {
            _journal = journal;
            _settingsService = settingsService;
            _accounts = accounts;
            _journalRows = journalRows;
        }

        private static Result<ReportData> Ok(List<F.Line> rows, Dictionary<string, string> totals) =>
            Result.Ok(new ReportData { Rows = rows, Totals = totals });

        public Result<ReportData> TrialBalance(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;

            var result = _journal.GetTrialBalance(from, to, includeZero: true);
            if (!result.IsSuccess) return Result.Fail<ReportData>(result.ErrorMessage);

            var rows = result.Value.Select(l => new TrialBalanceRow
            {
                Code = l.Code, Name = l.Name,
                OpeningDebit = l.OpeningDebit, OpeningCredit = l.OpeningCredit,
                PeriodDebit  = l.PeriodDebit,  PeriodCredit  = l.PeriodCredit,
                ClosingDebit = l.ClosingDebit, ClosingCredit = l.ClosingCredit
            }).ToList();

            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = new()
                {
                    ["Opening"] = Localization.Get("Str.Statement.OpeningTotals", rows.Sum(r => r.OpeningDebit), rows.Sum(r => r.OpeningCredit)),
                    ["Period"]  = Localization.Get("Str.Statement.PeriodTotals", rows.Sum(r => r.PeriodDebit), rows.Sum(r => r.PeriodCredit)),
                    ["Closing"] = Localization.Get("Str.Statement.ClosingTotals", rows.Sum(r => r.ClosingDebit), rows.Sum(r => r.ClosingCredit)),
                }
            });
        }

        public Result<ReportData> IncomeStatement(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;

            var result = _journal.GetTrialBalance(from, to, includeZero: true);
            if (!result.IsSuccess) return Result.Fail<ReportData>(result.ErrorMessage);

            var cogsAccount = _settingsService.Get(SettingKeys.Accounts.COGS, "");

            var sales     = F.Period(result.Value, AccountType.Revenue, true,  F.StartsWith("41"));
            var cogs      = string.IsNullOrWhiteSpace(cogsAccount)
                ? new List<F.Line>()
                : F.Period(result.Value, AccountType.Expense, false, F.StartsWith(cogsAccount));
            var otherRevenue = F.Period(result.Value, AccountType.Revenue, true, F.StartsWith("42"));

            var expenses = new List<F.Line>();
            var operatingExpenses = 0m;
            for (var code = 51; code <= 56; code++)
            {
                var prefix = code.ToString();
                var category = F.Period(result.Value, AccountType.Expense, false, F.StartsWith(prefix));
                if (category.Count == 0 || prefix == "51" && cogsAccount == prefix) continue;

                var matches = prefix == "51"
                    ? F.StartsWithBut(prefix, cogsAccount)
                    : F.StartsWith(prefix);
                var accounts = F.Period(result.Value, AccountType.Expense, false, matches);
                var amount = F.Sum(accounts);
                var title = category[0].Statement;

                expenses.Add(new F.Line { Statement = title, Partial = -amount });
                operatingExpenses += amount;
            }

            var incomeTax = F.Period(result.Value, AccountType.Expense, false, F.StartsWith("57"));

            var salesAmount = F.Sum(sales);
            var cogsAmount = F.Sum(cogs);
            var otherRevenueAmount = F.Sum(otherRevenue);
            var incomeTaxAmount = F.Sum(incomeTax);
            var salesTitle = result.Value.FirstOrDefault(account => account.Code == "41")?.Name
                ?? Localization.Get("Str.Revenue");
            var grossProfit = StatementCalc.GrossProfit(salesAmount, cogsAmount);
            var profitBeforeTax = StatementCalc.ProfitBeforeTax(grossProfit, otherRevenueAmount, operatingExpenses);
            var netIncome = StatementCalc.AfterTax(profitBeforeTax, incomeTaxAmount);

            var rows = new List<F.Line>
            {
                F.Grand(salesTitle, salesAmount),
                F.Grand(Localization.Get("Str.Statement.Cogs"), -cogsAmount),
                F.Grand(Localization.Get("Str.Statement.GrossProfit"), grossProfit),
                F.Grand(Localization.Get("Str.Statement.OtherRevenue"), otherRevenueAmount)
            };
            rows.AddRange(expenses);
            rows.Add(F.Grand(Localization.Get("Str.Statement.TotalOperatingExpenses"), -operatingExpenses));
            rows.Add(F.Grand(Localization.Get("Str.Statement.ProfitBeforeTax"), profitBeforeTax));
            rows.Add(F.Grand(Localization.Get("Str.Statement.IncomeTax"), -incomeTaxAmount));
            rows.Add(F.Grand(LocalizationService.Get("Str.NetIncome"), netIncome));

            return Ok(rows, new Dictionary<string, string>
            {
                ["Gross"] = Localization.Get("Str.Statement.GrossProfitIs", grossProfit),
                ["BeforeTax"] = Localization.Get("Str.Statement.ProfitBeforeTaxIs", profitBeforeTax),
                ["Tax"] = Localization.Get("Str.Statement.IncomeTaxIs", incomeTaxAmount),
                ["Net"] = $"{LocalizationService.Get("Str.NetIncome")}: {netIncome:N2}"
            });
        }

        public Result<ReportData> BalanceSheet(DateTime asOf)
        {
            var gate = Gate(); if (gate != null) return gate;

            var result = _journal.GetTrialBalance(new DateTime(1900, 1, 1), asOf, includeZero: true);
            if (!result.IsSuccess) return Result.Fail<ReportData>(result.ErrorMessage);

            var currentAssets    = F.Closing(result.Value, AccountType.Asset, false, F.StartsWith("12"));
            var nonCurrentAssets = F.Closing(result.Value, AccountType.Asset, false, F.StartsWith("11"));
            var currentLiab      = F.Closing(result.Value, AccountType.Liability, true, F.StartsWith("22"));
            var longTermLiab     = F.Closing(result.Value, AccountType.Liability, true, F.StartsWith("21"));
            var equity           = F.Closing(result.Value, AccountType.Equity, true, _ => true);

            var assetsTotal      = F.Sum(currentAssets) + F.Sum(nonCurrentAssets);
            var liabilitiesTotal = F.Sum(currentLiab) + F.Sum(longTermLiab);
            var equityTotal      = F.Sum(equity);

            var rows = new List<F.Line> { F.Heading(Localization.Get("Str.Assets")) }
            .Concat(F.Group(Localization.Get("Str.Statement.CurrentAssets"), currentAssets, Localization.Get("Str.Statement.TotalCurrentAssets")))
            .Concat(F.Group(Localization.Get("Str.Statement.NonCurrentAssets"), nonCurrentAssets, Localization.Get("Str.Statement.TotalNonCurrentAssets")))
            .Append(F.Grand(Localization.Get("Str.Statement.TotalAssets"), assetsTotal))
            .Append(F.Heading(Localization.Get("Str.Statement.LiabilitiesAndEquity")))
            .Concat(F.Group(Localization.Get("Str.Statement.CurrentLiabilities"), currentLiab, Localization.Get("Str.Statement.TotalCurrentLiabilities")))
            .Concat(F.Group(Localization.Get("Str.Statement.LongTermLiabilities"), longTermLiab, Localization.Get("Str.Statement.TotalLongTermLiabilities")))
            .Append(F.Grand(Localization.Get("Str.Statement.TotalLiabilities"), liabilitiesTotal, 1))
            .Concat(F.Group(Localization.Get("Str.Equity"), equity, Localization.Get("Str.Statement.TotalEquity")))
            .Append(F.Grand(Localization.Get("Str.Statement.TotalLiabilitiesAndEquity"), liabilitiesTotal + equityTotal))
            .ToList();
            return Ok(rows, new Dictionary<string, string>
            {
            ["Assets"] = Localization.Get("Str.Statement.TotalAssetsIs", assetsTotal),
            ["Sources"] = Localization.Get("Str.Statement.LiabilitiesAndEquityIs", (liabilitiesTotal + equityTotal)),
            ["Check"] = StatementCalc.BalanceGap(assetsTotal, liabilitiesTotal, equityTotal) == 0
            ? Localization.Get("Str.Statement.Balanced")
            : Localization.Get("Str.Statement.Unbalanced", StatementCalc.BalanceGap(assetsTotal, liabilitiesTotal, equityTotal))
            });
        }

        public Result<ReportData> CashFlow(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;


            var balance = _journal.GetTrialBalance(from, to, includeZero: true);
            if (!balance.IsSuccess) return Result.Fail<ReportData>(balance.ErrorMessage);
            var lines = balance.Value;

            var period = _journalRows.GetAccountSums(from, to, postedOnly: true, exceptSource: Entries.ClosingSource)
                .ToDictionary(s => s.AccountCode, s => s.SumCredit - s.SumDebit);

            string L(string key) => Localization.Get("Str.Statement." + key);
            string Code(string key) => _settingsService.Get(key, "");
            string NameOf(string key) => _accounts.GetByCode(Code(key))?.Name ?? Code(key);
            F.Line Named(string key, decimal amount) => new() { Statement = NameOf(key), Partial = amount };
            F.Line Changed(string label, F.Line line) => new() { Statement = Localization.Get("Str.Statement." + label, line.Statement), Partial = line.Partial };
            decimal Effect(TrialBalanceLine l) => period.GetValueOrDefault(l.Code);
            decimal Of(Func<string, bool> code) => period.Where(p => code(p.Key)).Sum(p => p.Value);

            var isCash         = F.StartsWith("1203", "1204");
            var gains          = F.StartsWith(Code(SettingKeys.Accounts.CapitalGains));
            var losses         = F.StartsWith(Code(SettingKeys.Accounts.CapitalLosses));
            var depreciation   = F.StartsWith(Code(SettingKeys.Accounts.DepreciationExpense));
            var accumulated    = F.StartsWith(Code(SettingKeys.Accounts.AccumulatedDepreciation));
            var financeExpense = F.StartsWith(Code(SettingKeys.Accounts.FinanceExpense));
            var currentAssets  = F.StartsWith("12");
            var currentLiab    = F.StartsWith("22");
            var fixedAssets    = F.StartsWith("11");
            var longTermLiab   = F.StartsWith("21");

            var cash = lines.Where(l => l.IsLeaf && isCash(l.Code)).ToList();
            var openingCash = cash.Sum(l => l.OpeningDebit - l.OpeningCredit);
            var closingCash = cash.Sum(l => l.ClosingDebit - l.ClosingCredit);

            var netIncome = lines.Where(l => l.IsLeaf && l.Type is AccountType.Revenue or AccountType.Expense).Sum(Effect);

            var adjustments = new List<F.Line>
            {
                Named(SettingKeys.Accounts.DepreciationExpense, -Of(depreciation)),
                Named(SettingKeys.Accounts.CapitalGains,        -Of(gains)),
                Named(SettingKeys.Accounts.CapitalLosses,       -Of(losses)),
                Named(SettingKeys.Accounts.FinanceExpense,      -Of(financeExpense)),
            };

            var workingCapital = F.Grouped(lines, AccountType.Asset, code => currentAssets(code) && !isCash(code), Effect, 3)
                .Select(l => Changed("AssetChange", l))
                .Concat(F.Grouped(lines, AccountType.Liability, currentLiab, Effect, 3).Select(l => Changed("LiabilityChange", l)))
                .ToList();

            var investing = F.Grouped(lines, AccountType.Asset, code => fixedAssets(code) && !accumulated(code), Effect, 3)
                .Select(l => Changed("AssetChange", l))
                .Append(Changed("AssetChange", Named(SettingKeys.Accounts.AccumulatedDepreciation, Of(accumulated) + Of(depreciation))))
                .Append(Named(SettingKeys.Accounts.CapitalGains, Of(gains)))
                .Append(Named(SettingKeys.Accounts.CapitalLosses, Of(losses)))
                .ToList();

            var financing = F.Grouped(lines, AccountType.Liability, longTermLiab, Effect, 3)
                .Concat(F.Grouped(lines, AccountType.Equity, _ => true, Effect))
                .Select(l => Changed("LiabilityChange", l))
                .Append(Named(SettingKeys.Accounts.FinanceExpense, Of(financeExpense)))
                .ToList();

            var operating = netIncome + F.Sum(adjustments) + F.Sum(workingCapital);
            var netChange = operating + F.Sum(investing) + F.Sum(financing);

            var rows = new List<F.Line> { F.Heading(L("OperatingCashFlows")), F.Item(L("NetIncomeAfterTax"), netIncome) }
            .Concat(F.Group(L("NonCashAdjustments"), adjustments, L("TotalAdjustments")))
            .Concat(F.Group(L("WorkingCapitalChanges"), workingCapital, L("TotalWorkingCapital")))
            .Append(F.Grand(L("NetOperatingFlow"), operating, 1))
            .Concat(F.Group(L("InvestingCashFlows"), investing, L("NetInvestingFlow"), level: 0))
            .Concat(F.Group(L("FinancingCashFlows"), financing, L("NetFinancingFlow"), level: 0))
            .Append(F.Grand(L("NetCashChange"), netChange))
            .Append(F.Grand(L("OpeningCash"), openingCash))
            .Append(F.Grand(L("ClosingCash"), openingCash + netChange))
            .ToList();
            return Ok(rows, new Dictionary<string, string>
            {
            ["Net"] = Localization.Get("Str.Statement.NetChangeIs", netChange),
            ["Closing"] = Localization.Get("Str.Statement.ClosingCashIs", (openingCash + netChange)),
            ["Check"] = StatementCalc.CashGap(openingCash, netChange, closingCash) == 0
            ? Localization.Get("Str.Statement.CashMatches")
            : Localization.Get("Str.Statement.CashDifference", StatementCalc.CashGap(openingCash, netChange, closingCash))
            });
        }
    }
}
