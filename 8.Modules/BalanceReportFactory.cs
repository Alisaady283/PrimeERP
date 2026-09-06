using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Composition.Definitions;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.Modules
{
    /// <summary>
    /// تقارير الأرصدة بالشكل الرسمي: رصيد أول المدة، ثم حركتا الفترة، ثم رصيد آخر المدة. العملاء
    /// والموردون يختلفان في طبيعة الحساب وتسمية الحركتين فقط — فالتقرير واحد بمعاملين لا نسختان.
    /// </summary>
    public static class BalanceReportFactory
    {
        public class PartyBalanceRow
        {
            public string  Code    { get; set; }
            public string  Name    { get; set; }
            public decimal Opening { get; set; }
            public decimal Charged { get; set; }
            public decimal Settled { get; set; }
            public decimal Closing { get; set; }
        }

        /// <summary>الأطراف: كود ← اسم وحساب. المدين طبيعةً للعملاء، والدائن للموردين.</summary>
        public static Result<ReportResult> Build(
            IServiceProvider services, IDictionary<string, object> parameters,
            IReadOnlyList<(string Code, string Name, string AccountCode)> parties,
            bool debitIsCharge, string title, string chargeLabel, string settleLabel)
        {
            var from = (DateTime)(parameters.TryGetValue("From", out var f) && f != null ? f : DateTime.Today.AddMonths(-1));
            var to   = (DateTime)(parameters.TryGetValue("To", out var t) && t != null ? t : DateTime.Today);

            var balance = services.GetRequiredService<IJournalService>().GetTrialBalance(from, to, includeZero: true);
            if (!balance.IsSuccess) return Result.Fail<ReportResult>(balance.ErrorMessage);

            var byAccount = balance.Value.ToDictionary(l => l.Code);

            var rows = new List<PartyBalanceRow>();
            foreach (var (code, name, accountCode) in parties)
            {
                if (string.IsNullOrWhiteSpace(accountCode) || !byAccount.TryGetValue(accountCode, out var line)) continue;

                // الرصيد بإشارة طبيعة الحساب: موجب يعني مديونية الطرف للعملاء، والتزاماً علينا للموردين.
                var sign = debitIsCharge ? 1 : -1;
                var row = new PartyBalanceRow
                {
                    Code = code, Name = name,
                    Opening = sign * (line.OpeningDebit - line.OpeningCredit),
                    Charged = debitIsCharge ? line.PeriodDebit : line.PeriodCredit,
                    Settled = debitIsCharge ? line.PeriodCredit : line.PeriodDebit,
                    Closing = sign * (line.ClosingDebit - line.ClosingCredit),
                };

                if (row.Opening != 0 || row.Charged != 0 || row.Settled != 0 || row.Closing != 0) rows.Add(row);
            }

            return Result.Ok(new ReportResult
            {
                Title = title,
                Columns = Columns(chargeLabel, settleLabel),
                Rows = rows,
                Totals = new()
                {
                    ["Opening"] = $"أول المدة: {rows.Sum(r => r.Opening):N2}",
                    ["Charged"] = $"{chargeLabel}: {rows.Sum(r => r.Charged):N2}",
                    ["Settled"] = $"{settleLabel}: {rows.Sum(r => r.Settled):N2}",
                    ["Closing"] = $"آخر المدة: {rows.Sum(r => r.Closing):N2}",
                }
            });
        }

        public static List<ParameterDefinition> Period() => new()
        {
            new() { Key = "From", LabelKey = "Str.DateFrom", Kind = FieldKind.Date, DefaultValue = DateTime.Today.AddMonths(-1) },
            new() { Key = "To",   LabelKey = "Str.DateTo",   Kind = FieldKind.Date, DefaultValue = DateTime.Today },
        };

        private static List<GridColumn> Columns(string chargeLabel, string settleLabel)
        {
            GridColumn Money(string header, string binding) => new()
            {
                Header = header, Binding = binding, Width = 130, Align = ColumnAlign.Center, Format = "N2"
            };

            return new List<GridColumn>
            {
                new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(PartyBalanceRow.Code), Width = 110 },
                new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(PartyBalanceRow.Name), Width = 240, IsStarWidth = true },
                Money("رصيد أول المدة", nameof(PartyBalanceRow.Opening)),
                Money(chargeLabel,      nameof(PartyBalanceRow.Charged)),
                Money(settleLabel,      nameof(PartyBalanceRow.Settled)),
                Money("رصيد آخر المدة", nameof(PartyBalanceRow.Closing)),
            };
        }
    }
}
