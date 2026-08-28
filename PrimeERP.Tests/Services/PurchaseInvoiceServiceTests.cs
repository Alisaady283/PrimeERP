using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Purchasing;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    // نسخة طبق الأصل من SalesInvoiceServiceTests — نفس منطق زرع الحسابات، الاتجاه معكوس (شراء يزيد المخزون).
    public class PurchaseInvoiceServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly IPurchaseInvoiceService _invoices;
        private readonly int _productId, _warehouseId, _supplierId;

        public PurchaseInvoiceServiceTests()
        {
            AppSession.DevMode = true;
            _invoices = _db.Services.GetRequiredService<IPurchaseInvoiceService>();

            var accounts = _db.Services.GetRequiredService<IAccountService>();
            var settings = _db.Services.GetRequiredService<ISettingsService>();

            string LeafUnder(string parentCode, string name)
            {
                var parent = accounts.GetByCode(parentCode).Value;
                var leaf = accounts.Create(new CreateAccountDto { ParentId = parent.Id, Name = name, IsLeaf = true, SkipAutoLink = true }).Value;
                return leaf.Code;
            }

            settings.Set(SettingKeys.Accounts.Inventory, LeafUnder("1201", "مخزون اختبار شراء"));
            settings.Set(SettingKeys.Accounts.VATInput, LeafUnder("12", "ضريبة مدخلات مستردة"));

            var categories = _db.Services.GetRequiredService<ICategoryService>();
            var category = categories.Create(new CreateCategoryDto { Name = "فئة اختبار", ModuleKey = "Products" }).Value;

            var products = _db.Services.GetRequiredService<IProductService>();
            _productId = products.Create(new CreateProductDto { Name = "صنف اختبار", CategoryId = category.Id, CostPrice = 10, SalePrice = 25 }).Value.Id;

            var warehouses = _db.Services.GetRequiredService<IWarehouseService>();
            _warehouseId = warehouses.Create(new CreateWarehouseDto { Name = "مخزن اختبار" }).Value.Id;

            var suppliers = _db.Services.GetRequiredService<ISupplierService>();
            _supplierId = suppliers.Create(new CreateSupplierDto { Name = "مورد اختبار" }).Value.Id;
        }

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Create_ValidInvoice_IncreasesStockAndPostsBalancedJournal()
        {
            var products = _db.Services.GetRequiredService<IProductService>();
            var productCode = products.GetById(_productId).Value.Code;

            var result = _invoices.Create(new CreatePurchaseInvoiceDto
            {
                InvoiceDate = DateTime.Today, SupplierId = _supplierId, WarehouseId = _warehouseId,
                Lines = { new CreatePurchaseInvoiceLineDto { LineNo = 1, ProductCode = productCode, Qty = 20, UnitPrice = 8, TaxPercent = 15 } }
            });

            Assert.True(result.IsSuccess, result.ErrorMessage);
            var invoice = result.Value;

            Assert.Equal(160, invoice.SubTotal);
            Assert.Equal(24, invoice.TaxAmount);
            Assert.Equal(184, invoice.NetTotal);

            var stock = _db.Services.GetRequiredService<IStockService>();
            Assert.Equal(20, stock.GetBalance(_productId, _warehouseId).Value);

            var journal = _db.Services.GetRequiredService<IJournalService>();
            var trialBalance = journal.GetTrialBalance(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)).Value;
            Assert.Equal(trialBalance.Sum(l => l.PeriodDebit), trialBalance.Sum(l => l.PeriodCredit));
        }
    }
}
