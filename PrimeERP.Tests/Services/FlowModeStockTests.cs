using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Tests.Helpers;
using PrimeERP.Domain.Entities;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.PageServices.Parties;
using PrimeERP.Application.PageServices.Sales;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>الفرق بين الوضعين</summary>
    public class FlowModeStockTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public FlowModeStockTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Theory]
        [InlineData(true,  6)]   // مبسّط: 10 - 4
        [InlineData(false, 10)]  // شامل: الفاتورة لا تمسّ الرصيد
        public void SalesInvoice_MovesStock_OnlyInSimplifiedFlow(bool simplified, decimal expectedOnHand)
        {
            var accounts = _db.Services.GetRequiredService<PrimeERP.Application.PageServices.Accounting.IAccountService>();
            var settings = _db.Services.GetRequiredService<ISettingsService>();

            string LeafUnder(string parentCode, string name)
            {
                var parent = accounts.GetByCode(parentCode).Value;
                return accounts.Create(new PrimeERP.Application.DTOs.Accounting.CreateAccountDto
                { ParentId = parent.Id, Name = name, IsLeaf = true, SkipAutoLink = true }).Value.Code;
            }

            settings.Set(SettingKeys.Accounts.Inventory, LeafUnder("1201", "مخزون"));
            settings.Set(SettingKeys.Accounts.Sales, LeafUnder("41", "مبيعات"));
            settings.Set(SettingKeys.Accounts.COGS, LeafUnder("51", "تكلفة مبيعات"));
            settings.Set(SettingKeys.Accounts.VATOutput, LeafUnder("2203", "ضريبة مخرجات"));
            settings.SetMany(new Dictionary<string, object> { [SettingKeys.Documents.SimplifiedFlow] = simplified });

            var warehouseId = _db.Services.GetRequiredService<Lookup<Warehouse>>().Add("مخزن الاختبار");

            var product = _db.Services.GetRequiredService<IProductService>()
                .Create(new Product { Name = "صنف", CostPrice = 5, SalePrice = 20, IsActive = true });
            Assert.True(product.IsSuccess, product.ErrorMessage);

            var customer = _db.Services.GetRequiredService<ICustomerService>()
                .Create(new Customer { Name = "عميل", IsActive = true });
            Assert.True(customer.IsSuccess, customer.ErrorMessage);

            var stockIn = _db.Services.GetRequiredService<IGoodsReceiptService>().Create(new CreateStockAdjustmentDto
            {
                WarehouseId = warehouseId,
                MovementDate = DateTime.Today,
                Lines = { new CreateStockAdjustmentLineDto { ProductCode = product.Value.Code, Qty = 10, UnitCost = 5 } }
            });
            Assert.True(stockIn.IsSuccess, stockIn.ErrorMessage);

            var invoice = _db.Services.GetRequiredService<ISalesInvoiceService>().Create(new CreateSalesInvoiceDto
            {
                CustomerId = customer.Value.Id,
                WarehouseId = warehouseId,
                InvoiceDate = DateTime.Today,
                Lines = { new CreateSalesInvoiceLineDto { ProductCode = product.Value.Code, Qty = 4, UnitPrice = 20 } }
            });
            Assert.True(invoice.IsSuccess, invoice.ErrorMessage);

            var onHand = _db.Services.GetRequiredService<IStockMove>()
                .GetBalance(product.Value.Id, warehouseId);
            Assert.True(onHand.IsSuccess, onHand.ErrorMessage);
            Assert.Equal(expectedOnHand, onHand.Value);
        }
    }
}
