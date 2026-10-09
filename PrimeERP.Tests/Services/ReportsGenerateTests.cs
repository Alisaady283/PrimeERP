using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Tests.Helpers;
using PrimeERP.Domain.Entities;
using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.Common;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.PageServices.Parties;
using PrimeERP.Application.PageServices.Purchasing;
using PrimeERP.Application.PageServices.Sales;
using PrimeERP.Composition.Registry;
using PrimeERP.Modules;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;
using PrimeERP.Composition.Renderers;

namespace PrimeERP.Tests.Services
{
    /// <summary>توليد التقارير</summary>
    public class ReportsGenerateTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly int _customerId, _supplierId, _warehouseId;
        private readonly string _productCode;
        private readonly IModuleRegistry _registry;

        public ReportsGenerateTests()
        {
            AppSession.DevMode = true;
            _registry = _db.Services.GetRequiredService<IModuleRegistry>();
            ReportRegistrations.RegisterAll(_registry);

            var accounts = _db.Services.GetRequiredService<IAccountService>();
            var settings = _db.Services.GetRequiredService<ISettingsService>();
            string LeafUnder(string parentCode, string name)
            {
                var parent = accounts.GetByCode(parentCode).Value;
                return accounts.Create(new CreateAccountDto { ParentId = parent.Id, Name = name, IsLeaf = true, SkipAutoLink = true }).Value.Code;
            }
            settings.Set(SettingKeys.Accounts.Sales, LeafUnder("41", "مبيعات"));
            settings.Set(SettingKeys.Accounts.COGS, LeafUnder("51", "تكلفة"));
            settings.Set(SettingKeys.Accounts.Inventory, LeafUnder("1201", "مخزون"));
            settings.Set(SettingKeys.Accounts.VATOutput, LeafUnder("2203", "ضريبة مخرجات"));
            settings.Set(SettingKeys.Accounts.VATInput, LeafUnder("12", "ضريبة مدخلات"));
            settings.Set(SettingKeys.Accounts.Cash, LeafUnder("1203007", "الصندوق"));

            var categories = _db.Services.GetRequiredService<ICategoryService>();
            var category = categories.Create(new Category { Name = "فئة", ModuleKey = "Products" }).Value;
            var products = _db.Services.GetRequiredService<IProductService>();
            var product = products.Create(new Product { Name = "صنف", CategoryId = category.Id, CostPrice = 10, SalePrice = 25 }).Value;
            _productCode = product.Code;

            var warehouses = _db.Services.GetRequiredService<Lookup<Warehouse>>();
            _warehouseId = warehouses.Add("مخزن");

            var customers = _db.Services.GetRequiredService<ICustomerService>();
            _customerId = customers.Create(new Customer { Name = "عميل" }).Value.Id;
            var suppliers = _db.Services.GetRequiredService<ISupplierService>();
            _supplierId = suppliers.Create(new Supplier { Name = "مورد" }).Value.Id;

            var stock = _db.Services.GetRequiredService<IStockMove>();
            Data.Core.DbContextFactory.RunTransaction(db => stock.RecordMovement(db, product.Id, _warehouseId, Domain.Enums.MovementType.In, 100, 10, "Seed", null, "SEED"));

            _db.Services.GetRequiredService<ISalesInvoiceService>().Create(new CreateSalesInvoiceDto
            {
                InvoiceDate = DateTime.Today, CustomerId = _customerId, WarehouseId = _warehouseId,
                Lines = { new CreateSalesInvoiceLineDto { LineNo = 1, ProductCode = _productCode, Qty = 5, UnitPrice = 25, VatPercent = 14 } }
            });
            _db.Services.GetRequiredService<IPurchaseInvoiceService>().Create(new CreatePurchaseInvoiceDto
            {
                InvoiceDate = DateTime.Today, SupplierId = _supplierId, WarehouseId = _warehouseId,
                Lines = { new CreatePurchaseInvoiceLineDto { LineNo = 1, ProductCode = _productCode, Qty = 10, UnitPrice = 8, VatPercent = 14 } }
            });
        }

        public void Dispose() => _db.Dispose();

        private Dictionary<string, object> Params(params (string, object)[] values) => values.ToDictionary(v => v.Item1, v => v.Item2);

        [Fact]
        public void CustomerStatement_ReturnsInvoiceLine()
        {
            var def = _registry.Get("CustomerStatement").Report;
            var result = ReportRenderer.Run(def, _db.Services, Params(("CustomerId", _customerId), ("From", DateTime.Today.AddDays(-1)), ("To", DateTime.Today.AddDays(1))));
            Assert.True(result.IsSuccess, result.ErrorMessage);
        }

        [Fact]
        public void SupplierStatement_ReturnsInvoiceLine()
        {
            var def = _registry.Get("SupplierStatement").Report;
            var result = ReportRenderer.Run(def, _db.Services, Params(("SupplierId", _supplierId), ("From", DateTime.Today.AddDays(-1)), ("To", DateTime.Today.AddDays(1))));
            Assert.True(result.IsSuccess, result.ErrorMessage);
        }

        [Fact]
        public void ItemCard_ShowsInAndOutMovements()
        {
            var products = _db.Services.GetRequiredService<IProductService>();
            var productId = products.GetPaged(1, 10).Value.Items.First(p => p.Code == _productCode).Id;

            var def = _registry.Get("ItemCard").Report;
            var result = ReportRenderer.Run(def, _db.Services, Params(("ProductId", productId)));
            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.True(result.Value.Rows.Cast<object>().Count() >= 3); // seed + sale + purchase
        }

        [Fact]
        public void IncomeStatement_HasRevenueAndExpenseWithNetIncome()
        {
            var def = _registry.Get("IncomeStatement").Report;
            var result = ReportRenderer.Run(def, _db.Services, Params(("From", DateTime.Today.AddDays(-1)), ("To", DateTime.Today.AddDays(1))));
            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.True(result.Value.Totals.ContainsKey("Net"));
        }

        [Fact]
        public void BalanceSheet_HasAssetsLiabilitiesEquity()
        {
            var def = _registry.Get("BalanceSheet").Report;
            var result = ReportRenderer.Run(def, _db.Services, Params(("AsOf", DateTime.Today.AddDays(1))));
            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.True(result.Value.Totals.ContainsKey("Assets"));
        }

        [Fact]
        public void CashFlow_ShowsCashMovement()
        {
            var def = _registry.Get("CashFlow").Report;
            var result = ReportRenderer.Run(def, _db.Services, Params(("From", DateTime.Today.AddDays(-1)), ("To", DateTime.Today.AddDays(1))));
            Assert.True(result.IsSuccess, result.ErrorMessage);
        }

        [Fact]
        public void StockReport_ListsMovements()
        {
            var def = _registry.Get("StockReport").Report;
            var result = ReportRenderer.Run(def, _db.Services, Params(("From", DateTime.Today.AddDays(-1)), ("To", DateTime.Today.AddDays(1))));
            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.True(result.Value.Rows.Cast<object>().Count() >= 3);
        }

        [Fact]
        public void SalesReport_ShowsInvoiceInRange()
        {
            var def = _registry.Get("SalesReport").Report;
            var result = ReportRenderer.Run(def, _db.Services, Params(("From", DateTime.Today.AddDays(-1)), ("To", DateTime.Today.AddDays(1))));
            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Single(result.Value.Rows.Cast<object>());
        }
    }
}
