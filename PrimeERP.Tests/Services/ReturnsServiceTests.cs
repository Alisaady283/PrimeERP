using PrimeERP.Data.Core;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Purchasing;
using PrimeERP.Application.Services.Sales;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;
using PrimeERP.Application.Services.Admin;

namespace PrimeERP.Tests.Services
{
    /// <summary>المرتجعات</summary>
    public class ReturnsServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly int _productId, _warehouseId, _customerId, _supplierId;
        private readonly string _productCode;

        public ReturnsServiceTests()
        {
            AppSession.DevMode = true;

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
            settings.Set(SettingKeys.Accounts.VATOutput, LeafUnder("21", "ضريبة مخرجات"));
            settings.Set(SettingKeys.Accounts.VATInput, LeafUnder("12", "ضريبة مدخلات"));

            var categories = _db.Services.GetRequiredService<ICategoryService>();
            var category = categories.Create(new CreateCategoryDto { Name = "فئة", ModuleKey = "Products" }).Value;
            var products = _db.Services.GetRequiredService<IProductService>();
            var product = products.Create(new CreateProductDto { Name = "صنف", CategoryId = category.Id, CostPrice = 10, SalePrice = 25 }).Value;
            _productId = product.Id; _productCode = product.Code;

            var warehouses = _db.Services.GetRequiredService<IWarehouseService>();
            _warehouseId = warehouses.Create(new CreateWarehouseDto { Name = "مخزن" }).Value.Id;

            var customers = _db.Services.GetRequiredService<ICustomerService>();
            _customerId = customers.Create(new CreateCustomerDto { Name = "عميل" }).Value.Id;
            var suppliers = _db.Services.GetRequiredService<ISupplierService>();
            _supplierId = suppliers.Create(new CreateSupplierDto { Name = "مورد" }).Value.Id;

            var stock = _db.Services.GetRequiredService<IStockService>();
            DbContextFactory.RunTransaction(db => stock.RecordMovement(db, _productId, _warehouseId, Domain.Enums.MovementType.In, 100, 10, "Seed", null, "SEED"));
        }

        public void Dispose() => _db.Dispose();

        [Fact]
        public void SalesReturn_AfterSale_RestoresStockAndBalancedJournal()
        {
            var invoices = _db.Services.GetRequiredService<ISalesInvoiceService>();
            invoices.Create(new CreateSalesInvoiceDto
            {
                InvoiceDate = DateTime.Today, CustomerId = _customerId, WarehouseId = _warehouseId,
                Lines = { new CreateSalesInvoiceLineDto { LineNo = 1, ProductCode = _productCode, Qty = 10, UnitPrice = 25, VatPercent = 14 } }
            });

            var stock = _db.Services.GetRequiredService<IStockService>();
            Assert.Equal(90, stock.GetBalance(_productId, _warehouseId).Value);

            var returns = _db.Services.GetRequiredService<ISalesReturnService>();
            var result = returns.Create(new CreateSalesReturnDto
            {
                ReturnDate = DateTime.Today, CustomerId = _customerId, WarehouseId = _warehouseId,
                Lines = { new CreateSalesReturnLineDto { LineNo = 1, ProductCode = _productCode, Qty = 4, UnitPrice = 25, VatPercent = 14 } }
            });

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(94, stock.GetBalance(_productId, _warehouseId).Value);

            var journal = _db.Services.GetRequiredService<IJournalService>();
            var tb = journal.GetTrialBalance(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)).Value;
            Assert.Equal(tb.Sum(l => l.PeriodDebit), tb.Sum(l => l.PeriodCredit));
        }

        [Fact]
        public void PurchaseReturn_AfterPurchase_ReducesStockAndBalancedJournal()
        {
            var invoices = _db.Services.GetRequiredService<IPurchaseInvoiceService>();
            invoices.Create(new CreatePurchaseInvoiceDto
            {
                InvoiceDate = DateTime.Today, SupplierId = _supplierId, WarehouseId = _warehouseId,
                Lines = { new CreatePurchaseInvoiceLineDto { LineNo = 1, ProductCode = _productCode, Qty = 20, UnitPrice = 8, VatPercent = 14 } }
            });

            var stock = _db.Services.GetRequiredService<IStockService>();
            Assert.Equal(120, stock.GetBalance(_productId, _warehouseId).Value);

            var returns = _db.Services.GetRequiredService<IPurchaseReturnService>();
            var result = returns.Create(new CreatePurchaseReturnDto
            {
                ReturnDate = DateTime.Today, SupplierId = _supplierId, WarehouseId = _warehouseId,
                Lines = { new CreatePurchaseReturnLineDto { LineNo = 1, ProductCode = _productCode, Qty = 5, UnitPrice = 8, VatPercent = 14 } }
            });

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(115, stock.GetBalance(_productId, _warehouseId).Value);

            var journal = _db.Services.GetRequiredService<IJournalService>();
            var tb = journal.GetTrialBalance(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)).Value;
            Assert.Equal(tb.Sum(l => l.PeriodDebit), tb.Sum(l => l.PeriodCredit));
        }
    }
}
