using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Sales;
using PrimeERP.Platform.Settings;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.Application.Services;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Tree;

using F = PrimeERP.Application.Reporting.FinancialStatementFactory;
using PrimeERP.Application.Reporting;

namespace PrimeERP.Modules
{
    // النمط 4 (تقرير) — كل تقرير: معايير + دالة Generate قصيرة تستدعي خدمة موجودة فعلاً، بلا محرك جديد
    // (راجع ReportRenderer/ReportDefinition في 7.Composition). ثلاثة تقارير أولى تثبت النمط؛ الباقي بنفس
    // القالب حرفياً.
    public static class ReportRegistrations
    {
        /// <summary>ميزان المراجعة القياسي: افتتاحي ثم حركة الفترة ثم ختامي، كلٌّ مدين ودائن.</summary>
        private class TrialBalanceRow
        {
            public string  Code { get; set; }
            public string  Name { get; set; }
            public decimal OpeningDebit  { get; set; }
            public decimal OpeningCredit { get; set; }
            public decimal PeriodDebit   { get; set; }
            public decimal PeriodCredit  { get; set; }
            public decimal ClosingDebit  { get; set; }
            public decimal ClosingCredit { get; set; }
        }
        private class BalanceRow { public string Code { get; set; } public string Name { get; set; } public decimal Balance { get; set; } }
        /// <summary>المخزون بشكل الفترة كالعملاء والموردين: أول المدة، الوارد، المنصرف، آخر المدة.</summary>
        private class StockBalanceRow
        {
            public string  ProductCode   { get; set; }
            public string  ProductName   { get; set; }
            public string  WarehouseName { get; set; }
            public decimal Opening       { get; set; }
            public decimal In            { get; set; }
            public decimal Out           { get; set; }
            public decimal Closing       { get; set; }
        }
        private class StatementRow { public string Date { get; set; } public string EntryNo { get; set; } public string Description { get; set; } public decimal Debit { get; set; } public decimal Credit { get; set; } public decimal RunningBalance { get; set; } }
        private class ItemCardRow { public string Date { get; set; } public string MovementType { get; set; } public decimal Qty { get; set; } public decimal UnitCost { get; set; } public decimal BalanceAfter { get; set; } public string SourceDoc { get; set; } }
        private class FinancialLineRow { public string Code { get; set; } public string Name { get; set; } public decimal Amount { get; set; } }

        /// <summary>أعمدة ميزان المراجعة — ثلاث مجموعات، كلٌّ مدين ودائن، والكود والاسم بلا مجموعة
        /// فيمتدّ عنوانهما على صفَّي الرأس.</summary>
        private static List<GridColumn> TrialBalanceColumns()
        {
            var debit = LocalizationService.Get("Str.Debit");
            var credit = LocalizationService.Get("Str.Credit");

            GridColumn Money(string group, string header, string binding) => new()
            {
                Group = group, Header = header, Binding = binding,
                Width = 120, Align = ColumnAlign.Center, Format = "N2"
            };

            return new List<GridColumn>
            {
                new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(TrialBalanceRow.Code), Width = 110 },
                new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(TrialBalanceRow.Name), Width = 240, IsStarWidth = true },
                Money("الأرصدة الافتتاحية", debit,  nameof(TrialBalanceRow.OpeningDebit)),
                Money("الأرصدة الافتتاحية", credit, nameof(TrialBalanceRow.OpeningCredit)),
                Money("الحركة خلال الفترة", debit,  nameof(TrialBalanceRow.PeriodDebit)),
                Money("الحركة خلال الفترة", credit, nameof(TrialBalanceRow.PeriodCredit)),
                Money("الأرصدة الختامية",  debit,  nameof(TrialBalanceRow.ClosingDebit)),
                Money("الأرصدة الختامية",  credit, nameof(TrialBalanceRow.ClosingCredit)),
            };
        }

        /// <summary>قسم في قائمة مالية: عنوان ثم سطوره ثم مجموعه. القسم الفارغ لا يُعرض إطلاقاً — قائمة
        /// بلا خصوم لا تُظهر عنوان "الخصوم" بصفر، وهو ما يجعل القائمة تُقرأ كقائمة مالية لا كجدول أرصدة.</summary>
        private static IEnumerable<FinancialLineRow> Section(string title, List<FinancialLineRow> lines)
        {
            if (lines.Count == 0) yield break;

            yield return new FinancialLineRow { Name = title };
            foreach (var line in lines) yield return line;

            yield return new FinancialLineRow { Name = $"إجمالي {title}", Amount = lines.Sum(l => l.Amount) };
        }

        private static List<GridColumn> FinancialColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(FinancialLineRow.Code), Width = 100 },
            new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(FinancialLineRow.Name), Width = 300, IsStarWidth = true },
            new() { Header = LocalizationService.Get("Str.Balance"), Binding = nameof(FinancialLineRow.Amount), Width = 140, Align = ColumnAlign.Center, Format = "N2" },
        };
        private class StockMovementRow { public string Date { get; set; } public string ProductCode { get; set; } public string ProductName { get; set; } public string WarehouseName { get; set; } public string MovementType { get; set; } public decimal Qty { get; set; } public decimal BalanceAfter { get; set; } }
        private class SalesReportRow { public string InvoiceNo { get; set; } public string Date { get; set; } public string PartyName { get; set; } public decimal NetTotal { get; set; } }

        public static void RegisterAll(IModuleRegistry registry)
        {
            registry.Register(new ModuleDefinition
            {
                Key = "TrialBalance", TitleKey = "Str.Module.TrialBalance", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "TrialBalance", TitleKey = "Str.Module.TrialBalance", PermissionKey = "Reports.View",
                    Parameters =
                    [
                        ..StandardFields.DateRange(),
                    ],
                    Generate = (services, p) =>
                    {
                        var journal = services.GetRequiredService<IJournalService>();
                        var from = (DateTime)(p["From"] ?? DateTime.Today.AddMonths(-1));
                        var to = (DateTime)(p["To"] ?? DateTime.Today);

                        var result = journal.GetTrialBalance(from, to, includeZero: true);
                        if (!result.IsSuccess) return Result.Fail<ReportResult>(result.ErrorMessage);

                        var rows = result.Value.Select(l => new TrialBalanceRow
                        {
                            Code = l.Code, Name = l.Name,
                            OpeningDebit = l.OpeningDebit, OpeningCredit = l.OpeningCredit,
                            PeriodDebit  = l.PeriodDebit,  PeriodCredit  = l.PeriodCredit,
                            ClosingDebit = l.ClosingDebit, ClosingCredit = l.ClosingCredit
                        }).ToList();

                        return Result.Ok(new ReportResult
                        {
                            Title = LocalizationService.Get("Str.Module.TrialBalance"),
                            Columns = TrialBalanceColumns(),
                            Rows = rows,
                            Totals = new()
                            {
                                ["Opening"] = $"افتتاحي: {rows.Sum(r => r.OpeningDebit):N2} / {rows.Sum(r => r.OpeningCredit):N2}",
                                ["Period"]  = $"الفترة: {rows.Sum(r => r.PeriodDebit):N2} / {rows.Sum(r => r.PeriodCredit):N2}",
                                ["Closing"] = $"ختامي: {rows.Sum(r => r.ClosingDebit):N2} / {rows.Sum(r => r.ClosingCredit):N2}",
                            }
                        });
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "CustomerBalances", TitleKey = "Str.Module.CustomerBalances", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "CustomerBalances", TitleKey = "Str.Module.CustomerBalances", PermissionKey = "Reports.View",
                    Parameters = StandardFields.DateRange(),
                    Generate = (services, p) =>
                    {
                        var result = services.GetRequiredService<ICustomerService>().GetPaged(1, 5000);
                        if (!result.IsSuccess) return Result.Fail<ReportResult>(result.ErrorMessage);

                        return BalanceReportFactory.Build(services, p,
                            result.Value.Items.Select(c => (c.Code, c.Name, c.AccountCode)).ToList(),
                            debitIsCharge: true,
                            LocalizationService.Get("Str.Module.CustomerBalances"), "المبيعات", "المقبوضات");
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "SupplierBalances", TitleKey = "Str.Module.SupplierBalances", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "SupplierBalances", TitleKey = "Str.Module.SupplierBalances", PermissionKey = "Reports.View",
                    Parameters = StandardFields.DateRange(),
                    Generate = (services, p) =>
                    {
                        var result = services.GetRequiredService<ISupplierService>().GetPaged(1, 5000);
                        if (!result.IsSuccess) return Result.Fail<ReportResult>(result.ErrorMessage);

                        return BalanceReportFactory.Build(services, p,
                            result.Value.Items.Select(x => (x.Code, x.Name, x.AccountCode)).ToList(),
                            debitIsCharge: false,
                            LocalizationService.Get("Str.Module.SupplierBalances"), "المشتريات", "المدفوعات");
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "StockBalances", TitleKey = "Str.Module.StockBalances", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "StockBalances", TitleKey = "Str.Module.StockBalances", PermissionKey = "Reports.View",
                    Parameters = StandardFields.DateRange(),
                    Generate = (services, p) =>
                    {
                        var from = (DateTime)(p["From"] ?? DateTime.Today.AddMonths(-1));
                        var to = (DateTime)(p["To"] ?? DateTime.Today);

                        var stock = services.GetRequiredService<IStockService>();
                        var products = services.GetRequiredService<IProductService>().GetPaged(1, 5000);
                        if (!products.IsSuccess) return Result.Fail<ReportResult>(products.ErrorMessage);

                        var warehouses = services.GetRequiredService<IWarehouseService>().GetAll(true);
                        if (!warehouses.IsSuccess) return Result.Fail<ReportResult>(warehouses.ErrorMessage);

                        // كل الحركات منذ البداية: ما قبل الفترة رصيدٌ أول المدة، وما فيها وارد ومنصرف.
                        var history = stock.GetMovements(new DateTime(1900, 1, 1), to, null, int.MaxValue);
                        if (!history.IsSuccess) return Result.Fail<ReportResult>(history.ErrorMessage);

                        var productNames = products.Value.Items.ToDictionary(x => x.Id, x => (x.Code, x.Name));
                        var warehouseNames = warehouses.Value.ToDictionary(w => w.Id, w => w.Name);

                        var rows = history.Value
                            .GroupBy(m => (m.ProductId, m.WarehouseId))
                            .Select(g =>
                            {
                                decimal Signed(Domain.Entities.StockMovement m) =>
                                    m.MovementType == MovementType.Out ? -m.Qty : m.Qty;

                                var before = g.Where(m => m.MovementDate < from).Sum(Signed);
                                var inside = g.Where(m => m.MovementDate >= from && m.MovementDate <= to).ToList();
                                var received = inside.Where(m => Signed(m) > 0).Sum(Signed);
                                var issued = -inside.Where(m => Signed(m) < 0).Sum(Signed);

                                var product = productNames.TryGetValue(g.Key.ProductId, out var pn) ? pn : ("", "");
                                return new StockBalanceRow
                                {
                                    ProductCode = product.Item1,
                                    ProductName = product.Item2,
                                    WarehouseName = warehouseNames.TryGetValue(g.Key.WarehouseId, out var wn) ? wn : "",
                                    Opening = before,
                                    In = received,
                                    Out = issued,
                                    Closing = before + received - issued,
                                };
                            })
                            .Where(r => r.Opening != 0 || r.In != 0 || r.Out != 0 || r.Closing != 0)
                            .OrderBy(r => r.ProductCode)
                            .ToList();

                        GridColumn Qty(string header, string binding) => new()
                        { Header = header, Binding = binding, Width = 120, Align = ColumnAlign.Center, Format = "N2" };

                        return Result.Ok(new ReportResult
                        {
                            Title = LocalizationService.Get("Str.Module.StockBalances"),
                            Columns = new()
                            {
                                new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(StockBalanceRow.ProductCode), Width = 110 },
                                new() { Header = LocalizationService.Get("Str.Product"), Binding = nameof(StockBalanceRow.ProductName), Width = 220, IsStarWidth = true },
                                new() { Header = LocalizationService.Get("Str.Warehouse"), Binding = nameof(StockBalanceRow.WarehouseName), Width = 150 },
                                Qty("رصيد أول المدة", nameof(StockBalanceRow.Opening)),
                                Qty("الوارد", nameof(StockBalanceRow.In)),
                                Qty("المنصرف", nameof(StockBalanceRow.Out)),
                                Qty("رصيد آخر المدة", nameof(StockBalanceRow.Closing)),
                            },
                            Rows = rows,
                            Totals = new()
                            {
                                ["Opening"] = $"أول المدة: {rows.Sum(r => r.Opening):N2}",
                                ["In"] = $"الوارد: {rows.Sum(r => r.In):N2}",
                                ["Out"] = $"المنصرف: {rows.Sum(r => r.Out):N2}",
                                ["Closing"] = $"آخر المدة: {rows.Sum(r => r.Closing):N2}",
                            }
                        });
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "CustomerStatement", TitleKey = "Str.Module.CustomerStatement", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "CustomerStatement", TitleKey = "Str.Module.CustomerStatement", PermissionKey = "Reports.View",
                    Parameters =
                    [
                        new() { Key = "CustomerId", LabelKey = "Str.Customer", Kind = FieldKind.Picker, PickerType = "Customer" },
                        ..StandardFields.DateRange(),
                    ],
                    Generate = (services, p) => BuildStatement(services.GetRequiredService<ICustomerService>().GetStatement,
                        "Str.Module.CustomerStatement", p)
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "SupplierStatement", TitleKey = "Str.Module.SupplierStatement", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "SupplierStatement", TitleKey = "Str.Module.SupplierStatement", PermissionKey = "Reports.View",
                    Parameters =
                    [
                        new() { Key = "SupplierId", LabelKey = "Str.Supplier", Kind = FieldKind.Picker, PickerType = "Supplier" },
                        ..StandardFields.DateRange(),
                    ],
                    Generate = (services, p) => BuildStatement(
                        (id, from, to) => services.GetRequiredService<ISupplierService>().GetStatement(id, from, to),
                        "Str.Module.SupplierStatement", p, "SupplierId")
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "AccountStatement", TitleKey = "Str.Module.AccountStatement", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "AccountStatement", TitleKey = "Str.Module.AccountStatement", PermissionKey = "Reports.View",
                    Parameters =
                    [
                        // الكشف يخصّ حساباً يقبل قيوداً — الحسابات التجميعية لا حركة لها بذاتها.
                        new() { Key = "AccountId", LabelKey = "Str.Account", Kind = FieldKind.Picker, PickerType = "Account", PickerLeafOnly = true },
                        ..StandardFields.DateRange(),
                    ],
                    Generate = (services, p) =>
                    {
                        var accountId = p.TryGetValue("AccountId", out var av) && av is int aid ? aid : 0;
                        if (accountId == 0) return Result.Fail<ReportResult>("اختر حساباً");

                        var accountResult = services.GetRequiredService<IAccountService>().GetById(accountId);
                        if (!accountResult.IsSuccess) return Result.Fail<ReportResult>(accountResult.ErrorMessage);

                        return BuildStatement((_, from, to) => services.GetRequiredService<IAccountService>().GetStatement(accountResult.Value.Code, from, to),
                            "Str.Module.AccountStatement", p, "AccountId");
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "ItemCard", TitleKey = "Str.Module.ItemCard", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "ItemCard", TitleKey = "Str.Module.ItemCard", PermissionKey = "Reports.View",
                    // منتقي المعايير (ReportRenderer) يبني الحقل عبر FieldDefinition العادي — PickerValueField
                    // الافتراضي "Id" (لا "Code" الخاص بمنتقي سطر مستند)، فالقيمة هنا Id رقمي دائماً.
                    Parameters = new() { new() { Key = "ProductId", LabelKey = "Str.Product", Kind = FieldKind.Picker, PickerType = "Product" } },
                    Generate = (services, p) =>
                    {
                        var productId = p.TryGetValue("ProductId", out var pv) && pv is int pid ? pid : 0;
                        if (productId == 0) return Result.Fail<ReportResult>("اختر صنفاً");

                        var productResult = services.GetRequiredService<IProductService>().GetById(productId);
                        if (!productResult.IsSuccess) return Result.Fail<ReportResult>("الصنف غير موجود");
                        var product = productResult.Value;

                        var history = services.GetRequiredService<IStockService>().GetHistory(product.Id, null);
                        if (!history.IsSuccess) return Result.Fail<ReportResult>(history.ErrorMessage);

                        var rows = history.Value.Select(m => new ItemCardRow
                        {
                            Date = m.MovementDate.ToString("yyyy-MM-dd"), MovementType = m.MovementType.ToString(),
                            Qty = m.Qty, UnitCost = m.UnitCost, BalanceAfter = m.BalanceAfter, SourceDoc = m.SourceDocNo
                        }).ToList();

                        return Result.Ok(new ReportResult
                        {
                            Title = $"{LocalizationService.Get("Str.Module.ItemCard")} — {product.Name}",
                            Columns = new()
                            {
                                new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(ItemCardRow.Date), Width = 100 },
                                new() { Header = LocalizationService.Get("Str.MovementType"), Binding = nameof(ItemCardRow.MovementType), Width = 90 },
                                new() { Header = LocalizationService.Get("Str.Qty"), Binding = nameof(ItemCardRow.Qty), Width = 90, Align = ColumnAlign.Center, Format = "N2" },
                                new() { Header = LocalizationService.Get("Str.UnitCost"), Binding = nameof(ItemCardRow.UnitCost), Width = 100, Align = ColumnAlign.Center, Format = "N2" },
                                new() { Header = LocalizationService.Get("Str.RunningBalance"), Binding = nameof(ItemCardRow.BalanceAfter), Width = 110, Align = ColumnAlign.Center, Format = "N2" },
                                new() { Header = LocalizationService.Get("Str.SourceDoc"), Binding = nameof(ItemCardRow.SourceDoc), Width = 140, IsStarWidth = true },
                            },
                            Rows = rows
                        });
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "IncomeStatement", TitleKey = "Str.Module.IncomeStatement", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "IncomeStatement", TitleKey = "Str.Module.IncomeStatement", PermissionKey = "Reports.View",
                    Parameters =
                    [
                        ..StandardFields.DateRange(),
                    ],
                    Generate = (services, p) =>
                    {
                        var statements = services.GetRequiredService<IFinancialStatementService>();
                        var from = (DateTime)(p["From"] ?? DateTime.Today.AddMonths(-1));
                        var to = (DateTime)(p["To"] ?? DateTime.Today);

                        var result = statements.IncomeStatement(from, to);
                        if (!result.IsSuccess) return Result.Fail<ReportResult>(result.ErrorMessage);

                        return Result.Ok(new ReportResult
                        {
                            Title = LocalizationService.Get("Str.Module.IncomeStatement"),
                            Columns = FinancialStatementColumns.Build(),
                            RowKind = F.RowKind,
                            AlternatingRows = false,
                            Rows = result.Value.Rows,
                            Totals = result.Value.Totals
                        });
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "BalanceSheet", TitleKey = "Str.Module.BalanceSheet", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "BalanceSheet", TitleKey = "Str.Module.BalanceSheet", PermissionKey = "Reports.View",
                    Parameters = new() { new() { Key = "AsOf", LabelKey = "Str.AsOfDate", Kind = FieldKind.Date, DefaultValue = DateTime.Today } },
                    Generate = (services, p) =>
                    {
                        var statements = services.GetRequiredService<IFinancialStatementService>();
                        var asOf = (DateTime)(p["AsOf"] ?? DateTime.Today);

                        var result = statements.BalanceSheet(asOf);
                        if (!result.IsSuccess) return Result.Fail<ReportResult>(result.ErrorMessage);

                        return Result.Ok(new ReportResult
                        {
                            Title = LocalizationService.Get("Str.Module.BalanceSheet"),
                            Columns = FinancialStatementColumns.Build(),
                            RowKind = F.RowKind,
                            AlternatingRows = false,
                            Rows = result.Value.Rows,
                            Totals = result.Value.Totals
                        });
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "CashFlow", TitleKey = "Str.Module.CashFlow", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "CashFlow", TitleKey = "Str.Module.CashFlow", PermissionKey = "Reports.View",
                    Parameters =
                    [
                        ..StandardFields.DateRange(),
                    ],
                    // قائمة تدفقات لا كشف صندوق: الحركة مصنَّفة بنشاطها من نوع الحساب المقابل، ثم
                    // النقدية أول المدة + صافي التغيّر = النقدية آخر المدة — وهو ما يجعلها تُطابق الميزانية.
                    Generate = (services, p) =>
                    {
                        var statements = services.GetRequiredService<IFinancialStatementService>();
                        var from = (DateTime)(p["From"] ?? DateTime.Today.AddMonths(-1));
                        var to = (DateTime)(p["To"] ?? DateTime.Today);

                        var result = statements.CashFlow(from, to);
                        if (!result.IsSuccess) return Result.Fail<ReportResult>(result.ErrorMessage);

                        return Result.Ok(new ReportResult
                        {
                            Title = LocalizationService.Get("Str.Module.CashFlow"),
                            Columns = FinancialStatementColumns.Build(),
                            RowKind = F.RowKind,
                            AlternatingRows = false,
                            Rows = result.Value.Rows,
                            Totals = result.Value.Totals
                        });
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "StockReport", TitleKey = "Str.Module.StockReport", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "StockReport", TitleKey = "Str.Module.StockReport", PermissionKey = "Reports.View",
                    Parameters =
                    [
                        ..StandardFields.DateRange(),
                        new() { Key = "WarehouseId", LabelKey = "Str.Warehouse", Kind = FieldKind.Picker, PickerType = "Warehouse" },
                    ],
                    Generate = (services, p) =>
                    {
                        var from = (DateTime)(p["From"] ?? DateTime.Today.AddMonths(-1));
                        var to = (DateTime)(p["To"] ?? DateTime.Today);
                        int? warehouseId = p.TryGetValue("WarehouseId", out var w) && w is int wi ? wi : null;

                        var movements = services.GetRequiredService<IStockService>().GetMovements(from, to, warehouseId);
                        if (!movements.IsSuccess) return Result.Fail<ReportResult>(movements.ErrorMessage);

                        var products = services.GetRequiredService<IProductService>();
                        var warehouses = services.GetRequiredService<IWarehouseService>().GetAll();
                        var warehouseNames = warehouses.IsSuccess ? warehouses.Value.ToDictionary(x => x.Id, x => x.Name) : new();

                        var rows = movements.Value.Select(m =>
                        {
                            var product = products.GetById(m.ProductId);
                            return new StockMovementRow
                            {
                                Date = m.MovementDate.ToString("yyyy-MM-dd"), ProductCode = product.IsSuccess ? product.Value.Code : null,
                                ProductName = product.IsSuccess ? product.Value.Name : null,
                                WarehouseName = warehouseNames.TryGetValue(m.WarehouseId, out var wn) ? wn : "-",
                                MovementType = m.MovementType.ToString(), Qty = m.Qty, BalanceAfter = m.BalanceAfter
                            };
                        }).ToList();

                        return Result.Ok(new ReportResult
                        {
                            Title = LocalizationService.Get("Str.Module.StockReport"),
                            Columns = new()
                            {
                                new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(StockMovementRow.Date), Width = 100 },
                                new() { Header = LocalizationService.Get("Str.Product"), Binding = nameof(StockMovementRow.ProductName), Width = 200, IsStarWidth = true },
                                new() { Header = LocalizationService.Get("Str.Warehouse"), Binding = nameof(StockMovementRow.WarehouseName), Width = 140 },
                                new() { Header = LocalizationService.Get("Str.MovementType"), Binding = nameof(StockMovementRow.MovementType), Width = 90 },
                                new() { Header = LocalizationService.Get("Str.Qty"), Binding = nameof(StockMovementRow.Qty), Width = 90, Align = ColumnAlign.Center, Format = "N2" },
                                new() { Header = LocalizationService.Get("Str.RunningBalance"), Binding = nameof(StockMovementRow.BalanceAfter), Width = 110, Align = ColumnAlign.Center, Format = "N2" },
                            },
                            Rows = rows
                        });
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "SalesReport", TitleKey = "Str.Module.SalesReport", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "SalesReport", TitleKey = "Str.Module.SalesReport", PermissionKey = "Reports.View",
                    Parameters =
                    [
                        ..StandardFields.DateRange(),
                    ],
                    // فلترة التاريخ في الذاكرة — لا معامل تاريخ في SalesInvoiceFilter بعد (نطاق مُبسَّط، حجم
                    // البيانات المتوقَّع في هذه المرحلة صغير).
                    Generate = (services, p) =>
                    {
                        var from = (DateTime)(p["From"] ?? DateTime.Today.AddMonths(-1));
                        var to = (DateTime)(p["To"] ?? DateTime.Today);

                        var result = services.GetRequiredService<ISalesInvoiceService>().GetPaged(1, 5000);
                        if (!result.IsSuccess) return Result.Fail<ReportResult>(result.ErrorMessage);

                        var rows = result.Value.Items.Where(i => i.InvoiceDate.Date >= from.Date && i.InvoiceDate.Date <= to.Date)
                            .Select(i => new SalesReportRow { InvoiceNo = i.InvoiceNo, Date = i.InvoiceDate.ToString("yyyy-MM-dd"), PartyName = i.CustomerName, NetTotal = i.NetTotal })
                            .ToList();

                        return Result.Ok(new ReportResult
                        {
                            Title = LocalizationService.Get("Str.Module.SalesReport"),
                            Columns = new()
                            {
                                new() { Header = LocalizationService.Get("Str.InvoiceNo"), Binding = nameof(SalesReportRow.InvoiceNo), Width = 110 },
                                new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(SalesReportRow.Date), Width = 100 },
                                new() { Header = LocalizationService.Get("Str.Customer"), Binding = nameof(SalesReportRow.PartyName), Width = 220, IsStarWidth = true },
                                new() { Header = LocalizationService.Get("Str.NetTotal"), Binding = nameof(SalesReportRow.NetTotal), Width = 130, Align = ColumnAlign.Center, Format = "N2" },
                            },
                            Rows = rows,
                            Totals = new() { ["Total"] = $"{LocalizationService.Get("Str.Total")}: {rows.Sum(r => r.NetTotal):N2}" }
                        });
                    }
                }
            });
        }

        // بناء تقرير كشف حساب مشترك (عميل/مورد) — التوقيع (id, from, to) موحّد بين ICustomerService.GetStatement
        // وISupplierService.GetStatement.
        private static Result<ReportResult> BuildStatement(Func<int, DateTime, DateTime, Result<List<AccountStatementLine>>> getStatement,
            string titleKey, Dictionary<string, object> p, string idKey = "CustomerId")
        {
            var id = p.TryGetValue(idKey, out var v) && v is int i ? i : 0;
            if (id == 0) return Result.Fail<ReportResult>("اختر عميلاً أو مورداً");

            var from = (DateTime)(p["From"] ?? DateTime.Today.AddMonths(-1));
            var to = (DateTime)(p["To"] ?? DateTime.Today);

            var result = getStatement(id, from, to);
            if (!result.IsSuccess) return Result.Fail<ReportResult>(result.ErrorMessage);

            var rows = result.Value.Select(l => new StatementRow
            { Date = l.Date, EntryNo = l.EntryNo, Description = l.Description, Debit = l.Debit, Credit = l.Credit, RunningBalance = l.RunningBalance }).ToList();

            // رصيد آخر المدة سطراً ختامياً — الكشف بلا خلاصة يُجبر القارئ على تتبّع آخر رصيد جارٍ بعينه.
            if (rows.Count > 0)
                rows.Add(new StatementRow
                {
                    Date = to.ToString("yyyy-MM-dd"),
                    Description = "رصيد آخر المدة",
                    RunningBalance = rows[^1].RunningBalance
                });

            return Result.Ok(new ReportResult { Title = LocalizationService.Get(titleKey), Columns = StatementColumns(), Rows = rows });
        }

        private static List<GridColumn> StatementColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(StatementRow.Date), Width = 100 },
            new() { Header = LocalizationService.Get("Str.InvoiceNo"), Binding = nameof(StatementRow.EntryNo), Width = 100 },
            new() { Header = LocalizationService.Get("Str.Description"), Binding = nameof(StatementRow.Description), Width = 220, IsStarWidth = true },
            new() { Header = LocalizationService.Get("Str.Debit"), Binding = nameof(StatementRow.Debit), Width = 110, Align = ColumnAlign.Center, Format = "N2" },
            new() { Header = LocalizationService.Get("Str.Credit"), Binding = nameof(StatementRow.Credit), Width = 110, Align = ColumnAlign.Center, Format = "N2" },
            new() { Header = LocalizationService.Get("Str.RunningBalance"), Binding = nameof(StatementRow.RunningBalance), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
        };
    }
}
