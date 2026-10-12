using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Application.PageServices.Common;
using PrimeERP.Application.PageServices.HR;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.PageServices.Parties;
using PrimeERP.Application.PageServices.Purchasing;
using PrimeERP.Application.PageServices.Sales;
using PrimeERP.Application.PageServices.Treasury;
using PrimeERP.Application.PageServices.Vouchers;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PrimeERP.Tests.Tools
{
    /// <summary>بياناتٌ وهمية عبر الخدمات نفسها: يعمل فقط إن حُدِّد ملف القاعدة في PRIMEERP_DEMO_DB، ويأخذ نسخةً قبل الكتابة</summary>
    public class DemoDataTool
    {
        private readonly ITestOutputHelper _log;
        private readonly Random _random = new(2026);

        public DemoDataTool(ITestOutputHelper log) => _log = log;

        [Fact]
        public void Generate()
        {
            var path = Environment.GetEnvironmentVariable("PRIMEERP_DEMO_DB");
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;

            var backup = $"{path}.before-demo-{DateTime.Now:yyyyMMdd-HHmmss}.bak";
            File.Copy(path, backup);
            _log.WriteLine($"backup: {backup}");

            AppSession.DevMode = true;
            DbConfig.Use(new DbConfig { Provider = DatabaseProvider.Sqlite, FilePath = path });
            var services = TestDatabaseFixture.BuildServices();
            var settings = services.GetRequiredService<ISettingsService>();
            var simplified = settings.Get(SettingKeys.Documents.SimplifiedFlow, true);
            settings.SetMany(new Dictionary<string, object> { [SettingKeys.Documents.SimplifiedFlow] = true });
            try { Seed(services); }
            finally { settings.SetMany(new Dictionary<string, object> { [SettingKeys.Documents.SimplifiedFlow] = simplified }); }
        }

        private void Seed(IServiceProvider services)
        {
            var start = new DateTime(DateTime.Today.Year, 1, 1);
            var today = DateTime.Today;

            var warehouses = services.GetRequiredService<Lookup<Warehouse>>();
            var main = warehouses.Add("المخزن الرئيسي");
            var branch = warehouses.Add("مخزن الفرع");

            var categories = services.GetRequiredService<ICategoryService>();
            var categoryIds = new[] { "إلكترونيات", "أدوات منزلية", "قرطاسية", "مواد غذائية" }
                .Select(n => Ok(categories.Create(new Category { Name = n, ModuleKey = "Products", IsActive = true })).Id).ToList();

            var productService = services.GetRequiredService<IProductService>();
            var products = Enumerable.Range(1, 20).Select(i =>
            {
                var cost = Math.Round((decimal)(20 + _random.NextDouble() * 480), 0);
                return Ok(productService.Create(new Product
                {
                    Name = $"صنف تجريبي {i:00}", CategoryId = categoryIds[i % categoryIds.Count],
                    CostPrice = cost, SalePrice = Math.Round(cost * (decimal)(1.25 + _random.NextDouble() * 0.35), 0), IsActive = true
                }));
            }).ToList();
            var selling = products.Take(17).ToList();
            var dead = products.Skip(17).ToList();

            var cities = new[] { "القاهرة", "الإسكندرية", "الجيزة", "المنصورة", "طنطا", "أسيوط" };
            var customerService = services.GetRequiredService<ICustomerService>();
            var customers = Enumerable.Range(1, 12).Select(i => (Party: Ok(customerService.Create(new Customer
            {
                Name = $"عميل تجريبي {i:00}", City = cities[i % cities.Length], PaymentTermDays = new[] { 0, 15, 30, 45 }[i % 4], IsActive = true
            })), Behaviour: i <= 6 ? 0 : i <= 10 ? 1 : 2)).ToList();

            var supplierService = services.GetRequiredService<ISupplierService>();
            var suppliers = Enumerable.Range(1, 6).Select(i => Ok(supplierService.Create(new Supplier
            {
                Name = $"مورد تجريبي {i:00}", City = cities[i % cities.Length], PaymentTermDays = 30, IsActive = true
            }))).ToList();

            var treasury = Treasury(services);
            Capital(services, treasury, start);
            var purchases = services.GetRequiredService<IPurchaseInvoiceService>();
            var sales = services.GetRequiredService<ISalesInvoiceService>();
            var returns = services.GetRequiredService<ISalesReturnService>();
            var receipts = services.GetRequiredService<IReceiptVoucherService>();
            var payments = services.GetRequiredService<IPaymentVoucherService>();

            foreach (var product in dead)
                Ok(purchases.Create(Purchase(suppliers[0].Id, branch, start.AddDays(2), new[] { (product, 40m) })));

            var invoiceCount = 0;
            for (var month = start; month <= today; month = month.AddMonths(1))
            {
                foreach (var (supplier, s) in suppliers.Select((x, s) => (x, s)))
                {
                    var date = Clamp(month.AddDays(s % 3), today);
                    var bought = Ok(purchases.Create(Purchase(supplier.Id, main, date,
                        selling.Where((_, i) => i % suppliers.Count == s).Select(p => (p, (decimal)_random.Next(8, 16))).ToArray())));
                    var payDate = date.AddDays(_random.Next(20, 50));
                    if (payDate <= today)
                        Ok(payments.Create(Voucher(supplier.Id, treasury, Math.Round(bought.NetTotal * (payDate < today.AddDays(-45) ? 1m : 0.6m), 2), payDate)));
                }

                for (var n = 0; n < 15; n++)
                {
                    var date = Clamp(month.AddDays(_random.Next(5, 28)), today);
                    var (customer, behaviour) = customers[_random.Next(customers.Count)];
                    var lines = selling.OrderBy(_ => _random.Next()).Take(_random.Next(1, 4)).Select(p => (p, (decimal)_random.Next(1, 6))).ToArray();
                    var created = sales.Create(new CreateSalesInvoiceDto
                    {
                        InvoiceDate = date, CustomerId = customer.Id, WarehouseId = main,
                        Lines = lines.Select(l => new CreateSalesInvoiceLineDto { ProductCode = l.p.Code, Qty = l.Item2, UnitPrice = l.p.SalePrice }).ToList()
                    });
                    if (created.IsFailure) { _log.WriteLine($"sale {date:yyyy-MM-dd} skipped: {created.ErrorMessage}"); continue; }
                    var invoice = created.Value;
                    invoiceCount++;

                    if (invoiceCount % 11 == 0)
                    {
                        var line = invoice.Lines.First();
                        Ok(returns.Create(new CreateSalesReturnDto
                        {
                            ReturnDate = Clamp(date.AddDays(5), today), CustomerId = customer.Id, WarehouseId = main,
                            Lines = { new CreateSalesReturnLineDto { ProductCode = line.ProductCode, Qty = 1, UnitPrice = line.UnitPrice,
                                SourceType = "SalesInvoices", SourceId = invoice.Id, SourceNo = invoice.InvoiceNo, SourceLineId = line.Id } }
                        }));
                    }

                    var (share, delay) = behaviour switch
                    {
                        0 => (1m, _random.Next(5, 25)),
                        1 => (_random.Next(0, 2) == 0 ? 0.5m : 1m, _random.Next(40, 110)),
                        _ => (_random.Next(0, 4) == 0 ? 0.3m : 0m, _random.Next(60, 200)),
                    };
                    var paid = date.AddDays(delay);
                    if (share > 0 && paid <= today)
                        Ok(receipts.Create(Voucher(customer.Id, treasury, Math.Round(invoice.NetTotal * share, 2), paid)));
                }
            }

            var settings = services.GetRequiredService<ISettingsService>();
            if (settings.Get(SettingKeys.HR.PayrollTreasury, 0) == 0) settings.Set(SettingKeys.HR.PayrollTreasury, treasury);
            People(services, start, today);
            _log.WriteLine($"sales invoices: {invoiceCount}");
        }

        private void People(IServiceProvider services, DateTime start, DateTime today)
        {
            var departments = services.GetRequiredService<Lookup<Department>>();
            var ids = new[] { "المبيعات", "المخازن", "الإدارة" }.Select(d => departments.Add(d)).ToList();
            var employees = services.GetRequiredService<IEmployeeService>();
            for (var i = 1; i <= 10; i++)
                Ok(employees.Create(new Employee
                {
                    Name = $"موظف تجريبي {i:00}", DepartmentId = ids[i % ids.Count], HireDate = start.AddMonths(-_random.Next(1, 36)).AddDays(i),
                    BasicSalary = _random.Next(4, 11) * 500, Status = EmployeeStatus.Active
                }));

            var payroll = services.GetRequiredService<IPayrollService>();
            for (var month = start; month.AddMonths(1) <= today; month = month.AddMonths(1))
            {
                var open = payroll.Open(month.Month, month.Year);
                if (open.IsFailure) { _log.WriteLine($"payroll {month:yyyy-MM}: {open.ErrorMessage}"); continue; }
                var dto = open.Value;
                dto.PaymentDate = month.AddMonths(1).AddDays(-1);
                var created = payroll.Create(dto);
                if (created.IsFailure) _log.WriteLine($"payroll {month:yyyy-MM}: {created.ErrorMessage}");
            }
        }

        /// <summary>رأس مالٍ افتتاحي في الخزينة حتى لا يرفض حارس النقدية السداد</summary>
        private void Capital(IServiceProvider services, int treasuryId, DateTime date)
        {
            var account = services.GetRequiredService<ITreasuryService>().GetUsable().Value.First(t => t.Id == treasuryId).AccountCode;
            var journal = services.GetRequiredService<IJournalService>();
            Ok(journal.Create(new PrimeERP.Application.DTOs.Accounting.CreateJournalDto
            {
                EntryDate = date, Description = "رأس مال افتتاحي",
                Lines =
                {
                    new() { LineNo = 1, AccountCode = account, Debit = 3_000_000 },
                    new() { LineNo = 2, AccountCode = "3101", Credit = 3_000_000 },
                }
            }));
        }

        private int Treasury(IServiceProvider services)
        {
            var usable = services.GetRequiredService<ITreasuryService>().GetUsable();
            if (usable.IsSuccess && usable.Value.Any()) return usable.Value.First().Id;
            var parentId = services.GetRequiredService<PrimeERP.Data.Repositories.IAccountRepository>().GetByCode("12").Id;
            var account = Ok(services.GetRequiredService<IAccountService>().Create(new PrimeERP.Application.DTOs.Accounting.CreateAccountDto { ParentId = parentId, Name = "الخزينة الرئيسية", IsLeaf = true }));
            return Ok(services.GetRequiredService<ITreasuryService>().Create(new Treasury { Name = "الخزينة الرئيسية", AccountCode = account.Code })).Id;
        }

        private static CreatePurchaseInvoiceDto Purchase(int supplierId, int warehouseId, DateTime date, (Product Product, decimal Qty)[] lines) => new()
        {
            InvoiceDate = date, SupplierId = supplierId, WarehouseId = warehouseId,
            Lines = lines.Select(l => new CreatePurchaseInvoiceLineDto { ProductCode = l.Product.Code, Qty = l.Qty, UnitPrice = l.Product.CostPrice }).ToList()
        };

        private static CreateVoucherDto Voucher(int partyId, int treasuryId, decimal amount, DateTime date) => new()
        {
            VoucherDate = date, PartyId = partyId, TreasuryId = treasuryId, Amount = amount, Method = (int)PaymentMethod.Cash
        };

        private static DateTime Clamp(DateTime date, DateTime max) => date > max ? max : date;

        private T Ok<T>(Result<T> result)
        {
            if (result.IsFailure) throw new InvalidOperationException(result.ErrorMessage);
            return result.Value;
        }
    }
}
