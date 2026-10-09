using System.Linq;
using System.Collections.Generic;
using PrimeERP.Application.Reporting;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Tree;
using F = PrimeERP.Application.Reporting.FinancialStatementFactory;

namespace PrimeERP.Modules
{
    /// <summary>التقارير الثلاثة عشر</summary>
    public static class ReportRegistrations
    {
        public static void RegisterAll(IModuleRegistry registry)
        {
            Register(registry, "TrialBalance", "Str.Module.TrialBalance",
                typeof(IFinancialStatementService), nameof(IFinancialStatementService.TrialBalance),
                StandardFields.DateRange(), TrialBalanceColumns());

            Register(registry, "CustomerBalances", "Str.Module.CustomerBalances",
                typeof(IPartyReportService), nameof(IPartyReportService.CustomerBalances),
                StandardFields.DateRange(), PartyBalanceColumns(LocalizationService.Get("Str.Reports.Sales"), LocalizationService.Get("Str.Reports.Receipts")));

            Register(registry, "SupplierBalances", "Str.Module.SupplierBalances",
                typeof(IPartyReportService), nameof(IPartyReportService.SupplierBalances),
                StandardFields.DateRange(), PartyBalanceColumns(LocalizationService.Get("Str.Reports.Purchases"), LocalizationService.Get("Str.Reports.Payments")));

            Register(registry, "StockBalances", "Str.Module.StockBalances",
                typeof(IStockReportService), nameof(IStockReportService.Balances),
                StandardFields.DateRange(), StockBalanceColumns());

            Register(registry, "CustomerStatement", "Str.Module.CustomerStatement",
                typeof(IPartyReportService), nameof(IPartyReportService.CustomerStatement),
                Party("CustomerId", "Str.Customer", "Customer"), StatementColumns(),
                arguments: new[] { "CustomerId", "From", "To" });

            Register(registry, "SupplierStatement", "Str.Module.SupplierStatement",
                typeof(IPartyReportService), nameof(IPartyReportService.SupplierStatement),
                Party("SupplierId", "Str.Supplier", "Supplier"), StatementColumns(),
                arguments: new[] { "SupplierId", "From", "To" });

            Register(registry, "AccountStatement", "Str.Module.AccountStatement",
                typeof(IPartyReportService), nameof(IPartyReportService.AccountStatement),
                Account(), StatementColumns(), arguments: new[] { "AccountId", "From", "To" });

            Register(registry, "Payslip", "Str.Module.Payslip",
                typeof(IPayslipReportService), nameof(IPayslipReportService.Payslip),
                new List<ParameterDefinition>
                {
                    new() { Key = "EmployeeId", LabelKey = "Str.Employee", Kind = FieldKind.Picker, PickerType = "Employee" },
                }.Concat(StandardFields.DateRange()).ToList(),
                PayslipColumns(), arguments: new[] { "EmployeeId", "From", "To" }, titleFromTotal: "Employee");

            Register(registry, "ItemCard", "Str.Module.ItemCard",
                typeof(IStockReportService), nameof(IStockReportService.ItemCard),
                new List<ParameterDefinition>
                { new() { Key = "ProductId", LabelKey = "Str.Product", Kind = FieldKind.Picker, PickerType = "Product" } },
                ItemCardColumns(), arguments: new[] { "ProductId" }, titleFromTotal: "Product");

            Register(registry, "IncomeStatement", "Str.Module.IncomeStatement",
                typeof(IFinancialStatementService), nameof(IFinancialStatementService.IncomeStatement),
                StandardFields.DateRange(), FinancialStatementColumns.Build(), statement: true);

            Register(registry, "BalanceSheet", "Str.Module.BalanceSheet",
                typeof(IFinancialStatementService), nameof(IFinancialStatementService.BalanceSheet),
                new List<ParameterDefinition>
                { new() { Key = "AsOf", LabelKey = "Str.AsOfDate", Kind = FieldKind.Date, DefaultValue = System.DateTime.Today } },
                FinancialStatementColumns.Build(), arguments: new[] { "AsOf" }, statement: true);

            Register(registry, "CashFlow", "Str.Module.CashFlow",
                typeof(IFinancialStatementService), nameof(IFinancialStatementService.CashFlow),
                StandardFields.DateRange(), FinancialStatementColumns.Build(), statement: true);

            Register(registry, "StockReport", "Str.Module.StockReport",
                typeof(IStockReportService), nameof(IStockReportService.Movements),
                Warehouse(), StockMovementColumns(), arguments: new[] { "From", "To", "WarehouseId" });

            Register(registry, "SalesReport", "Str.Module.SalesReport",
                typeof(ISalesReportService), nameof(ISalesReportService.Invoices),
                StandardFields.DateRange(), SalesReportColumns());

            Register(registry, "AssetRegister", "Str.Module.AssetRegister",
                typeof(IAssetReportService), nameof(IAssetReportService.Register),
                StandardFields.DateRange().Concat(AssetCategory()).ToList(), AssetRegisterColumns(), arguments: new[] { "From", "To", "CategoryId" });

            Register(registry, "AssetsByCategory", "Str.Module.AssetsByCategory",
                typeof(IAssetReportService), nameof(IAssetReportService.ByCategory),
                new List<ParameterDefinition>(), AssetCategoryColumns(),
                arguments: System.Array.Empty<string>(), statement: true);
        }

        private static void Register(IModuleRegistry registry, string key, string titleKey,
            System.Type serviceType, string method, List<ParameterDefinition> parameters, List<GridColumn> columns,
            string[] arguments = null, bool statement = false, string titleFromTotal = null) =>
            registry.Register(new ModuleDefinition
            {
                Key = key, TitleKey = titleKey, PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = key, TitleKey = titleKey, PermissionKey = "Reports.View",
                    ServiceType = serviceType, Method = method,
                    Arguments = arguments ?? new[] { "From", "To" },
                    Parameters = parameters,
                    Columns = columns,
                    RowKind = statement ? (key == "AssetsByCategory" ? AssetRowKind : F.RowKind) : null,
                    AlternatingRows = !statement,
                    TitleOverrideTotalKey = titleFromTotal
                }
            });

        private static readonly System.Func<object, string> AssetRowKind = row => (row as AssetRegisterRow)?.Kind;


        private static List<ParameterDefinition> Party(string key, string labelKey, string pickerType)
        {
            var list = new List<ParameterDefinition>
            { new() { Key = key, LabelKey = labelKey, Kind = FieldKind.Picker, PickerType = pickerType } };
            list.AddRange(StandardFields.DateRange());
            return list;
        }

        private static List<ParameterDefinition> Account()
        {
            var list = new List<ParameterDefinition>
            { new() { Key = "AccountId", LabelKey = "Str.Account", Kind = FieldKind.Picker, PickerType = "Account", PickerLeafOnly = true } };
            list.AddRange(StandardFields.DateRange());
            return list;
        }

        private static List<ParameterDefinition> AssetCategory() => new()
        {
            new() { Key = "CategoryId", LabelKey = "Str.Asset.Type", Kind = FieldKind.Picker,
                    PickerType = "Category", PickerCategoryModuleKey = "AssetCategories" }
        };

        private static List<ParameterDefinition> Warehouse()
        {
            var list = StandardFields.DateRange();
            list.Add(new() { Key = "WarehouseId", LabelKey = "Str.Warehouse", Kind = FieldKind.Picker, PickerType = "Warehouse" });
            return list;
        }


        private static GridColumn Money(string header, string binding, string group = null) => new()
        {
            Group = group, Header = header, Binding = binding, Width = 120, Align = ColumnAlign.Center, Format = "N2"
        };

        private static List<GridColumn> TrialBalanceColumns()
        {
            var debit = LocalizationService.Get("Str.Debit");
            var credit = LocalizationService.Get("Str.Credit");

            return new()
            {
                new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(TrialBalanceRow.Code), Width = 110 },
                new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(TrialBalanceRow.Name), Width = 240, IsStarWidth = true },
                Money(debit,  nameof(TrialBalanceRow.OpeningDebit),  LocalizationService.Get("Str.Reports.OpeningGroup")),
                Money(credit, nameof(TrialBalanceRow.OpeningCredit), LocalizationService.Get("Str.Reports.OpeningGroup")),
                Money(debit,  nameof(TrialBalanceRow.PeriodDebit),   LocalizationService.Get("Str.Reports.PeriodGroup")),
                Money(credit, nameof(TrialBalanceRow.PeriodCredit),  LocalizationService.Get("Str.Reports.PeriodGroup")),
                Money(debit,  nameof(TrialBalanceRow.ClosingDebit),  LocalizationService.Get("Str.Reports.ClosingGroup")),
                Money(credit, nameof(TrialBalanceRow.ClosingCredit), LocalizationService.Get("Str.Reports.ClosingGroup")),
            };
        }

        private static List<GridColumn> PartyBalanceColumns(string chargeLabel, string settleLabel) => new()
        {
            new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(PartyBalanceRow.Code), Width = 110 },
            new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(PartyBalanceRow.Name), Width = 240, IsStarWidth = true },
            Money(LocalizationService.Get("Str.Reports.OpeningBalance"), nameof(PartyBalanceRow.Opening)),
            Money(chargeLabel,      nameof(PartyBalanceRow.Charged)),
            Money(settleLabel,      nameof(PartyBalanceRow.Settled)),
            Money(LocalizationService.Get("Str.Reports.ClosingBalance"), nameof(PartyBalanceRow.Closing)),
        };

        private static List<GridColumn> StockBalanceColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(StockBalanceRow.ProductCode), Width = 110 },
            new() { Header = LocalizationService.Get("Str.Product"), Binding = nameof(StockBalanceRow.ProductName), Width = 220, IsStarWidth = true },
            new() { Header = LocalizationService.Get("Str.Warehouse"), Binding = nameof(StockBalanceRow.WarehouseName), Width = 150 },
            Money(LocalizationService.Get("Str.Reports.OpeningBalance"), nameof(StockBalanceRow.Opening)),
            Money(LocalizationService.Get("Str.Reports.In"), nameof(StockBalanceRow.In)),
            Money(LocalizationService.Get("Str.Reports.Out"), nameof(StockBalanceRow.Out)),
            Money(LocalizationService.Get("Str.Reports.ClosingBalance"), nameof(StockBalanceRow.Closing)),
        };

        private static List<GridColumn> StatementColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(StatementRow.Date), Width = 100 },
            new() { Header = LocalizationService.Get("Str.EntryNo"), Binding = nameof(StatementRow.EntryNo), Width = 110 },
            new() { Header = LocalizationService.Get("Str.Description"), Binding = nameof(StatementRow.Description), Width = 260, IsStarWidth = true },
            Money(LocalizationService.Get("Str.Debit"),  nameof(StatementRow.Debit)),
            Money(LocalizationService.Get("Str.Credit"), nameof(StatementRow.Credit)),
            Money(LocalizationService.Get("Str.RunningBalance"), nameof(StatementRow.RunningBalance)),
        };

        private static List<GridColumn> PayslipColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.Payroll.No"), Binding = nameof(PayslipRow.PayrollNo), Width = 110 },
            new() { Header = LocalizationService.Get("Str.Period"), Binding = nameof(PayslipRow.Period), Width = 180, IsStarWidth = true },
            Money(LocalizationService.Get("Str.Payroll.Basic"), nameof(PayslipRow.BasicSalary)),
            Money(LocalizationService.Get("Str.Allowances"), nameof(PayslipRow.Allowances)),
            Money(LocalizationService.Get("Str.Payroll.Overtime"), nameof(PayslipRow.Overtime)),
            Money(LocalizationService.Get("Str.Payroll.Gross"), nameof(PayslipRow.Gross)),
            Money(LocalizationService.Get("Str.Payroll.Deductions"), nameof(PayslipRow.Deductions)),
            Money(LocalizationService.Get("Str.Payroll.Advances"), nameof(PayslipRow.Advances)),
            Money(LocalizationService.Get("Str.Payroll.Insurance"), nameof(PayslipRow.Insurance)),
            Money(LocalizationService.Get("Str.Payroll.Tax"), nameof(PayslipRow.Tax)),
            Money(LocalizationService.Get("Str.Deductions"), nameof(PayslipRow.Withheld)),
            Money(LocalizationService.Get("Str.Payroll.Net"), nameof(PayslipRow.NetSalary)),
        };

        private static List<GridColumn> ItemCardColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(ItemCardRow.Date), Width = 95 },
            new() { Header = LocalizationService.Get("Str.SourceDoc"), Binding = nameof(ItemCardRow.SourceDoc), Width = 120 },

            Money(LocalizationService.Get("Str.Reports.InQty"), nameof(ItemCardRow.InQty)),
            Money(LocalizationService.Get("Str.Reports.InPrice"), nameof(ItemCardRow.InPrice)),
            Money(LocalizationService.Get("Str.Reports.InValue"), nameof(ItemCardRow.InValue)),

            Money(LocalizationService.Get("Str.Reports.OutQty"), nameof(ItemCardRow.OutQty)),
            Money(LocalizationService.Get("Str.Reports.OutPrice"), nameof(ItemCardRow.OutPrice)),
            Money(LocalizationService.Get("Str.Reports.OutValue"), nameof(ItemCardRow.OutValue)),

            Money(LocalizationService.Get("Str.Reports.BalanceQty"), nameof(ItemCardRow.BalanceQty)),
            Money(LocalizationService.Get("Str.Reports.BalancePrice"), nameof(ItemCardRow.BalancePrice)),
            Money(LocalizationService.Get("Str.Reports.BalanceValue"), nameof(ItemCardRow.BalanceValue)),
        };

        private static List<GridColumn> StockMovementColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(StockMovementRow.Date), Width = 100 },
            new() { Header = LocalizationService.Get("Str.Product"), Binding = nameof(StockMovementRow.ProductName), Width = 200, IsStarWidth = true },
            new() { Header = LocalizationService.Get("Str.Warehouse"), Binding = nameof(StockMovementRow.WarehouseName), Width = 140 },
            new() { Header = LocalizationService.Get("Str.MovementType"), Binding = nameof(StockMovementRow.MovementType), Width = 90 },
            Money(LocalizationService.Get("Str.Qty"), nameof(StockMovementRow.Qty)),
            Money(LocalizationService.Get("Str.RunningBalance"), nameof(StockMovementRow.BalanceAfter)),
        };

        private static List<GridColumn> AssetRegisterColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.Serial"), Binding = nameof(AssetRegisterRow.No), Width = 50, Align = ColumnAlign.Center },
            new() { Header = LocalizationService.Get("Str.Asset.Type"), Binding = nameof(AssetRegisterRow.CategoryName), Width = 130 },
            new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(AssetRegisterRow.Name), Width = 180, IsStarWidth = true },
            new() { Header = LocalizationService.Get("Str.Supplier"), Binding = nameof(AssetRegisterRow.SupplierName), Width = 140 },
            new() { Header = LocalizationService.Get("Str.Asset.PurchaseDate"), Binding = nameof(AssetRegisterRow.PurchaseDate), Width = 100 },
            Money(LocalizationService.Get("Str.Asset.Cost"), nameof(AssetRegisterRow.PurchaseCost)),
            Money(LocalizationService.Get("Str.Asset.RevaluationAdditions"), nameof(AssetRegisterRow.Additions)),
            Money(LocalizationService.Get("Str.Asset.Reductions"), nameof(AssetRegisterRow.Reductions)),
            new() { Header = LocalizationService.Get("Str.Asset.Rate"), Binding = nameof(AssetRegisterRow.Rate), Width = 80, Align = ColumnAlign.Center, Format = "N2" },
            Money(LocalizationService.Get("Str.Asset.AccumulatedStart"), nameof(AssetRegisterRow.AccumulatedStart)),
            Money(LocalizationService.Get("Str.Asset.Charge"), nameof(AssetRegisterRow.Charge)),
            Money(LocalizationService.Get("Str.Asset.AccumulatedEnd"), nameof(AssetRegisterRow.AccumulatedEnd)),
            Money(LocalizationService.Get("Str.Asset.NetValue"), nameof(AssetRegisterRow.Net)),
        };

        private static List<GridColumn> AssetCategoryColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.Asset.Type"), Binding = nameof(AssetRegisterRow.Name), Width = 300, IsStarWidth = true },
            Money(LocalizationService.Get("Str.Asset.Cost"), nameof(AssetRegisterRow.PurchaseCost)),
            Money(LocalizationService.Get("Str.Asset.Revalued"), nameof(AssetRegisterRow.Revalued)),
            Money(LocalizationService.Get("Str.Asset.Accumulated"), nameof(AssetRegisterRow.Accumulated)),
            Money(LocalizationService.Get("Str.Asset.BookValue"), nameof(AssetRegisterRow.BookValue)),
        };

        private static List<GridColumn> SalesReportColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.InvoiceNo"), Binding = nameof(SalesReportRow.InvoiceNo), Width = 110 },
            new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(SalesReportRow.Date), Width = 100 },
            new() { Header = LocalizationService.Get("Str.Customer"), Binding = nameof(SalesReportRow.PartyName), Width = 220, IsStarWidth = true },
            Money(LocalizationService.Get("Str.NetTotal"), nameof(SalesReportRow.NetTotal)),
        };
    }
}
