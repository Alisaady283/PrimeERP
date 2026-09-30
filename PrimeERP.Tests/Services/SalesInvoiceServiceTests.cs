using PrimeERP.Application.Legacy.Admin;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Tests.Helpers;
using PrimeERP.Domain.Entities;
using PrimeERP.Data.Core;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.Legacy.Common;
using PrimeERP.Application.Legacy.Inventory;
using PrimeERP.Application.Legacy.Parties;
using PrimeERP.Application.Legacy.Sales;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>فاتورة البيع وقيدها</summary>
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

            var warehouses = _db.Services.GetRequiredService<Lookup<Warehouse>>();
            _warehouseId = warehouses.Add("مخزن اختبار");

            var customers = _db.Services.GetRequiredService<ICustomerService>();
            _customerId = customers.Create(new CreateCustomerDto { Name = "عميل اختبار" }).Value.Id;

            var stock = _db.Services.GetRequiredService<IStockMove>();
            DbContextFactory.RunTransaction(db => stock.RecordMovement(db, _productId, _warehouseId, MovementType.In, 100, 10, "Seed", null, "SEED-1"));
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

            var line = invoice.Lines.Single();
            Assert.Equal(250m, line.LineTotal);
            Assert.Equal(37.5m, line.VatAmount);
            Assert.Equal(287.5m, line.NetAmount);

            var stock = _db.Services.GetRequiredService<IStockMove>();
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

            var stock = _db.Services.GetRequiredService<IStockMove>();
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
