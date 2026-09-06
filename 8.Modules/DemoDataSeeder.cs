using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Assets;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Services.HR;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Purchasing;
using PrimeERP.Application.Services.Sales;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Settings;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Modules
{
    // بيانات وهمية لتشغيل فعلي/تجربة يدوية — تستدعي الخدمات الحقيقية فقط (لا SQL مباشر هنا)، فكل ما يُزرع
    // يمرّ بنفس التحقق/الربط التلقائي/الترحيل الذي يمر منه أي إدخال يدوي حقيقي. بوابة واحدة عند البداية
    // (إن وُجد عملاء بالفعل) — لا يُعاد الزرع أبداً بعد أول تشغيل ناجح.
    public static class DemoDataSeeder
    {
        public static void Seed(IServiceProvider services)
        {
            var customers = services.GetRequiredService<ICustomerService>();
            if (customers.GetPaged(1, 1).Value.TotalCount > 0) return;

            var accounts = SeedPostingAccounts(services);
            var categoryIds = SeedProductCategories(services);
            var productCodes = SeedProducts(services, categoryIds);
            var warehouseIds = SeedWarehouses(services);
            SeedOpeningStock(services, productCodes, warehouseIds[0]);
            var customerIds = SeedCustomers(services);
            var supplierIds = SeedSuppliers(services);
            var (departmentIds, jobTitleIds) = SeedDepartmentsAndJobTitles(services);
            SeedEmployees(services, departmentIds, jobTitleIds);
            SeedBrandsUnitsAssets(services);
            SeedJournalEntries(services, accounts);
            SeedSalesInvoice(services, customerIds[0], warehouseIds[0], productCodes[0]);
            SeedPurchaseInvoice(services, supplierIds[0], warehouseIds[0], productCodes[1]);
        }

        // كل نداء زرع يفترض نجاح Result — بدل NullReferenceException غامض عند فشل صامت (صلاحية/تحقق)، رسالة
        // الخطأ الحقيقية من الخدمة نفسها تظهر فوراً.
        private static T Unwrap<T>(PrimeERP.Domain.Results.Result<T> result, string step)
        {
            if (!result.IsSuccess) throw new InvalidOperationException($"DemoDataSeeder فشل في {step}: {result.ErrorMessage}");
            return result.Value;
        }

        // الجذور الافتراضية (41/51/1201/21/12) IsLeaf=false — تُنشئ فرعاً حقيقياً تحت كل واحد وتُحدِّث
        // الإعدادات لتشير إليه، تماماً كما يفعل مسؤول النظام يدوياً أول مرة (نفس ما أثبتته اختبارات الفواتير).
        private static (string Sales, string COGS, string Inventory, string VATOutput, string VATInput) SeedPostingAccounts(IServiceProvider services)
        {
            var accountSvc = services.GetRequiredService<IAccountService>();
            var settings = services.GetRequiredService<ISettingsService>();

            string LeafUnder(string parentCode, string name)
            {
                var parent = Unwrap(accountSvc.GetByCode(parentCode), $"GetByCode({parentCode})");
                return Unwrap(accountSvc.Create(new CreateAccountDto { ParentId = parent.Id, Name = name, IsLeaf = true, SkipAutoLink = true }), $"Create account {name}").Code;
            }

            var sales = LeafUnder("41", "مبيعات بضائع");
            var cogs = LeafUnder("51", "تكلفة البضاعة المباعة");
            var inventory = LeafUnder("1201", "مخزون البضائع");
            var vatOutput = LeafUnder("21", "ضريبة مبيعات مستحقة");
            var vatInput = LeafUnder("12", "ضريبة مشتريات مستردة");
            var capital = LeafUnder("31", "رأس المال المدفوع");
            var adminExpense = LeafUnder("51", "مصروفات إدارية عمومية");

            settings.Set(SettingKeys.Accounts.Sales, sales);
            settings.Set(SettingKeys.Accounts.COGS, cogs);
            settings.Set(SettingKeys.Accounts.Inventory, inventory);
            settings.Set(SettingKeys.Accounts.VATOutput, vatOutput);
            settings.Set(SettingKeys.Accounts.VATInput, vatInput);
            // Accounts.Cash أصل تُنشأ تحته الخزائن (مثل Accounts.Customers) — ضبطه على ورقة يُعطّل الربط.

            return (sales, cogs, inventory, vatOutput, vatInput);
        }

        private static int[] SeedProductCategories(IServiceProvider services)
        {
            var categories = services.GetRequiredService<ICategoryService>();
            string[] names = { "فئة 1", "فئة 2" };
            return names.Select(n => Unwrap(categories.Create(new CreateCategoryDto { Name = n, ModuleKey = "Products" }), $"Create category {n}").Id).ToArray();
        }

        private static string[] SeedProducts(IServiceProvider services, int[] categoryIds)
        {
            var products = services.GetRequiredService<IProductService>();
            (string Name, decimal Cost, decimal Sale)[] items =
            {
                ("صنف 1", 10, 15), ("صنف 2", 20, 30),
            };

            return items.Select((it, i) => Unwrap(products.Create(new CreateProductDto
            {
                Name = it.Name, CategoryId = categoryIds[i % categoryIds.Length], CostPrice = it.Cost, SalePrice = it.Sale, IsActive = true
            }), $"Create product {it.Name}").Code).ToArray();
        }

        private static int[] SeedWarehouses(IServiceProvider services)
        {
            var warehouses = services.GetRequiredService<IWarehouseService>();
            return new[]
            {
                Unwrap(warehouses.Create(new CreateWarehouseDto { Name = "المخزن الرئيسي", Location = "القاهرة" }), "Create warehouse 1").Id,
                Unwrap(warehouses.Create(new CreateWarehouseDto { Name = "مخزن الفرع", Location = "الإسكندرية" }), "Create warehouse 2").Id,
            };
        }

        private static void SeedOpeningStock(IServiceProvider services, string[] productCodes, int warehouseId)
        {
            var products = services.GetRequiredService<IProductService>();
            var stock = services.GetRequiredService<IStockService>();

            // Product ليس له GetByCode على مستوى الخدمة (فقط المستودع) — نمرّ عبر GetPaged بدل فتح طبقة
            // البيانات من هنا.
            var all = products.GetPaged(1, 100).Value.Items;
            Db.RunTransaction((conn, tx) =>
            {
                foreach (var code in productCodes)
                {
                    var product = all.First(p => p.Code == code);
                    stock.RecordMovement(conn, tx, product.Id, warehouseId, MovementType.In, 100, product.CostPrice, "Opening", null, "OPEN-" + code);
                }
            });
        }

        private static int[] SeedCustomers(IServiceProvider services)
        {
            var customers = services.GetRequiredService<ICustomerService>();
            (string Name, string Phone, string City)[] items =
            {
                ("عميل 1", "01001234567", "القاهرة"), ("عميل 2", "01012345678", "الجيزة"),
            };
            return items.Select(it => Unwrap(customers.Create(new CreateCustomerDto { Name = it.Name, Phone = it.Phone, City = it.City, CreditLimit = 50000 }), $"Create customer {it.Name}").Id).ToArray();
        }

        private static int[] SeedSuppliers(IServiceProvider services)
        {
            var suppliers = services.GetRequiredService<ISupplierService>();
            (string Name, string Phone, string City)[] items =
            {
                ("مورد 1", "01111234567", "القاهرة"), ("مورد 2", "01122345678", "دمياط"),
            };
            return items.Select(it => Unwrap(suppliers.Create(new CreateSupplierDto { Name = it.Name, Phone = it.Phone, City = it.City, CreditLimit = 100000 }), $"Create supplier {it.Name}").Id).ToArray();
        }

        private static (int[] Departments, int[] JobTitles) SeedDepartmentsAndJobTitles(IServiceProvider services)
        {
            var departments = services.GetRequiredService<IDepartmentService>();
            var jobTitles = services.GetRequiredService<IJobTitleService>();

            var deptIds = new[] { "قسم 1", "قسم 2" }.Select(n => Unwrap(departments.Create(new CreateDepartmentDto { Name = n }), $"Create department {n}").Id).ToArray();
            var jobIds = new[] { "وظيفة 1", "وظيفة 2" }.Select(n => Unwrap(jobTitles.Create(new CreateJobTitleDto { Name = n }), $"Create job title {n}").Id).ToArray();
            return (deptIds, jobIds);
        }

        private static void SeedEmployees(IServiceProvider services, int[] departmentIds, int[] jobTitleIds)
        {
            var employees = services.GetRequiredService<IEmployeeService>();
            (string Name, int DeptIdx, int JobIdx, decimal Salary)[] items =
            {
                ("موظف 1", 0, 0, 6000), ("موظف 2", 1, 1, 7500),
            };

            foreach (var it in items)
                employees.Create(new CreateEmployeeDto
                {
                    Name = it.Name, DepartmentId = departmentIds[it.DeptIdx], JobTitleId = jobTitleIds[it.JobIdx],
                    HireDate = DateTime.Today.AddYears(-1), BasicSalary = it.Salary, IsActive = true
                });
        }

        private static void SeedBrandsUnitsAssets(IServiceProvider services)
        {
            var categories = services.GetRequiredService<ICategoryService>();
            new[] { "ماركة 1", "ماركة 2" }.ToList().ForEach(n => categories.Create(new CreateCategoryDto { Name = n, ModuleKey = "Brands" }));

            var units = services.GetRequiredService<IUnitService>();
            new (string, string)[] { ("وحدة 1", "U1"), ("وحدة 2", "U2") }
                .ToList().ForEach(u => units.Create(new CreateUnitDto { Name = u.Item1, Symbol = u.Item2 }));

            var assetCategoryId = Unwrap(categories.Create(new CreateCategoryDto { Name = "أجهزة حاسوب", ModuleKey = "AssetCategories" }), "Create asset category").Id;
            var assets = services.GetRequiredService<IAssetService>();
            assets.Create(new CreateAssetDto { Name = "سيرفر مكتبي", CategoryId = assetCategoryId, PurchaseDate = DateTime.Today.AddMonths(-6), PurchaseCost = 15000, CurrentValue = 13000, Location = "غرفة السيرفرات" });
            assets.Create(new CreateAssetDto { Name = "طابعة مكتبية", CategoryId = assetCategoryId, PurchaseDate = DateTime.Today.AddMonths(-3), PurchaseCost = 4000, CurrentValue = 3600, Location = "الاستقبال" });
        }

        private static void SeedJournalEntries(IServiceProvider services, (string Sales, string COGS, string Inventory, string VATOutput, string VATInput) accounts)
        {
            var journal = services.GetRequiredService<IJournalService>();
            var accountSvc = services.GetRequiredService<IAccountService>();

            // النقدية تأتي من خزينة فعلية لا من حساب ورقي مزروع مباشرة — الخزينة هي التي تملك حسابها.
            var treasurySvc = services.GetRequiredService<PrimeERP.Application.Services.Treasury.ITreasuryService>();
            treasurySvc.SeedDefaults();
            var cash = treasurySvc.GetAll().Value.First(t => t.Kind == PrimeERP.Domain.Enums.TreasuryKind.Cash).AccountCode;
            var capital = accountSvc.GetLeaves().Value.First(a => a.Name == "رأس المال المدفوع").Code;
            var adminExpense = accountSvc.GetLeaves().Value.First(a => a.Name == "مصروفات إدارية عمومية").Code;

            void Post(string description, params (string Code, decimal Debit, decimal Credit)[] lines)
            {
                var dto = new PrimeERP.Application.DTOs.Accounting.CreateJournalDto
                {
                    EntryDate = DateTime.Today, Description = description, Source = nameof(JournalSource.Manual),
                    Lines = lines.Select((l, i) => new CreateJournalLineDto { LineNo = i + 1, AccountCode = l.Code, Debit = l.Debit, Credit = l.Credit }).ToList()
                };
                var result = journal.Create(dto);
                if (result.IsSuccess) journal.Post(result.Value.Id);
            }

            Post("إيداع رأس مال افتتاحي", (cash, 100000, 0), (capital, 0, 100000));
            Post("سداد مصروفات إدارية نقداً", (adminExpense, 2500, 0), (cash, 0, 2500));
            Post("سحب نقدي لتغطية مصروفات متنوعة", (adminExpense, 1200, 0), (cash, 0, 1200));
        }

        private static void SeedSalesInvoice(IServiceProvider services, int customerId, int warehouseId, string productCode)
        {
            var products = services.GetRequiredService<IProductService>();
            var product = products.GetPaged(1, 100).Value.Items.First(p => p.Code == productCode);

            services.GetRequiredService<ISalesInvoiceService>().Create(new CreateSalesInvoiceDto
            {
                InvoiceDate = DateTime.Today, CustomerId = customerId, WarehouseId = warehouseId,
                Lines = { new CreateSalesInvoiceLineDto { LineNo = 1, ProductCode = productCode, Qty = 3, UnitPrice = product.SalePrice, VatPercent = 14 } }
            });
        }

        private static void SeedPurchaseInvoice(IServiceProvider services, int supplierId, int warehouseId, string productCode)
        {
            var products = services.GetRequiredService<IProductService>();
            var product = products.GetPaged(1, 100).Value.Items.First(p => p.Code == productCode);

            services.GetRequiredService<IPurchaseInvoiceService>().Create(new CreatePurchaseInvoiceDto
            {
                InvoiceDate = DateTime.Today, SupplierId = supplierId, WarehouseId = warehouseId,
                Lines = { new CreatePurchaseInvoiceLineDto { LineNo = 1, ProductCode = productCode, Qty = 20, UnitPrice = product.CostPrice, VatPercent = 14 } }
            });
        }
    }
}
