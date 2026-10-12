using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Manufacturing;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.PageServices.Manufacturing;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Tests.Helpers;
using Xunit;

namespace PrimeERP.Tests.Services
{
    [Collection("Database")]
    public class ManufacturingTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public ManufacturingTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void BillProductionUnbuildAndScrap_MoveStockAtCost()
        {
            var accounts = _db.Services.GetRequiredService<IAccountService>();
            var settings = _db.Services.GetRequiredService<ISettingsService>();
            string LeafUnder(string parent, string name) =>
                accounts.Create(new CreateAccountDto { ParentId = accounts.GetByCode(parent).Value.Id, Name = name, IsLeaf = true, SkipAutoLink = true }).Value.Code;
            settings.Set(SettingKeys.Accounts.Inventory, LeafUnder("1201", "مخزون"));
            settings.Set(SettingKeys.Accounts.ScrapExpense, LeafUnder("53", "هالك"));

            var warehouse = _db.Services.GetRequiredService<Lookup<Warehouse>>().Add("مخزن الإنتاج");
            var products = _db.Services.GetRequiredService<IProductService>();
            string Product(string name, decimal cost) => products.Create(new Product { Name = name, CostPrice = cost, SalePrice = cost * 2, IsActive = true }).Value.Code;
            var flour = Product("دقيق", 10);
            var sugar = Product("سكر", 20);
            var cake = Product("كيك", 0);

            var stockIn = _db.Services.GetRequiredService<IStockInService>().Create(new CreateStockAdjustmentDto
            {
                WarehouseId = warehouse, MovementDate = DateTime.Today,
                Lines = { new CreateStockAdjustmentLineDto { ProductCode = flour, Qty = 100, UnitCost = 10 }, new CreateStockAdjustmentLineDto { ProductCode = sugar, Qty = 50, UnitCost = 20 } }
            });
            Assert.True(stockIn.IsSuccess, stockIn.ErrorMessage);

            var bom = _db.Services.GetRequiredService<IBillOfMaterialsService>().Create(new CreateBomDto
            {
                Name = "كيك", ProductCode = cake, Qty = 1,
                Lines =
                {
                    new CreateBomLineDto { LineNo = 1, Kind = (int)BomLineKind.Component, ProductCode = flour, Qty = 2, WastePercent = 10 },
                    new CreateBomLineDto { LineNo = 2, Kind = (int)BomLineKind.Component, ProductCode = sugar, Qty = 1 },
                }
            });
            Assert.True(bom.IsSuccess, bom.ErrorMessage);

            var orders = _db.Services.GetRequiredService<IProductionOrderService>();
            var opened = orders.Open(bom.Value.Id, 5);
            Assert.True(opened.IsSuccess, opened.ErrorMessage);
            Assert.Equal(11m, opened.Value.Lines[0].Qty);
            Assert.Equal(5m, opened.Value.Lines[1].Qty);

            opened.Value.RawWarehouseId = warehouse;
            opened.Value.FinishedWarehouseId = warehouse;
            var produced = orders.Create(opened.Value);
            Assert.True(produced.IsSuccess, produced.ErrorMessage);
            Assert.Equal(210m, produced.Value.TotalCost);

            var stock = _db.Services.GetRequiredService<IStockMove>();
            int Id(string code) => products.GetByCode(code).Value.Id;
            Assert.Equal(89m, stock.GetBalance(Id(flour), warehouse).Value);
            Assert.Equal(45m, stock.GetBalance(Id(sugar), warehouse).Value);
            Assert.Equal(5m, stock.GetBalance(Id(cake), warehouse).Value);

            var unbuild = _db.Services.GetRequiredService<IUnbuildService>();
            var reverse = unbuild.Open(bom.Value.Id, 1).Value;
            reverse.RawWarehouseId = warehouse;
            reverse.FinishedWarehouseId = warehouse;
            Assert.True(unbuild.Create(reverse).IsSuccess);
            Assert.Equal(4m, stock.GetBalance(Id(cake), warehouse).Value);
            Assert.Equal(91m, stock.GetBalance(Id(flour), warehouse).Value);

            var scrap = _db.Services.GetRequiredService<IScrapService>().Create(new CreateStockAdjustmentDto
            {
                WarehouseId = warehouse, MovementDate = DateTime.Today,
                Lines = { new CreateStockAdjustmentLineDto { ProductCode = sugar, Qty = 3 } }
            });
            Assert.True(scrap.IsSuccess, scrap.ErrorMessage);
            Assert.Equal(43m, stock.GetBalance(Id(sugar), warehouse).Value);

            Assert.True(orders.Delete(produced.Value.Id).IsFailure, "deleting the order would leave the cake stock at -1");
        }
    }
}
