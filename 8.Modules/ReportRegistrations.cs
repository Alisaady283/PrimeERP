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
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.Modules
{
    // النمط 4 (تقرير) — كل تقرير: معايير + دالة Generate قصيرة تستدعي خدمة موجودة فعلاً، بلا محرك جديد
    // (راجع ReportRenderer/ReportDefinition في 7.Composition). ثلاثة تقارير أولى تثبت النمط؛ الباقي بنفس
    // القالب حرفياً.
    public static class ReportRegistrations
    {
        private class TrialBalanceRow { public string Code { get; set; } public string Name { get; set; } public decimal Debit { get; set; } public decimal Credit { get; set; } }
        private class BalanceRow { public string Code { get; set; } public string Name { get; set; } public decimal Balance { get; set; } }
        private class StockBalanceRow { public string ProductCode { get; set; } public string ProductName { get; set; } public string WarehouseName { get; set; } public decimal Balance { get; set; } }
        private class StatementRow { public string Date { get; set; } public string EntryNo { get; set; } public string Description { get; set; } public decimal Debit { get; set; } public decimal Credit { get; set; } public decimal RunningBalance { get; set; } }
        private class ItemCardRow { public string Date { get; set; } public string MovementType { get; set; } public decimal Qty { get; set; } public decimal UnitCost { get; set; } public decimal BalanceAfter { get; set; } public string SourceDoc { get; set; } }
        private class FinancialLineRow { public string Code { get; set; } public string Name { get; set; } public decimal Amount { get; set; } }

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
                    Parameters = new()
                    {
                        new() { Key = "From", LabelKey = "Str.DateFrom", Kind = FieldKind.Date, DefaultValue = DateTime.Today.AddMonths(-1) },
                        new() { Key = "To", LabelKey = "Str.DateTo", Kind = FieldKind.Date, DefaultValue = DateTime.Today },
                    },
                    Generate = (services, p) =>
                    {
                        var journal = services.GetRequiredService<IJournalService>();
                        var from = (DateTime)(p["From"] ?? DateTime.Today.AddMonths(-1));
                        var to = (DateTime)(p["To"] ?? DateTime.Today);

                        var result = journal.GetTrialBalance(from, to);
                        if (!result.IsSuccess) return Result.Fail<ReportResult>(result.ErrorMessage);

                        var rows = result.Value.Where(l => l.PeriodDebit != 0 || l.PeriodCredit != 0)
                            .Select(l => new TrialBalanceRow { Code = l.Code, Name = l.Name, Debit = l.PeriodDebit, Credit = l.PeriodCredit }).ToList();

                        return Result.Ok(new ReportResult
                        {
                            Title = LocalizationService.Get("Str.Module.TrialBalance"),
                            Columns = new()
                            {
                                new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(TrialBalanceRow.Code), Width = 100 },
                                new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(TrialBalanceRow.Name), Width = 260, IsStarWidth = true },
                                new() { Header = LocalizationService.Get("Str.Debit"), Binding = nameof(TrialBalanceRow.Debit), Width = 130, Align = ColumnAlign.Center, Format = "N2" },
                                new() { Header = LocalizationService.Get("Str.Credit"), Binding = nameof(TrialBalanceRow.Credit), Width = 130, Align = ColumnAlign.Center, Format = "N2" },
                            },
                            Rows = rows,
                            Totals = new() { ["Debit"] = $"{LocalizationService.Get("Str.Debit")}: {rows.Sum(r => r.Debit):N2}", ["Credit"] = $"{LocalizationService.Get("Str.Credit")}: {rows.Sum(r => r.Credit):N2}" }
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
                    Generate = (services, p) =>
                    {
                        var customers = services.GetRequiredService<ICustomerService>();
                        var result = customers.GetPaged(1, 5000);
                        if (!result.IsSuccess) return Result.Fail<ReportResult>(result.ErrorMessage);

                        var rows = result.Value.Items.Where(c => c.Balance != 0)
                            .Select(c => new BalanceRow { Code = c.Code, Name = c.Name, Balance = c.Balance }).ToList();

                        return Result.Ok(new ReportResult
                        {
                            Title = LocalizationService.Get("Str.Module.CustomerBalances"),
                            Columns = new()
                            {
                                new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(BalanceRow.Code), Width = 100 },
                                new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(BalanceRow.Name), Width = 260, IsStarWidth = true },
                                new() { Header = LocalizationService.Get("Str.Balance"), Binding = nameof(BalanceRow.Balance), Width = 130, Align = ColumnAlign.Center, Format = "N2" },
                            },
                            Rows = rows,
                            Totals = new() { ["Balance"] = $"{LocalizationService.Get("Str.Total")}: {rows.Sum(r => r.Balance):N2}" }
                        });
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "SupplierBalances", TitleKey = "Str.Module.SupplierBalances", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "SupplierBalances", TitleKey = "Str.Module.SupplierBalances", PermissionKey = "Reports.View",
                    Generate = (services, p) =>
                    {
                        var suppliers = services.GetRequiredService<ISupplierService>();
                        var result = suppliers.GetPaged(1, 5000);
                        if (!result.IsSuccess) return Result.Fail<ReportResult>(result.ErrorMessage);

                        var rows = result.Value.Items.Where(s => s.Balance != 0)
                            .Select(s => new BalanceRow { Code = s.Code, Name = s.Name, Balance = s.Balance }).ToList();

                        return Result.Ok(new ReportResult
                        {
                            Title = LocalizationService.Get("Str.Module.SupplierBalances"),
                            Columns = new()
                            {
                                new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(BalanceRow.Code), Width = 100 },
                                new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(BalanceRow.Name), Width = 260, IsStarWidth = true },
                                new() { Header = LocalizationService.Get("Str.Balance"), Binding = nameof(BalanceRow.Balance), Width = 130, Align = ColumnAlign.Center, Format = "N2" },
                            },
                            Rows = rows,
                            Totals = new() { ["Balance"] = $"{LocalizationService.Get("Str.Total")}: {rows.Sum(r => r.Balance):N2}" }
                        });
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "StockBalances", TitleKey = "Str.Module.StockBalances", PermissionPrefix = "Reports", LayoutKind = LayoutKind.Report,
                Report = new ReportDefinition
                {
                    Key = "StockBalances", TitleKey = "Str.Module.StockBalances", PermissionKey = "Reports.View",
                    Generate = (services, p) =>
                    {
                        var stock = services.GetRequiredService<IStockService>();
                        var products = services.GetRequiredService<IProductService>();
                        var warehouses = services.GetRequiredService<IWarehouseService>().GetAll();
                        if (!warehouses.IsSuccess) return Result.Fail<ReportResult>(warehouses.ErrorMessage);
                        var warehouseNames = warehouses.Value.ToDictionary(w => w.Id, w => w.Name);

                        var balances = stock.GetAllBalances();
                        if (!balances.IsSuccess) return Result.Fail<ReportResult>(balances.ErrorMessage);

                        var rows = balances.Value.Select(b =>
                        {
                            var product = products.GetById(b.ProductId);
                            return new StockBalanceRow
                            {
                                ProductCode = product.IsSuccess ? product.Value.Code : null, ProductName = product.IsSuccess ? product.Value.Name : null,
                                WarehouseName = warehouseNames.TryGetValue(b.WarehouseId, out var wn) ? wn : "-", Balance = b.Balance
                            };
                        }).ToList();

                        return Result.Ok(new ReportResult
                        {
                            Title = LocalizationService.Get("Str.Module.StockBalances"),
                            Columns = new()
                            {
                                new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(StockBalanceRow.ProductCode), Width = 100 },
                                new() { Header = LocalizationService.Get("Str.Product"), Binding = nameof(StockBalanceRow.ProductName), Width = 220, IsStarWidth = true },
                                new() { Header = LocalizationService.Get("Str.Warehouse"), Binding = nameof(StockBalanceRow.WarehouseName), Width = 160 },
                                new() { Header = LocalizationService.Get("Str.Qty"), Binding = nameof(StockBalanceRow.Balance), Width = 110, Align = ColumnAlign.Center, Format = "N2" },
                            },
                            Rows = rows
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
                    Parameters = new()
                    {
                        new() { Key = "CustomerId", LabelKey = "Str.Customer", Kind = FieldKind.Picker, PickerType = "Customer" },
                        new() { Key = "From", LabelKey = "Str.DateFrom", Kind = FieldKind.Date, DefaultValue = DateTime.Today.AddMonths(-1) },
                        new() { Key = "To", LabelKey = "Str.DateTo", Kind = FieldKind.Date, DefaultValue = DateTime.Today },
                    },
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
                    Parameters = new()
                    {
                        new() { Key = "SupplierId", LabelKey = "Str.Supplier", Kind = FieldKind.Picker, PickerType = "Supplier" },
                        new() { Key = "From", LabelKey = "Str.DateFrom", Kind = FieldKind.Date, DefaultValue = DateTime.Today.AddMonths(-1) },
                        new() { Key = "To", LabelKey = "Str.DateTo", Kind = FieldKind.Date, DefaultValue = DateTime.Today },
                    },
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
                    Parameters = new()
                    {
                        // الكشف يخصّ حساباً يقبل قيوداً — الحسابات التجميعية لا حركة لها بذاتها.
                        new() { Key = "AccountId", LabelKey = "Str.Account", Kind = FieldKind.Picker, PickerType = "Account", PickerLeafOnly = true },
                        new() { Key = "From", LabelKey = "Str.DateFrom", Kind = FieldKind.Date, DefaultValue = DateTime.Today.AddMonths(-1) },
                        new() { Key = "To", LabelKey = "Str.DateTo", Kind = FieldKind.Date, DefaultValue = DateTime.Today },
                    },
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
                    Parameters = new()
                    {
                        new() { Key = "From", LabelKey = "Str.DateFrom", Kind = FieldKind.Date, DefaultValue = DateTime.Today.AddMonths(-1) },
                        new() { Key = "To", LabelKey = "Str.DateTo", Kind = FieldKind.Date, DefaultValue = DateTime.Today },
                    },
                    Generate = (services, p) =>
                    {
                        var journal = services.GetRequiredService<IJournalService>();
                        var from = (DateTime)(p["From"] ?? DateTime.Today.AddMonths(-1));
                        var to = (DateTime)(p["To"] ?? DateTime.Today);

                        var result = journal.GetTrialBalance(from, to);
                        if (!result.IsSuccess) return Result.Fail<ReportResult>(result.ErrorMessage);

                        var revenue = result.Value.Where(l => l.IsLeaf && l.Type == AccountType.Revenue && (l.PeriodDebit != 0 || l.PeriodCredit != 0))
                            .Select(l => new FinancialLineRow { Code = l.Code, Name = l.Name, Amount = l.PeriodCredit - l.PeriodDebit }).ToList();
                        var expense = result.Value.Where(l => l.IsLeaf && l.Type == AccountType.Expense && (l.PeriodDebit != 0 || l.PeriodCredit != 0))
                            .Select(l => new FinancialLineRow { Code = l.Code, Name = l.Name, Amount = l.PeriodDebit - l.PeriodCredit }).ToList();

                        var totalRevenue = revenue.Sum(r => r.Amount);
                        var totalExpense = expense.Sum(r => r.Amount);

                        var rows = Section(LocalizationService.Get("Str.Revenue"), revenue)
                            .Concat(Section(LocalizationService.Get("Str.Expense"), expense))
                            .Append(new FinancialLineRow { Name = LocalizationService.Get("Str.NetIncome"), Amount = totalRevenue - totalExpense })
                            .ToList();

                        return Result.Ok(new ReportResult
                        {
                            Title = LocalizationService.Get("Str.Module.IncomeStatement"),
                            Columns = FinancialColumns(),
                            Rows = rows,
                            Totals = new()
                            {
                                ["Revenue"] = $"{LocalizationService.Get("Str.Revenue")}: {totalRevenue:N2}",
                                ["Expense"] = $"{LocalizationService.Get("Str.Expense")}: {totalExpense:N2}",
                                ["Net"] = $"{LocalizationService.Get("Str.NetIncome")}: {(totalRevenue - totalExpense):N2}"
                            }
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
                        var journal = services.GetRequiredService<IJournalService>();
                        var asOf = (DateTime)(p["AsOf"] ?? DateTime.Today);

                        // DateTime.MinValue كـfrom يُفجِّر حساب "الرصيد الافتتاحي" داخل GetTrialBalance (طرح يوم
                        // منها يفيض حسابياً) — بداية عملية واسعة بما يكفي عملياً بدلاً منها.
                        var result = journal.GetTrialBalance(new DateTime(1900, 1, 1), asOf);
                        if (!result.IsSuccess) return Result.Fail<ReportResult>(result.ErrorMessage);

                        FinancialLineRow ToRow(TrialBalanceLine l) => new() { Code = l.Code, Name = l.Name, Amount = l.ClosingDebit - l.ClosingCredit };

                        var assets = result.Value.Where(l => l.IsLeaf && l.Type == AccountType.Asset && (l.ClosingDebit != 0 || l.ClosingCredit != 0)).Select(ToRow).ToList();
                        var liabilities = result.Value.Where(l => l.IsLeaf && l.Type == AccountType.Liability && (l.ClosingDebit != 0 || l.ClosingCredit != 0))
                            .Select(l => new FinancialLineRow { Code = l.Code, Name = l.Name, Amount = l.ClosingCredit - l.ClosingDebit }).ToList();
                        var equity = result.Value.Where(l => l.IsLeaf && l.Type == AccountType.Equity && (l.ClosingDebit != 0 || l.ClosingCredit != 0))
                            .Select(l => new FinancialLineRow { Code = l.Code, Name = l.Name, Amount = l.ClosingCredit - l.ClosingDebit }).ToList();

                        var rows = Section(LocalizationService.Get("Str.Assets"), assets)
                            .Concat(Section(LocalizationService.Get("Str.Liabilities"), liabilities))
                            .Concat(Section(LocalizationService.Get("Str.Equity"), equity))
                            .ToList();

                        return Result.Ok(new ReportResult
                        {
                            Title = LocalizationService.Get("Str.Module.BalanceSheet"),
                            Columns = FinancialColumns(),
                            Rows = rows,
                            Totals = new()
                            {
                                ["Assets"] = $"{LocalizationService.Get("Str.Assets")}: {assets.Sum(r => r.Amount):N2}",
                                ["Liabilities"] = $"{LocalizationService.Get("Str.Liabilities")}: {liabilities.Sum(r => r.Amount):N2}",
                                ["Equity"] = $"{LocalizationService.Get("Str.Equity")}: {equity.Sum(r => r.Amount):N2}"
                            }
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
                    Parameters = new()
                    {
                        new() { Key = "From", LabelKey = "Str.DateFrom", Kind = FieldKind.Date, DefaultValue = DateTime.Today.AddMonths(-1) },
                        new() { Key = "To", LabelKey = "Str.DateTo", Kind = FieldKind.Date, DefaultValue = DateTime.Today },
                    },
                    // نسخة مُبسَّطة — حركة حساب الصندوق (Accounts.Cash) الفعلية بين تاريخين، لا تصنيف تشغيلي/
                    // استثماري/تمويلي كامل (نطاق مُبسَّط عمداً).
                    Generate = (services, p) =>
                    {
                        var settings = services.GetRequiredService<ISettingsProvider>();
                        var cashCode = settings.Get(SettingKeys.Accounts.Cash, "");
                        if (string.IsNullOrWhiteSpace(cashCode)) return Result.Fail<ReportResult>("حساب الصندوق غير مضبوط في الإعدادات");

                        var from = (DateTime)(p["From"] ?? DateTime.Today.AddMonths(-1));
                        var to = (DateTime)(p["To"] ?? DateTime.Today);

                        var result = services.GetRequiredService<IAccountService>().GetStatement(cashCode, from, to);
                        if (!result.IsSuccess) return Result.Fail<ReportResult>(result.ErrorMessage);

                        var rows = result.Value.Select(l => new StatementRow
                        { Date = l.Date, EntryNo = l.EntryNo, Description = l.Description, Debit = l.Debit, Credit = l.Credit, RunningBalance = l.RunningBalance }).ToList();

                        return Result.Ok(new ReportResult
                        {
                            Title = LocalizationService.Get("Str.Module.CashFlow"),
                            Columns = StatementColumns(),
                            Rows = rows,
                            Totals = new()
                            {
                                ["In"] = $"{LocalizationService.Get("Str.Debit")}: {rows.Sum(r => r.Debit):N2}",
                                ["Out"] = $"{LocalizationService.Get("Str.Credit")}: {rows.Sum(r => r.Credit):N2}"
                            }
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
                    Parameters = new()
                    {
                        new() { Key = "From", LabelKey = "Str.DateFrom", Kind = FieldKind.Date, DefaultValue = DateTime.Today.AddMonths(-1) },
                        new() { Key = "To", LabelKey = "Str.DateTo", Kind = FieldKind.Date, DefaultValue = DateTime.Today },
                        new() { Key = "WarehouseId", LabelKey = "Str.Warehouse", Kind = FieldKind.Picker, PickerType = "Warehouse" },
                    },
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
                    Parameters = new()
                    {
                        new() { Key = "From", LabelKey = "Str.DateFrom", Kind = FieldKind.Date, DefaultValue = DateTime.Today.AddMonths(-1) },
                        new() { Key = "To", LabelKey = "Str.DateTo", Kind = FieldKind.Date, DefaultValue = DateTime.Today },
                    },
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
