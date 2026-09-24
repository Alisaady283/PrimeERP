using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Audit;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Reporting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.Application.Services;
using PrimeERP.Platform.Settings;
using F = PrimeERP.Application.Reporting.FinancialStatementFactory;
using PrimeERP.Application.Services.Admin;

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
                    ["Opening"] = $"افتتاحي: {rows.Sum(r => r.OpeningDebit):N2} / {rows.Sum(r => r.OpeningCredit):N2}",
                    ["Period"]  = $"الفترة: {rows.Sum(r => r.PeriodDebit):N2} / {rows.Sum(r => r.PeriodCredit):N2}",
                    ["Closing"] = $"ختامي: {rows.Sum(r => r.ClosingDebit):N2} / {rows.Sum(r => r.ClosingCredit):N2}",
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

            var grossProfit     = F.Sum(sales) - F.Sum(cogs);
            var operatingProfit = grossProfit - F.Sum(operating);
            var netIncome       = operatingProfit + F.Sum(otherIn) - F.Sum(otherOut);

            var rows = F.Group("الإيرادات", sales, "إجمالي الإيرادات")
            .Concat(F.Group("تكلفة البضاعة المباعة", cogs, "إجمالي التكلفة"))
            .Append(F.Grand("مجمل الربح", grossProfit))
            .Concat(F.Group("المصروفات التشغيلية", operating, "إجمالي المصروفات التشغيلية"))
            .Append(F.Grand("الربح التشغيلي", operatingProfit))
            .Concat(F.Group("إيرادات أخرى", otherIn, "إجمالي الإيرادات الأخرى"))
            .Concat(F.Group("مصروفات أخرى", otherOut, "إجمالي المصروفات الأخرى"))
            .Append(F.Grand(LocalizationService.Get("Str.NetIncome"), netIncome))
            .ToList();
            return Ok(rows, new Dictionary<string, string>
            {
            ["Gross"] = $"مجمل الربح: {grossProfit:N2}",
            ["Operating"] = $"الربح التشغيلي: {operatingProfit:N2}",
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

            var rows = new List<F.Line> { F.Heading("الأصول") }
            .Concat(F.Group("الأصول المتداولة", currentAssets, "إجمالي الأصول المتداولة"))
            .Concat(F.Group("الأصول غير المتداولة", nonCurrentAssets, "إجمالي الأصول غير المتداولة"))
            .Append(F.Grand("إجمالي الأصول", assetsTotal))
            .Append(F.Heading("الخصوم وحقوق الملكية"))
            .Concat(F.Group("الخصوم المتداولة", currentLiab, "إجمالي الخصوم المتداولة"))
            .Concat(F.Group("الخصوم طويلة الأجل", longTermLiab, "إجمالي الخصوم طويلة الأجل"))
            .Append(F.Grand("إجمالي الخصوم", liabilitiesTotal, 1))
            .Concat(F.Group("حقوق الملكية", equity, "إجمالي حقوق الملكية"))
            .Append(F.Grand("إجمالي الخصوم وحقوق الملكية", liabilitiesTotal + equityTotal))
            .ToList();
            return Ok(rows, new Dictionary<string, string>
            {
            ["Assets"] = $"إجمالي الأصول: {assetsTotal:N2}",
            ["Sources"] = $"الخصوم وحقوق الملكية: {(liabilitiesTotal + equityTotal):N2}",
            ["Check"] = assetsTotal == liabilitiesTotal + equityTotal
            ? "الميزانية متوازنة"
            : $"فرق غير متوازن: {(assetsTotal - liabilitiesTotal - equityTotal):N2}"
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

            var rows = F.Group("التدفقات النقدية من الأنشطة التشغيلية", operating, "صافي التدفق التشغيلي")
            .Concat(F.Group("التدفقات النقدية من الأنشطة الاستثمارية", investing, "صافي التدفق الاستثماري"))
            .Concat(F.Group("التدفقات النقدية من الأنشطة التمويلية", financing, "صافي التدفق التمويلي"))
            .Append(F.Grand("صافي التغيّر في النقدية", netChange))
            .Append(F.Grand("النقدية أول المدة", openingCash))
            .Append(F.Grand("النقدية آخر المدة", openingCash + netChange))
            .ToList();
            return Ok(rows, new Dictionary<string, string>
            {
            ["Net"] = $"صافي التغيّر: {netChange:N2}",
            ["Closing"] = $"النقدية آخر المدة: {(openingCash + netChange):N2}",
            ["Check"] = Math.Round(openingCash + netChange, 2) == Math.Round(closingCash, 2)
            ? "مطابق لرصيد النقدية"
            : $"فرق عن رصيد النقدية: {(openingCash + netChange - closingCash):N2}"
            });
        }
    }
}
