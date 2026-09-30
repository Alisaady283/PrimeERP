using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Legacy.Admin;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Audit;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Reporting;
using PrimeERP.Application.Legacy.Accounting;
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

        public FinancialStatementService(IJournalService journal, ISettingsService settingsService, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
        : base(permissions, settings, localization, audit)
        {
            _journal = journal;
            _settingsService = settingsService;
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
            var operating = F.Period(result.Value, AccountType.Expense, false, F.StartsWithBut("51", cogsAccount));
            var otherIn   = F.Period(result.Value, AccountType.Revenue, true,  F.StartsWith("42"));
            var otherOut  = F.Period(result.Value, AccountType.Expense, false, F.StartsWithBut("52", cogsAccount));

            var grossProfit     = StatementCalc.GrossProfit(F.Sum(sales), F.Sum(cogs));
            var operatingProfit = StatementCalc.OperatingProfit(grossProfit, F.Sum(operating));
            var netIncome       = StatementCalc.NetIncome(operatingProfit, F.Sum(otherIn), F.Sum(otherOut));

            var rows = F.Group(Localization.Get("Str.Revenue"), sales, Localization.Get("Str.Statement.TotalRevenue"))
            .Concat(F.Group(Localization.Get("Str.Statement.Cogs"), cogs, Localization.Get("Str.Statement.TotalCost")))
            .Append(F.Grand(Localization.Get("Str.Statement.GrossProfit"), grossProfit))
            .Concat(F.Group(Localization.Get("Str.Statement.OperatingExpenses"), operating, Localization.Get("Str.Statement.TotalOperatingExpenses")))
            .Append(F.Grand(Localization.Get("Str.Statement.OperatingProfit"), operatingProfit))
            .Concat(F.Group(Localization.Get("Str.Statement.OtherRevenue"), otherIn, Localization.Get("Str.Statement.TotalOtherRevenue")))
            .Concat(F.Group(Localization.Get("Str.Statement.OtherExpenses"), otherOut, Localization.Get("Str.Statement.TotalOtherExpenses")))
            .Append(F.Grand(LocalizationService.Get("Str.NetIncome"), netIncome))
            .ToList();
            return Ok(rows, new Dictionary<string, string>
            {
            ["Gross"] = Localization.Get("Str.Statement.GrossProfitIs", grossProfit),
            ["Operating"] = Localization.Get("Str.Statement.OperatingProfitIs", operatingProfit),
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
            var currentLiab      = F.Closing(result.Value, AccountType.Liability, true, F.StartsWith("21"));
            var longTermLiab     = F.Closing(result.Value, AccountType.Liability, true, F.StartsWith("22"));
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

            bool IsCash(string code) => F.StartsWith("1203", "1204")(code);

            var cash = balance.Value.Where(l => l.IsLeaf && IsCash(l.Code)).ToList();
            var openingCash = cash.Sum(l => l.OpeningDebit - l.OpeningCredit);
            var closingCash = cash.Sum(l => l.ClosingDebit - l.ClosingCredit);

            var operating = F.Period(balance.Value, AccountType.Revenue, true, _ => true)
            .Concat(F.Period(balance.Value, AccountType.Expense, false, _ => true)
            .Select(l => new F.Line { Statement = l.Statement, Partial = -l.Partial }))
            .Concat(F.Period(balance.Value, AccountType.Asset, false, F.StartsWith("1201", "1202"))
            .Select(l => new F.Line { Statement = l.Statement, Partial = -l.Partial }))
            .Concat(F.Period(balance.Value, AccountType.Liability, true, F.StartsWith("21")))
            .ToList();

            var investing = F.Period(balance.Value, AccountType.Asset, false, F.StartsWith("11"))
            .Select(l => new F.Line { Statement = l.Statement, Partial = -l.Partial }).ToList();

            var financing = F.Period(balance.Value, AccountType.Equity, true, _ => true)
            .Concat(F.Period(balance.Value, AccountType.Liability, true, F.StartsWith("22")))
            .ToList();

            var netChange = F.Sum(operating) + F.Sum(investing) + F.Sum(financing);

            var rows = F.Group(Localization.Get("Str.Statement.OperatingCashFlows"), operating, Localization.Get("Str.Statement.NetOperatingFlow"))
            .Concat(F.Group(Localization.Get("Str.Statement.InvestingCashFlows"), investing, Localization.Get("Str.Statement.NetInvestingFlow")))
            .Concat(F.Group(Localization.Get("Str.Statement.FinancingCashFlows"), financing, Localization.Get("Str.Statement.NetFinancingFlow")))
            .Append(F.Grand(Localization.Get("Str.Statement.NetCashChange"), netChange))
            .Append(F.Grand(Localization.Get("Str.Statement.OpeningCash"), openingCash))
            .Append(F.Grand(Localization.Get("Str.Statement.ClosingCash"), openingCash + netChange))
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
