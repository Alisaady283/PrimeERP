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
    /// <summary>
    /// التقارير الثلاثة عشر — إعلانات خالصة: خدمةٌ ودالةٌ ووسائطها، ثم أعمدة العرض. لا كود هنا: منطق
    /// كل تقرير في خدمته بطبقة التطبيق، وأي حسابٍ يعود إلى هذا الملف كسرٌ يفحصه check.sh.
    /// </summary>
    public static class ReportRegistrations
    {
        public static void RegisterAll(IModuleRegistry registry)
        {
            Register(registry, "TrialBalance", "Str.Module.TrialBalance",
                typeof(IFinancialStatementService), nameof(IFinancialStatementService.TrialBalance),
                StandardFields.DateRange(), TrialBalanceColumns());

            Register(registry, "CustomerBalances", "Str.Module.CustomerBalances",
                typeof(IPartyReportService), nameof(IPartyReportService.CustomerBalances),
                StandardFields.DateRange(), PartyBalanceColumns("المبيعات", "المقبوضات"));

            Register(registry, "SupplierBalances", "Str.Module.SupplierBalances",
                typeof(IPartyReportService), nameof(IPartyReportService.SupplierBalances),
                StandardFields.DateRange(), PartyBalanceColumns("المشتريات", "المدفوعات"));

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
                AssetCategory(), AssetRegisterColumns(), arguments: new[] { "CategoryId" });

            Register(registry, "AssetsByCategory", "Str.Module.AssetsByCategory",
                typeof(IAssetReportService), nameof(IAssetReportService.ByCategory),
                new List<ParameterDefinition>(), AssetCategoryColumns(),
                arguments: System.Array.Empty<string>(), statement: true);
        }

        /// <summary>تسجيل تقرير: وحدةٌ بتخطيط تقرير وتعريفٍ يصف مصدره وأعمدته — بلا سطر خاص بكلٍّ.</summary>
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
                    // القوائم المالية تُقرأ نزولاً: عناوينها ومجاميعها مميَّزة، وبلا تبادل ألوان.
                    RowKind = statement ? (key == "AssetsByCategory" ? AssetRowKind : F.RowKind) : null,
                    AlternatingRows = !statement,
                    TitleOverrideTotalKey = titleFromTotal
                }
            });

        /// <summary>عنوان الفئة ومجموعها يُميَّزان بصرياً كما في القوائم المالية.</summary>
        private static readonly System.Func<object, string> AssetRowKind = row => (row as AssetRegisterRow)?.Kind;

        // ===================== البارامترات المتكرّرة =====================

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

        /// <summary>فئة الأصل — نفس قائمة شاشة الأصول لا قائمةً ثانية.</summary>
        private static List<ParameterDefinition> AssetCategory() => new()
        {
            new() { Key = "CategoryId", LabelKey = "Str.Category", Kind = FieldKind.Picker,
                    PickerType = "Category", PickerCategoryModuleKey = "AssetCategories" }
        };

        private static List<ParameterDefinition> Warehouse()
        {
            var list = StandardFields.DateRange();
            list.Add(new() { Key = "WarehouseId", LabelKey = "Str.Warehouse", Kind = FieldKind.Picker, PickerType = "Warehouse" });
            return list;
        }

        // ===================== الأعمدة =====================

        private static GridColumn Money(string header, string binding, string group = null) => new()
        {
            Group = group, Header = header, Binding = binding, Width = 120, Align = ColumnAlign.Center, Format = "N2"
        };

        /// <summary>ثلاث مجموعات، كلٌّ مدين ودائن، والكود والاسم بلا مجموعة فيمتدّ عنوانهما على صفَّي الرأس.</summary>
        private static List<GridColumn> TrialBalanceColumns()
        {
            var debit = LocalizationService.Get("Str.Debit");
            var credit = LocalizationService.Get("Str.Credit");

            return new()
            {
                new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(TrialBalanceRow.Code), Width = 110 },
                new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(TrialBalanceRow.Name), Width = 240, IsStarWidth = true },
                Money(debit,  nameof(TrialBalanceRow.OpeningDebit),  "الأرصدة الافتتاحية"),
                Money(credit, nameof(TrialBalanceRow.OpeningCredit), "الأرصدة الافتتاحية"),
                Money(debit,  nameof(TrialBalanceRow.PeriodDebit),   "الحركة خلال الفترة"),
                Money(credit, nameof(TrialBalanceRow.PeriodCredit),  "الحركة خلال الفترة"),
                Money(debit,  nameof(TrialBalanceRow.ClosingDebit),  "الأرصدة الختامية"),
                Money(credit, nameof(TrialBalanceRow.ClosingCredit), "الأرصدة الختامية"),
            };
        }

        private static List<GridColumn> PartyBalanceColumns(string chargeLabel, string settleLabel) => new()
        {
            new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(PartyBalanceRow.Code), Width = 110 },
            new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(PartyBalanceRow.Name), Width = 240, IsStarWidth = true },
            Money("رصيد أول المدة", nameof(PartyBalanceRow.Opening)),
            Money(chargeLabel,      nameof(PartyBalanceRow.Charged)),
            Money(settleLabel,      nameof(PartyBalanceRow.Settled)),
            Money("رصيد آخر المدة", nameof(PartyBalanceRow.Closing)),
        };

        private static List<GridColumn> StockBalanceColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(StockBalanceRow.ProductCode), Width = 110 },
            new() { Header = LocalizationService.Get("Str.Product"), Binding = nameof(StockBalanceRow.ProductName), Width = 220, IsStarWidth = true },
            new() { Header = LocalizationService.Get("Str.Warehouse"), Binding = nameof(StockBalanceRow.WarehouseName), Width = 150 },
            Money("رصيد أول المدة", nameof(StockBalanceRow.Opening)),
            Money("الوارد",         nameof(StockBalanceRow.In)),
            Money("المنصرف",        nameof(StockBalanceRow.Out)),
            Money("رصيد آخر المدة", nameof(StockBalanceRow.Closing)),
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

        // ثلاث مجموعات بثلاثة أعمدة: وارد · منصرف · رصيد — كلٌّ بكمية وسعر وقيمة. السعر مشتقّ من
        // القيمة على الكمية، والرصيد بالمتوسط المرجَّح المتحرّك.
        // سطرٌ لكل مسير وبنودُه أعمدة — كسطر الفاتورة: الاستحقاقات ثم الاستقطاعات ثم الصافي.
        private static List<GridColumn> PayslipColumns() => new()
        {
            new() { Header = "المسير", Binding = nameof(PayslipRow.PayrollNo), Width = 110 },
            new() { Header = "الفترة", Binding = nameof(PayslipRow.Period), Width = 180, IsStarWidth = true },
            Money("الأساسي", nameof(PayslipRow.BasicSalary)),
            Money("البدلات", nameof(PayslipRow.Allowances)),
            Money("الإضافي", nameof(PayslipRow.Overtime)),
            Money("الاستحقاقات", nameof(PayslipRow.Gross)),
            Money("الخصومات", nameof(PayslipRow.Deductions)),
            Money("السلف", nameof(PayslipRow.Advances)),
            Money("التأمينات", nameof(PayslipRow.Insurance)),
            Money("الضرائب", nameof(PayslipRow.Tax)),
            Money("الاستقطاعات", nameof(PayslipRow.Withheld)),
            Money("صافي الراتب", nameof(PayslipRow.NetSalary)),
        };

        private static List<GridColumn> ItemCardColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(ItemCardRow.Date), Width = 95 },
            new() { Header = LocalizationService.Get("Str.SourceDoc"), Binding = nameof(ItemCardRow.SourceDoc), Width = 120, IsStarWidth = true },

            Money("وارد: كمية", nameof(ItemCardRow.InQty)),
            Money("وارد: سعر",  nameof(ItemCardRow.InPrice)),
            Money("وارد: قيمة", nameof(ItemCardRow.InValue)),

            Money("منصرف: كمية", nameof(ItemCardRow.OutQty)),
            Money("منصرف: سعر",  nameof(ItemCardRow.OutPrice)),
            Money("منصرف: قيمة", nameof(ItemCardRow.OutValue)),

            Money("الرصيد: كمية", nameof(ItemCardRow.BalanceQty)),
            Money("الرصيد: سعر",  nameof(ItemCardRow.BalancePrice)),
            Money("الرصيد: قيمة", nameof(ItemCardRow.BalanceValue)),
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
            new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(AssetRegisterRow.Code), Width = 100 },
            new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(AssetRegisterRow.Name), Width = 200, IsStarWidth = true },
            new() { Header = LocalizationService.Get("Str.Category"), Binding = nameof(AssetRegisterRow.CategoryName), Width = 140 },
            new() { Header = LocalizationService.Get("Str.Asset.PurchaseDate"), Binding = nameof(AssetRegisterRow.PurchaseDate), Width = 105 },
            Money(LocalizationService.Get("Str.Asset.Cost"), nameof(AssetRegisterRow.PurchaseCost)),
            Money(LocalizationService.Get("Str.Asset.Revalued"), nameof(AssetRegisterRow.Revalued)),
            Money(LocalizationService.Get("Str.Asset.Accumulated"), nameof(AssetRegisterRow.Accumulated)),
            Money(LocalizationService.Get("Str.Asset.BookValue"), nameof(AssetRegisterRow.BookValue)),
        };

        /// <summary>المجمَّع بالفئة: عمودٌ واحد يحمل اسم الفئة أو اسم الأصل، والأرقام الأربعة بجانبه.</summary>
        private static List<GridColumn> AssetCategoryColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.Category"), Binding = nameof(AssetRegisterRow.Name), Width = 300, IsStarWidth = true },
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
