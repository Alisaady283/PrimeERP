using System;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Tests.Helpers;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.PageServices.Parties;
using PrimeERP.Application.PageServices.Purchasing;
using PrimeERP.Application.PageServices.Sales;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    [Collection("Database")]
    public class SimplifiedStockCycleTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public SimplifiedStockCycleTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void InvoicesReturnsAndDeletes_MoveTheWarehouseBalance_AndOverSellingIsRefused()
        {
            var accounts = _db.Services.GetRequiredService<IAccountService>();
            var settings = _db.Services.GetRequiredService<ISettingsService>();

            string LeafUnder(string parentCode, string name) =>
                accounts.Create(new CreateAccountDto { ParentId = accounts.GetByCode(parentCode).Value.Id, Name = name, IsLeaf = true, SkipAutoLink = true }).Value.Code;

            settings.Set(SettingKeys.Accounts.Inventory, LeafUnder("1201", "مخزون"));
            settings.Set(SettingKeys.Accounts.Sales, LeafUnder("41", "مبيعات"));
            settings.Set(SettingKeys.Accounts.COGS, LeafUnder("51", "تكلفة مبيعات"));
            settings.Set(SettingKeys.Accounts.VATOutput, LeafUnder("2203", "ضريبة مخرجات"));
            settings.Set(SettingKeys.Accounts.VATInput, LeafUnder("12", "ضريبة مدخلات"));
            settings.SetMany(new Dictionary<string, object> { [SettingKeys.Documents.SimplifiedFlow] = true });

            var warehouseId = _db.Services.GetRequiredService<Lookup<Warehouse>>().Add("مخزن");
            var code = _db.Services.GetRequiredService<IProductService>()
                .Create(new Product { Name = "صنف", CostPrice = 5, SalePrice = 20, IsActive = true }).Value.Code;
            var productId = _db.Services.GetRequiredService<IProductService>().GetByCode(code).Value.Id;
            var customerId = _db.Services.GetRequiredService<ICustomerService>().Create(new Customer { Name = "عميل", IsActive = true }).Value.Id;
            var supplierId = _db.Services.GetRequiredService<ISupplierService>().Create(new Supplier { Name = "مورد" }).Value.Id;

            var stock = _db.Services.GetRequiredService<IStockMove>();
            decimal OnHand() => stock.GetBalance(productId, warehouseId).Value;

            var purchases = _db.Services.GetRequiredService<IPurchaseInvoiceService>();
            var sales = _db.Services.GetRequiredService<ISalesInvoiceService>();
            var salesReturns = _db.Services.GetRequiredService<ISalesReturnService>();
            var purchaseReturns = _db.Services.GetRequiredService<IPurchaseReturnService>();

            var bought = purchases.Create(new CreatePurchaseInvoiceDto
            { SupplierId = supplierId, WarehouseId = warehouseId, Lines = { new CreatePurchaseInvoiceLineDto { ProductCode = code, Qty = 10, UnitPrice = 5 } } });
            Assert.True(bought.IsSuccess, bought.ErrorMessage);
            Assert.Equal(10m, OnHand());

            var sold = sales.Create(new CreateSalesInvoiceDto
            { CustomerId = customerId, WarehouseId = warehouseId, Lines = { new CreateSalesInvoiceLineDto { ProductCode = code, Qty = 4, UnitPrice = 20 } } });
            Assert.True(sold.IsSuccess, sold.ErrorMessage);
            Assert.Equal(6m, OnHand());

            var overSold = sales.Create(new CreateSalesInvoiceDto
            { CustomerId = customerId, WarehouseId = warehouseId, Lines = { new CreateSalesInvoiceLineDto { ProductCode = code, Qty = 7, UnitPrice = 20 } } });
            Assert.False(overSold.IsSuccess, "selling 7 with 6 on hand was accepted");
            Assert.Equal(6m, OnHand());

            var returned = salesReturns.Create(new CreateSalesReturnDto
            { CustomerId = customerId, WarehouseId = warehouseId, Lines = { new CreateSalesReturnLineDto { ProductCode = code, Qty = 2, UnitPrice = 20 } } });
            Assert.True(returned.IsSuccess, returned.ErrorMessage);
            Assert.Equal(8m, OnHand());

            var sentBack = purchaseReturns.Create(new CreatePurchaseReturnDto
            { SupplierId = supplierId, WarehouseId = warehouseId, Lines = { new CreatePurchaseReturnLineDto { ProductCode = code, Qty = 3, UnitPrice = 5 } } });
            Assert.True(sentBack.IsSuccess, sentBack.ErrorMessage);
            Assert.Equal(5m, OnHand());

            var overReturned = purchaseReturns.Create(new CreatePurchaseReturnDto
            { SupplierId = supplierId, WarehouseId = warehouseId, Lines = { new CreatePurchaseReturnLineDto { ProductCode = code, Qty = 6, UnitPrice = 5 } } });
            Assert.False(overReturned.IsSuccess, "returning 6 to the supplier with 5 on hand was accepted");
            Assert.Equal(5m, OnHand());

            Assert.True(salesReturns.Delete(returned.Value.Id).IsSuccess);
            Assert.Equal(3m, OnHand());

            Assert.True(sales.Delete(sold.Value.Id).IsSuccess);
            Assert.Equal(7m, OnHand());

            Assert.True(purchaseReturns.Delete(sentBack.Value.Id).IsSuccess);
            Assert.Equal(10m, OnHand());

            var soldAgain = sales.Create(new CreateSalesInvoiceDto
            { CustomerId = customerId, WarehouseId = warehouseId, Lines = { new CreateSalesInvoiceLineDto { ProductCode = code, Qty = 8, UnitPrice = 20 } } });
            Assert.True(soldAgain.IsSuccess, soldAgain.ErrorMessage);
            Assert.Equal(2m, OnHand());

            var deletePurchase = purchases.Delete(bought.Value.Id);
            Assert.False(deletePurchase.IsSuccess, "deleting the purchase left the warehouse at -8");
            Assert.Equal(2m, OnHand());
        }
    }
}
