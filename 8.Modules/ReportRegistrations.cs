using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
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
        }
    }
}
