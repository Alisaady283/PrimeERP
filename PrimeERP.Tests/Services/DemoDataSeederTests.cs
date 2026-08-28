using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Assets;
using PrimeERP.Application.Services.HR;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Purchasing;
using PrimeERP.Application.Services.Sales;
using PrimeERP.Modules;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    // يشغّل الزارع فعلياً عبر نفس الحاوية التي يبنيها OnStartup (TestDatabaseFixture تستخدم AddPlatform/
    // AddData/AddApplication/AddUI الحقيقية) — يتحقق من الأعداد ومن أن التشغيل الثاني لا يكرّر شيئاً.
    public class DemoDataSeederTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        public DemoDataSeederTests() => AppSession.DevMode = true;
        public void Dispose() => _db.Dispose();

        [Fact]
        public void Seed_PopulatesAllModules_AndIsIdempotent()
        {
            DemoDataSeeder.Seed(_db.Services);

            var customers = _db.Services.GetRequiredService<ICustomerService>();
            var suppliers = _db.Services.GetRequiredService<ISupplierService>();
            var products = _db.Services.GetRequiredService<IProductService>();
            var warehouses = _db.Services.GetRequiredService<IWarehouseService>();
            var employees = _db.Services.GetRequiredService<IEmployeeService>();
            var assets = _db.Services.GetRequiredService<IAssetService>();
            var salesInvoices = _db.Services.GetRequiredService<ISalesInvoiceService>();
            var purchaseInvoices = _db.Services.GetRequiredService<IPurchaseInvoiceService>();
            var stock = _db.Services.GetRequiredService<IStockService>();

            Assert.Equal(2, customers.GetPaged(1, 50).Value.TotalCount);
            Assert.Equal(2, suppliers.GetPaged(1, 50).Value.TotalCount);
            Assert.Equal(2, products.GetPaged(1, 50).Value.TotalCount);
            Assert.Equal(2, warehouses.GetAll().Value.Count);
            Assert.Equal(2, employees.GetPaged(1, 50).Value.TotalCount);
            Assert.Equal(2, assets.GetPaged(1, 50).Value.TotalCount);
            Assert.Equal(1, salesInvoices.GetPaged(1, 50).Value.TotalCount);
            Assert.Equal(1, purchaseInvoices.GetPaged(1, 50).Value.TotalCount);

            var firstProduct = products.GetPaged(1, 50).Value.Items[0];
            Assert.True(stock.GetBalance(firstProduct.Id).Value > 0);

            // تشغيل ثانٍ — البوابة الداخلية يجب أن تمنع أي تكرار.
            DemoDataSeeder.Seed(_db.Services);
            Assert.Equal(2, customers.GetPaged(1, 50).Value.TotalCount);
            Assert.Equal(2, products.GetPaged(1, 50).Value.TotalCount);
        }
    }
}
