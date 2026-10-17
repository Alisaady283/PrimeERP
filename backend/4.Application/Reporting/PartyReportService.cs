using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Audit;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.Parties;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Reporting
{
    /// <summary>أرصدة الأطراف وكشوفها</summary>
    public interface IPartyReportService
    {
        Result<ReportData> CustomerBalances(DateTime from, DateTime to);
        Result<ReportData> SupplierBalances(DateTime from, DateTime to);
        Result<ReportData> CustomerStatement(int customerId, DateTime from, DateTime to);
        Result<ReportData> SupplierStatement(int supplierId, DateTime from, DateTime to);
        Result<ReportData> AccountStatement(int accountId, DateTime from, DateTime to);
    }

    public class PartyReportService : ReportServiceBase, IPartyReportService
    {
        private readonly IJournalService _journal;
        private readonly ICustomerService _customers;
        private readonly ISupplierService _suppliers;
        private readonly IAccountService _accounts;
        private readonly IPartyRepository<Customer> _customerRows;
        private readonly IPartyRepository<Supplier> _supplierRows;

        public PartyReportService(IJournalService journal, ICustomerService customers,
                                   ISupplierService suppliers, IAccountService accounts, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
                                   IPartyRepository<Customer> customerRows, IPartyRepository<Supplier> supplierRows)
        : base(permissions, settings, localization, audit)
        {
            _customerRows = customerRows;
            _supplierRows = supplierRows;
            _journal = journal;
            _customers = customers;
            _suppliers = suppliers;
            _accounts = accounts;
        }

        public Result<ReportData> CustomerBalances(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;

            return Balances(_customerRows.GetAll(activeOnly: false).OrderBy(c => c.Code).Select(c => (c.Code, c.Name, c.AccountCode)).ToList(),
                from, to, debitIsCharge: true, Msg("Sales"), Msg("Receipts"));
        }

        public Result<ReportData> SupplierBalances(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;

            return Balances(_supplierRows.GetAll(activeOnly: false).OrderBy(s => s.Code).Select(s => (s.Code, s.Name, s.AccountCode)).ToList(),
                from, to, debitIsCharge: false, Msg("Purchases"), Msg("Payments"));
        }

        private Result<ReportData> Balances(IReadOnlyList<(string Code, string Name, string AccountCode)> parties,
            DateTime from, DateTime to, bool debitIsCharge, string chargeLabel, string settleLabel)
        {
            var balance = _journal.GetTrialBalance(from, to, includeZero: true);
            if (!balance.IsSuccess) return Result.Fail<ReportData>(balance.ErrorMessage);

            var byAccount = balance.Value.ToDictionary(l => l.Code);

            var rows = new List<PartyBalanceRow>();
            foreach (var (code, name, accountCode) in parties)
            {
                if (string.IsNullOrWhiteSpace(accountCode) || !byAccount.TryGetValue(accountCode, out var line)) continue;

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

            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = new()
                {
                    ["Opening"] = Msg("OpeningIs", rows.Sum(r => r.Opening)),
                    ["Charged"] = $"{chargeLabel}: {rows.Sum(r => r.Charged):N2}",
                    ["Settled"] = $"{settleLabel}: {rows.Sum(r => r.Settled):N2}",
                    ["Closing"] = Msg("ClosingIs", rows.Sum(r => r.Closing)),
                }
            });
        }

        public Result<ReportData> CustomerStatement(int customerId, DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;

            return Statement(customerId, from, to, () => _customers.GetStatement(customerId, from, to));
        }

        public Result<ReportData> SupplierStatement(int supplierId, DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;

            return Statement(supplierId, from, to, () => _suppliers.GetStatement(supplierId, from, to));
        }

        public Result<ReportData> AccountStatement(int accountId, DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;

            if (accountId == 0) return Result.Fail<ReportData>(Msg("PickAccount"));

            var account = _accounts.GetById(accountId);
            if (!account.IsSuccess) return Result.Fail<ReportData>(account.ErrorMessage);

            return Statement(accountId, from, to, () => _accounts.GetStatement(account.Value.Code, from, to));
        }

        private static Result<ReportData> Statement(int id, DateTime from, DateTime to,
            Func<Result<List<AccountStatementLine>>> getStatement)
        {
            if (id == 0) return Result.Fail<ReportData>(LocalizationService.Get("Str.Reports.PickParty"));

            var result = getStatement();
            if (!result.IsSuccess) return Result.Fail<ReportData>(result.ErrorMessage);

            var rows = result.Value.Select(l => new StatementRow
            { Date = l.Date, EntryNo = l.EntryNo, Description = l.Description, Debit = l.Debit, Credit = l.Credit, RunningBalance = l.RunningBalance }).ToList();

            if (rows.Count > 0)
                rows.Add(new StatementRow
                {
                    Date = to.ToString("yyyy-MM-dd"),
                    Description = LocalizationService.Get("Str.Reports.ClosingBalance"),
                    RunningBalance = rows[^1].RunningBalance
                });

            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = new()
                {
                    ["Debit"]   = LocalizationService.Get("Str.Reports.TotalDebitIs", rows.Sum(r => r.Debit)),
                    ["Credit"]  = LocalizationService.Get("Str.Reports.TotalCreditIs", rows.Sum(r => r.Credit)),
                    ["Closing"] = LocalizationService.Get("Str.Reports.BalanceIs", (rows.Count > 0 ? rows[^1].RunningBalance : 0)),
                }
            });
        }
    }
}
