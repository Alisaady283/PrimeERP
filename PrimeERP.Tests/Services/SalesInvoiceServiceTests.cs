using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Sales;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Tests.Services
{
    // اختبار حقيقي كامل — يزرع حساباً فرعياً حقيقياً تحت كل جذر (41/51/1201) ويعيد ضبط الإعدادات لتشير
    // إليها (بالضبط ما يفعله مسؤول النظام يدوياً؛ الجذور الافتراضية IsLeaf=false فلا تقبل ترحيلاً مباشراً —
    // نفس القيد المطبَّق أصلاً على 1202/2101 وتحله AutoLink للعميل/المورد تلقائياً).
    public class SalesInvoiceServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly ISalesInvoiceService _invoices;
        private readonly int _productId, _warehouseId, _customerId;

        public SalesInvoiceServiceTests()
        {
            AppSession.DevMode = true;
            _invoices = _db.Services.GetRequiredService<ISalesInvoiceService>();

            var accounts = _db.Services.GetRequiredService<IAccountService>();
            var settings = _db.Services.GetRequiredService<ISettingsService>();

            string LeafUnder(string parentCode, string name)
            {
                var parent = accounts.GetByCode(parentCode).Value;
                var leaf = accounts.Create(new CreateAccountDto { ParentId = parent.Id, Name = name, IsLeaf = true, SkipAutoLink = true }).Value;
                return leaf.Code;
            }

            settings.Set(SettingKeys.Accounts.Sales, LeafUnder("41", "مبيعات اختبار"));
            settings.Set(SettingKeys.Accounts.COGS, LeafUnder("51", "تكلفة مبيعات اختبار"));
            settings.Set(SettingKeys.Accounts.Inventory, LeafUnder("1201", "مخزون اختبار"));
            settings.Set(SettingKeys.Accounts.VATOutput, LeafUnder("21", "ضريبة مبيعات مستحقة"));

            var categories = _db.Services.GetRequiredService<ICategoryService>();
            var category = categories.Create(new CreateCategoryDto { Name = "فئة اختبار", ModuleKey = "Products" }).Value;

            var products = _db.Services.GetRequiredService<IProductService>();
            _productId = products.Create(new CreateProductDto { Name = "صنف اختبار", CategoryId = category.Id, CostPrice = 10, SalePrice = 25 }).Value.Id;

            var warehouses = _db.Services.GetRequiredService<IWarehouseService>();
            _warehouseId = warehouses.Create(new CreateWarehouseDto { Name = "مخزن اختبار" }).Value.Id;

            var customers = _db.Services.GetRequiredService<ICustomerService>();
            _customerId = customers.Create(new CreateCustomerDto { Name = "عميل اختبار" }).Value.Id;

            var stock = _db.Services.GetRequiredService<IStockService>();
            Db.RunTransaction((conn, tx) => stock.RecordMovement(conn, tx, _productId, _warehouseId, MovementType.In, 100, 10, "Seed", null, "SEED-1"));
        }

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Create_ValidInvoice_PostsStockAndBalancedJournal()
        {
            var products = _db.Services.GetRequiredService<IProductService>();
            var productCode = products.GetById(_productId).Value.Code;

            var result = _invoices.Create(new CreateSalesInvoiceDto
            {
                InvoiceDate = DateTime.Today, CustomerId = _customerId, WarehouseId = _warehouseId,
                Lines = { new CreateSalesInvoiceLineDto { LineNo = 1, ProductCode = productCode, Qty = 10, UnitPrice = 25, VatPercent = 15 } }
            });

            Assert.True(result.IsSuccess, result.ErrorMessage);
            var invoice = result.Value;

            Assert.Equal(250, invoice.SubTotal);
            Assert.Equal(37.5m, invoice.VatAmount);
            Assert.Equal(287.5m, invoice.NetTotal);
            Assert.Single(invoice.Lines);

            var stock = _db.Services.GetRequiredService<IStockService>();
            Assert.Equal(90, stock.GetBalance(_productId, _warehouseId).Value);

            var journal = _db.Services.GetRequiredService<IJournalService>();
            var trialBalance = journal.GetTrialBalance(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)).Value;
            var totalDebit = trialBalance.Sum(l => l.PeriodDebit);
            var totalCredit = trialBalance.Sum(l => l.PeriodCredit);
            Assert.Equal(totalDebit, totalCredit);
            Assert.True(totalDebit > 0);
        }

        [Fact]
        public void Create_InsufficientStock_Fails()
        {
            var products = _db.Services.GetRequiredService<IProductService>();
            var productCode = products.GetById(_productId).Value.Code;

            var result = _invoices.Create(new CreateSalesInvoiceDto
            {
                InvoiceDate = DateTime.Today, CustomerId = _customerId, WarehouseId = _warehouseId,
                Lines = { new CreateSalesInvoiceLineDto { LineNo = 1, ProductCode = productCode, Qty = 1000, UnitPrice = 25 } }
            });

            Assert.False(result.IsSuccess);

            var stock = _db.Services.GetRequiredService<IStockService>();
            Assert.Equal(100, stock.GetBalance(_productId, _warehouseId).Value);
        }

        [Fact]
        public void Update_AlwaysDenied()
        {
            var result = _invoices.Update(new CreateSalesInvoiceDto { Id = 1 });
            Assert.False(result.IsSuccess);
        }
    }
}
