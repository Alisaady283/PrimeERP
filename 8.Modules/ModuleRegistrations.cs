using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Services.Assets;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.DTOs.Security;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.Services.HR;
using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Application.Services.Purchasing;
using PrimeERP.Application.Services.Sales;
using PrimeERP.Application.Services.Security;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Print;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Tree;
using PrimeERP.UI.ViewModels;

namespace PrimeERP.Modules
{
    /// <summary>يسجّل كل وحدة عمل فعلية في IModuleRegistry — إثبات شرط إغلاق R8: وحدتان بالتكوين، الثانية
    /// (Suppliers) أقصر من الأولى (Customers) لأنها تعيد استخدام نفس التعريف بالكامل. Accounts (R9) تثبت
    /// أن التكوين يمتد لوحدة هرمية (ParentId/Level) بلا أي تعديل في العقد.</summary>
    public static class ModuleRegistrations
    {
        public static void RegisterAll(IModuleRegistry registry)
        {
            registry.Register(new ModuleDefinition
            {
                Key = "Customers",
                TitleKey = "Str.Module.Customers",
                PermissionPrefix = "Customers",
                ViewModelType = typeof(CustomersViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(CustomerDto.Id), Width = 70, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(CustomerDto.Name), Width = 220, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Category"), Binding = nameof(CustomerDto.CategoryName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.Phone"), Binding = nameof(CustomerDto.Phone), Width = 130 },
                    new() { Header = LocalizationService.Get("Str.CreditLimit"), Binding = nameof(CustomerDto.CreditLimit), Width = 120, Align = ColumnAlign.Center, Format = "N2", PermissionKey = PermissionKeys.Customers.ColumnCreditLimit },
                    new() { Header = LocalizationService.Get("Str.Balance"), Binding = nameof(CustomerDto.Balance), Width = 120, Align = ColumnAlign.Center, Format = "N2", Footer = FooterAggregate.Sum },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Customers.Add",
                    TitleEditKey = "Str.Customers.Edit",
                    GridColumns = 2,
                    ServiceType = typeof(ICustomerService),
                    CreateDtoType = typeof(CreateCustomerDto),
                    UpdateDtoType = typeof(UpdateCustomerDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateCustomerDto.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(CreateCustomerDto.Phone), LabelKey = "Str.Phone", Kind = FieldKind.Text, MaxLength = 30 },
                        new() { Key = nameof(CreateCustomerDto.Email), LabelKey = "Str.Email", Kind = FieldKind.Text, MaxLength = 150 },
                        new() { Key = nameof(CreateCustomerDto.CreditLimit), LabelKey = "Str.CreditLimit", Kind = FieldKind.Number },
                        new() { Key = nameof(CreateCustomerDto.CategoryId), LabelKey = "Str.Category", Kind = FieldKind.Picker, PickerType = "Category", PickerCategoryModuleKey = "Customers" },
                        new() { Key = nameof(CreateCustomerDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }.Concat(StandardFields.DialogFields()).ToList()
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Suppliers",
                TitleKey = "Str.Module.Suppliers",
                PermissionPrefix = "Suppliers",
                ViewModelType = typeof(SuppliersViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(SupplierDto.Id), Width = 70, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(SupplierDto.Name), Width = 220, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Category"), Binding = nameof(SupplierDto.CategoryName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.Phone"), Binding = nameof(SupplierDto.Phone), Width = 130 },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Suppliers.Add",
                    TitleEditKey = "Str.Suppliers.Edit",
                    GridColumns = 2,
                    ServiceType = typeof(ISupplierService),
                    CreateDtoType = typeof(CreateSupplierDto),
                    UpdateDtoType = typeof(UpdateSupplierDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateSupplierDto.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(CreateSupplierDto.Phone), LabelKey = "Str.Phone", Kind = FieldKind.Text, MaxLength = 30 },
                        new() { Key = nameof(CreateSupplierDto.Email), LabelKey = "Str.Email", Kind = FieldKind.Text, MaxLength = 150 },
                        new() { Key = nameof(CreateSupplierDto.CreditLimit), LabelKey = "Str.CreditLimit", Kind = FieldKind.Number },
                        new() { Key = nameof(CreateSupplierDto.CategoryId), LabelKey = "Str.Category", Kind = FieldKind.Picker, PickerType = "Category", PickerCategoryModuleKey = "Suppliers" },
                        new() { Key = nameof(CreateSupplierDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }.Concat(StandardFields.DialogFields()).ToList()
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Accounts",
                TitleKey = "Str.Module.Accounts",
                PermissionPrefix = "Accounts",
                ViewModelType = typeof(AccountsViewModel),
                LayoutKind = LayoutKind.TreeSplit,
                TreeOptions = new TreeLayoutOptions
                {
                    IdField = nameof(AccountDto.Id),
                    ParentIdField = nameof(AccountDto.ParentId),
                    CodeField = nameof(AccountDto.Code),
                    NameField = nameof(AccountDto.Name),
                    DisplayTemplate = "{Code} - {Name}",
                    ExtraInfoTemplate = "({Balance:N2})",
                    ExpandToLevel = 2,
                    LeafFlagField = nameof(AccountDto.IsLeaf),
                },
                // أعمدة لوحة التفاصيل (TreeSplit) — لا شبكة (Columns.Header فقط، لا Width/Align/Footer، غير
                // مُستهلَكة هنا؛ TreeRenderer.BuildDetailsPanel يقرأ Header/Binding/Format فقط).
                Columns = new List<GridColumn>
                {
                    new() { Header = LocalizationService.Get("Str.Code"),    Binding = nameof(AccountDto.Code) },
                    new() { Header = LocalizationService.Get("Str.Name"),    Binding = nameof(AccountDto.Name) },
                    new() { Header = LocalizationService.Get("Str.Level"),   Binding = nameof(AccountDto.Level) },
                    new() { Header = LocalizationService.Get("Str.Type"),    Binding = nameof(AccountDto.TypeName) },
                    new() { Header = LocalizationService.Get("Str.AcceptsEntries"), Binding = nameof(AccountDto.IsLeaf) },
                    new() { Header = LocalizationService.Get("Str.Balance"), Binding = nameof(AccountDto.Balance), Format = "N2", PermissionKey = PermissionKeys.Accounts.ColumnBalance },
                }.Concat(StandardFields.AuditColumns()).ToList(),
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Accounts.Add",
                    TitleEditKey = "Str.Accounts.Edit",
                    GridColumns = 1,
                    ServiceType = typeof(IAccountService),
                    CreateDtoType = typeof(CreateAccountDto),
                    UpdateDtoType = typeof(UpdateAccountDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateAccountDto.ParentId), LabelKey = "Str.ParentAccount", Kind = FieldKind.Picker, IsRequired = true, IsReadOnlyOnEdit = true, PickerType = "Account" },
                        new() { Key = nameof(CreateAccountDto.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(CreateAccountDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea },
                    }.Concat(StandardFields.DialogFields()).ToList()
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Journals",
                TitleKey = "Str.Module.Journals",
                // القيد يُنشأ مسودة، والكشوف والتقارير تقرأ المرحَّل وحده — فبلا هذا الزر لا يظهر القيد
                // اليدوي في أي مكان بينما تظهر قيود السندات (تُرحَّل تلقائياً عند إنشائها).
                RowActions = new()
                {
                    new()
                    {
                        Label = "ترحيل", Variant = "primary", PermissionKey = PermissionKeys.Journal.Post,
                        AppliesTo = item => item.GetType().GetProperty("IsPosted")?.GetValue(item) is false,
                        Execute = (services, item) => services.GetRequiredService<IJournalService>()
                            .Post((int)item.GetType().GetProperty("Id").GetValue(item))
                    }
                },
                PermissionPrefix = "Journal",
                ViewModelType = typeof(JournalsViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.EntryNo"), Binding = nameof(JournalEntryDto.EntryNo), Width = 110 },
                    new() { Header = LocalizationService.Get("Str.EntryDate"), Binding = nameof(JournalEntryDto.EntryDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Description"), Binding = nameof(JournalEntryDto.Description), Width = 260, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Debit"), Binding = nameof(JournalEntryDto.TotalDebit), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.Credit"), Binding = nameof(JournalEntryDto.TotalCredit), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.Status"), Binding = nameof(JournalEntryDto.StatusText), Width = 110, Align = ColumnAlign.Center },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    PrintTitle = "قيد يومية",
                    TitleKey = "Str.Journals.Add",
                    TitleEditKey = "Str.Journals.Edit",
                    ServiceType = typeof(IJournalService),
                    DtoType = typeof(CreateJournalDto),
                    LineDtoType = typeof(CreateJournalLineDto),
                    LinesPropertyName = nameof(CreateJournalDto.Lines),
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreateJournalDto.EntryDate), LabelKey = "Str.EntryDate", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateJournalDto.Description), LabelKey = "Str.Description", Kind = FieldKind.Text, IsRequired = true, MaxLength = 300 },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(CreateJournalLineDto.AccountCode), Header = LocalizationService.Get("Str.Account"), Kind = FieldKind.Picker, Width = 220, IsRequired = true, PickerType = "Account", PickerLeafOnly = true },
                        new() { Key = nameof(CreateJournalLineDto.Debit), Header = LocalizationService.Get("Str.Debit"), Kind = FieldKind.Number, Width = 110 },
                        new() { Key = nameof(CreateJournalLineDto.Credit), Header = LocalizationService.Get("Str.Credit"), Kind = FieldKind.Number, Width = 110 },
                        new() { Key = nameof(CreateJournalLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 180 },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Products",
                TitleKey = "Str.Module.Products",
                PermissionPrefix = "Products",
                ViewModelType = typeof(ProductsViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(ProductDto.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(ProductDto.Name), Width = 220, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Category"), Binding = nameof(ProductDto.CategoryName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.CostPrice"), Binding = nameof(ProductDto.CostPrice), Width = 110, Align = ColumnAlign.Center, Format = "N2", PermissionKey = PermissionKeys.Products.ColumnCostPrice },
                    new() { Header = LocalizationService.Get("Str.SalePrice"), Binding = nameof(ProductDto.SalePrice), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
                },
                Filters = new()
                {
                    new() { Key = nameof(ProductFilter.CategoryId), LabelKey = "Str.Category", PickerType = "Category", PickerCategoryModuleKey = "Products" },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Products.Add",
                    TitleEditKey = "Str.Products.Edit",
                    GridColumns = 2,
                    ServiceType = typeof(IProductService),
                    CreateDtoType = typeof(CreateProductDto),
                    UpdateDtoType = typeof(UpdateProductDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateProductDto.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(CreateProductDto.Barcode), LabelKey = "Str.Barcode", Kind = FieldKind.Text, MaxLength = 60 },
                        new() { Key = nameof(CreateProductDto.CostPrice), LabelKey = "Str.CostPrice", Kind = FieldKind.Number, IsRequired = true },
                        new() { Key = nameof(CreateProductDto.SalePrice), LabelKey = "Str.SalePrice", Kind = FieldKind.Number, IsRequired = true },
                        new() { Key = nameof(CreateProductDto.CategoryId), LabelKey = "Str.Category", Kind = FieldKind.Picker, PickerType = "Category", PickerCategoryModuleKey = "Products" },
                        new() { Key = nameof(CreateProductDto.BrandId), LabelKey = "Str.Brand", Kind = FieldKind.Picker, PickerType = "Category", PickerCategoryModuleKey = "Brands" },
                        new() { Key = nameof(CreateProductDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }.Concat(StandardFields.DialogFields()).ToList()
                }
            });

            // النمط 2 القائم على ModuleKey — سبع وحدات، فرق الأسطر فقط (لا خدمة/ViewModel/Dialog جديدة، كلها
            // تستدعي CategoryService/CategoryListViewModel/CategoryDialogFactory الموجودة).
            RegisterLookup(registry, "Categories", "Str.Module.Categories", "Categories.Add", "Categories.Edit", typeof(CategoriesLookupViewModel));
            RegisterLookup(registry, "Brands", "Str.Module.Brands", "Brands.Add", "Brands.Edit", typeof(BrandsViewModel));
            RegisterLookup(registry, "AssetCategories", "Str.Module.AssetCategories", "AssetCategories.Add", "AssetCategories.Edit", typeof(AssetCategoriesViewModel));

            // الأربعة أدناه على كيانات Domain مخصصة موجودة مسبقاً (لا جدول Category عام) — حقول/أعمدة مختلفة
            // لكل واحدة فتُسجَّل صراحة بدل RegisterLookup الموحّد.
            registry.Register(new ModuleDefinition
            {
                Key = "Departments", TitleKey = "Str.Module.Departments", PermissionPrefix = "Departments", ViewModelType = typeof(DepartmentsViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(DepartmentDto.Name), Width = 220, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Active"), Binding = nameof(DepartmentDto.IsActive), Width = 80, Align = ColumnAlign.Center },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Departments.Add", TitleEditKey = "Str.Departments.Edit", GridColumns = 1,
                    ServiceType = typeof(IDepartmentService), CreateDtoType = typeof(CreateDepartmentDto), UpdateDtoType = typeof(UpdateDepartmentDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateDepartmentDto.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(CreateDepartmentDto.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "JobTitles", TitleKey = "Str.Module.JobTitles", PermissionPrefix = "JobTitles", ViewModelType = typeof(JobTitlesViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(JobTitleDto.Name), Width = 220, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Active"), Binding = nameof(JobTitleDto.IsActive), Width = 80, Align = ColumnAlign.Center },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.JobTitles.Add", TitleEditKey = "Str.JobTitles.Edit", GridColumns = 1,
                    ServiceType = typeof(IJobTitleService), CreateDtoType = typeof(CreateJobTitleDto), UpdateDtoType = typeof(UpdateJobTitleDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateJobTitleDto.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(CreateJobTitleDto.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Units", TitleKey = "Str.Module.Units", PermissionPrefix = "Units", ViewModelType = typeof(UnitsViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(UnitDto.Name), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Symbol"), Binding = nameof(UnitDto.Symbol), Width = 100 },
                    new() { Header = LocalizationService.Get("Str.Active"), Binding = nameof(UnitDto.IsActive), Width = 80, Align = ColumnAlign.Center },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Units.Add", TitleEditKey = "Str.Units.Edit", GridColumns = 1,
                    ServiceType = typeof(IUnitService), CreateDtoType = typeof(CreateUnitDto), UpdateDtoType = typeof(UpdateUnitDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateUnitDto.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(CreateUnitDto.Symbol), LabelKey = "Str.Symbol", Kind = FieldKind.Text, MaxLength = 20 },
                        new() { Key = nameof(CreateUnitDto.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Warehouses", TitleKey = "Str.Module.Warehouses", PermissionPrefix = "Warehouses", ViewModelType = typeof(WarehousesViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(WarehouseDto.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(WarehouseDto.Name), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Location"), Binding = nameof(WarehouseDto.Location), Width = 160 },
                    new() { Header = LocalizationService.Get("Str.Active"), Binding = nameof(WarehouseDto.IsActive), Width = 80, Align = ColumnAlign.Center },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Warehouses.Add", TitleEditKey = "Str.Warehouses.Edit", GridColumns = 1,
                    ServiceType = typeof(IWarehouseService), CreateDtoType = typeof(CreateWarehouseDto), UpdateDtoType = typeof(UpdateWarehouseDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateWarehouseDto.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(CreateWarehouseDto.Location), LabelKey = "Str.Location", Kind = FieldKind.Text, MaxLength = 200 },
                        new() { Key = nameof(CreateWarehouseDto.ManagerName), LabelKey = "Str.ManagerName", Kind = FieldKind.Text, MaxLength = 150 },
                        new() { Key = nameof(CreateWarehouseDto.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Assets",
                TitleKey = "Str.Module.Assets",
                // الإهلاك معاملة كغيرها: تشغيلة واحدة تُنتج قيداً بكل الأصول المستحقّة حتى تاريخه.
                RowActions = new()
                {
                    new()
                    {
                        Label = "احتساب الإهلاك", Variant = "primary", PermissionKey = "Assets.Edit",
                        Execute = (services, _) => services
                            .GetRequiredService<PrimeERP.Application.Services.Assets.IAssetDepreciationService>()
                            .RunFor(DateTime.Today)
                    }
                },
                PermissionPrefix = "Assets",
                ViewModelType = typeof(AssetsViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(AssetDto.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(AssetDto.Name), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Category"), Binding = nameof(AssetDto.CategoryName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.PurchaseDate"), Binding = nameof(AssetDto.PurchaseDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.CurrentValue"), Binding = nameof(AssetDto.CurrentValue), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
                },
                Filters = new()
                {
                    new() { Key = nameof(AssetFilter.CategoryId), LabelKey = "Str.Category", PickerType = "Category", PickerCategoryModuleKey = "AssetCategories" },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Assets.Add",
                    TitleEditKey = "Str.Assets.Edit",
                    GridColumns = 2,
                    ServiceType = typeof(IAssetService),
                    CreateDtoType = typeof(CreateAssetDto),
                    UpdateDtoType = typeof(UpdateAssetDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateAssetDto.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(CreateAssetDto.CategoryId), LabelKey = "Str.Category", Kind = FieldKind.Picker, PickerType = "Category", PickerCategoryModuleKey = "AssetCategories" },
                        new() { Key = nameof(CreateAssetDto.PurchaseDate), LabelKey = "Str.PurchaseDate", Kind = FieldKind.Date },
                        new() { Key = nameof(CreateAssetDto.PurchaseCost), LabelKey = "Str.PurchaseCost", Kind = FieldKind.Number, IsRequired = true, Min = 0 },
                        new() { Key = nameof(CreateAssetDto.UsefulLifeYears), LabelKey = "العمر الإنتاجي (سنوات)", Kind = FieldKind.Number, Min = 0, Max = 100 },
                        new() { Key = nameof(CreateAssetDto.SalvageValue), LabelKey = "القيمة المتبقية", Kind = FieldKind.Number, Min = 0 },
                        new() { Key = nameof(CreateAssetDto.CurrentValue), LabelKey = "Str.CurrentValue", Kind = FieldKind.Number },
                        new() { Key = nameof(CreateAssetDto.Location), LabelKey = "Str.Location", Kind = FieldKind.Text, MaxLength = 200 },
                        new() { Key = nameof(CreateAssetDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }.Concat(StandardFields.DialogFields()).ToList()
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Employees",
                TitleKey = "Str.Module.Employees",
                PermissionPrefix = "HR",
                ViewModelType = typeof(EmployeesViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(EmployeeDto.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(EmployeeDto.Name), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Department"), Binding = nameof(EmployeeDto.DepartmentName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.JobTitle"), Binding = nameof(EmployeeDto.JobTitleName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.Phone"), Binding = nameof(EmployeeDto.Phone), Width = 120 },
                    new() { Header = LocalizationService.Get("Str.Salary"), Binding = nameof(EmployeeDto.BasicSalary), Width = 110, Align = ColumnAlign.Center, Format = "N2", PermissionKey = PermissionKeys.HR.ColumnSalary },
                },
                Filters = new()
                {
                    new() { Key = nameof(EmployeeFilter.DepartmentId), LabelKey = "Str.Department", PickerType = "Department" },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Employees.Add",
                    TitleEditKey = "Str.Employees.Edit",
                    GridColumns = 2,
                    ServiceType = typeof(IEmployeeService),
                    CreateDtoType = typeof(CreateEmployeeDto),
                    UpdateDtoType = typeof(UpdateEmployeeDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateEmployeeDto.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(CreateEmployeeDto.DepartmentId), LabelKey = "Str.Department", Kind = FieldKind.Picker, PickerType = "Department" },
                        new() { Key = nameof(CreateEmployeeDto.JobTitleId), LabelKey = "Str.JobTitle", Kind = FieldKind.Picker, PickerType = "JobTitle" },
                        new() { Key = nameof(CreateEmployeeDto.Phone), LabelKey = "Str.Phone", Kind = FieldKind.Text, MaxLength = 30 },
                        new() { Key = nameof(CreateEmployeeDto.Email), LabelKey = "Str.Email", Kind = FieldKind.Text, MaxLength = 120 },
                        new() { Key = nameof(CreateEmployeeDto.HireDate), LabelKey = "Str.HireDate", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateEmployeeDto.BasicSalary), LabelKey = "Str.Salary", Kind = FieldKind.Number },
                        new() { Key = nameof(CreateEmployeeDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }.Concat(StandardFields.DialogFields()).ToList()
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Roles", TitleKey = "Str.Module.Roles", PermissionPrefix = "Users", ViewModelType = typeof(RolesViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(RoleDto.NameAr), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.RoleNameEn"), Binding = nameof(RoleDto.Name), Width = 160 },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Roles.Add", TitleEditKey = "Str.Roles.Edit", GridColumns = 1,
                    ServiceType = typeof(IRoleService), CreateDtoType = typeof(CreateRoleDto), UpdateDtoType = typeof(UpdateRoleDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateRoleDto.NameAr), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 100 },
                        new() { Key = nameof(CreateRoleDto.Name), LabelKey = "Str.RoleNameEn", Kind = FieldKind.Text, IsRequired = true, MaxLength = 100 },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Users", TitleKey = "Str.Module.Users", PermissionPrefix = "Users", ViewModelType = typeof(UsersViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Username"), Binding = nameof(UserDto.Username), Width = 130 },
                    new() { Header = LocalizationService.Get("Str.DisplayName"), Binding = nameof(UserDto.DisplayName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Role"), Binding = nameof(UserDto.RoleName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.Status"), Binding = nameof(UserDto.StatusText), Width = 90, Align = ColumnAlign.Center },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Users.Add", TitleEditKey = "Str.Users.Edit", GridColumns = 1,
                    ServiceType = typeof(IUserService), CreateDtoType = typeof(CreateUserDto), UpdateDtoType = typeof(UpdateUserDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateUserDto.Username), LabelKey = "Str.Username", Kind = FieldKind.Text, IsRequired = true, MaxLength = 100, IsReadOnlyOnEdit = true },
                        new() { Key = nameof(CreateUserDto.DisplayName), LabelKey = "Str.DisplayName", Kind = FieldKind.Text, IsRequired = true, MaxLength = 150 },
                        new() { Key = nameof(CreateUserDto.RoleId), LabelKey = "Str.Role", Kind = FieldKind.Picker, PickerType = "Role", IsRequired = true },
                        new() { Key = nameof(CreateUserDto.Password), LabelKey = "Str.Password", Kind = FieldKind.Password, IsRequired = true },
                        new() { Key = nameof(CreateUserDto.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "SalesInvoices", TitleKey = "Str.Module.SalesInvoices", PermissionPrefix = "Sales", ViewModelType = typeof(SalesInvoicesViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.InvoiceNo"), Binding = nameof(SalesInvoiceDto.InvoiceNo), Width = 110 },
                    new() { Header = LocalizationService.Get("Str.InvoiceDate"), Binding = nameof(SalesInvoiceDto.InvoiceDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Customer"), Binding = nameof(SalesInvoiceDto.CustomerName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.NetTotal"), Binding = nameof(SalesInvoiceDto.NetTotal), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.Status"), Binding = nameof(SalesInvoiceDto.StatusText), Width = 100, Align = ColumnAlign.Center },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    PrintTitle = "فاتورة مبيعات",
                    PrintColumns = TradePaper.Columns(),
                    PrintTotals = TradePaper.Totals(),
                    LineMath = TradePaper.LineMath(),
                    TitleKey = "Str.SalesInvoices.Add", TitleEditKey = "Str.SalesInvoices.Edit",
                    ServiceType = typeof(ISalesInvoiceService), DtoType = typeof(CreateSalesInvoiceDto), LineDtoType = typeof(CreateSalesInvoiceLineDto),
                    LinesPropertyName = nameof(CreateSalesInvoiceDto.Lines),
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreateSalesInvoiceDto.InvoiceDate), LabelKey = "Str.InvoiceDate", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateSalesInvoiceDto.CustomerId), LabelKey = "Str.Customer", Kind = FieldKind.Picker, PickerType = "Customer", IsRequired = true },
                        new() { Key = nameof(CreateSalesInvoiceDto.WarehouseId), LabelKey = "Str.Warehouse", Kind = FieldKind.Picker, PickerType = "Warehouse", IsRequired = true, FlowScope = FlowScope.SimplifiedOnly },
                        new() { Key = nameof(CreateSalesInvoiceDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300 },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(CreateSalesInvoiceLineDto.ProductCode), Header = LocalizationService.Get("Str.Product"), Kind = FieldKind.Picker, Width = 220, IsRequired = true, PickerType = "Product" },
                        new() { Key = nameof(CreateSalesInvoiceLineDto.Qty), Header = LocalizationService.Get("Str.Qty"), Kind = FieldKind.Number, Width = 90, IsRequired = true },
                        new() { Key = nameof(CreateSalesInvoiceLineDto.UnitPrice), Header = LocalizationService.Get("Str.UnitPrice"), Kind = FieldKind.Number, Width = 100, IsRequired = true },
                        new() { Key = nameof(CreateSalesInvoiceLineDto.DiscountPercent), Header = "خصم %", Kind = FieldKind.Number, Width = 70 },
                        new() { Key = nameof(CreateSalesInvoiceLineDto.VatPercent), Header = "ق.مضافة %", Kind = FieldKind.Number, Width = 80 },
                        new() { Key = nameof(CreateSalesInvoiceLineDto.WithholdingPercent), Header = "خ.إضافة %", Kind = FieldKind.Number, Width = 80 },
                        TradePaper.NetColumn(),
                        new() { Key = nameof(CreateSalesInvoiceLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 160 },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "PurchaseInvoices", TitleKey = "Str.Module.PurchaseInvoices", PermissionPrefix = "Purchases", ViewModelType = typeof(PurchaseInvoicesViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.InvoiceNo"), Binding = nameof(PurchaseInvoiceDto.InvoiceNo), Width = 110 },
                    new() { Header = LocalizationService.Get("Str.InvoiceDate"), Binding = nameof(PurchaseInvoiceDto.InvoiceDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Supplier"), Binding = nameof(PurchaseInvoiceDto.SupplierName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.NetTotal"), Binding = nameof(PurchaseInvoiceDto.NetTotal), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.Status"), Binding = nameof(PurchaseInvoiceDto.StatusText), Width = 100, Align = ColumnAlign.Center },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    PrintTitle = "فاتورة مشتريات",
                    PrintColumns = TradePaper.Columns(),
                    PrintTotals = TradePaper.Totals(),
                    LineMath = TradePaper.LineMath(),
                    TitleKey = "Str.PurchaseInvoices.Add", TitleEditKey = "Str.PurchaseInvoices.Edit",
                    ServiceType = typeof(IPurchaseInvoiceService), DtoType = typeof(CreatePurchaseInvoiceDto), LineDtoType = typeof(CreatePurchaseInvoiceLineDto),
                    LinesPropertyName = nameof(CreatePurchaseInvoiceDto.Lines),
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreatePurchaseInvoiceDto.InvoiceDate), LabelKey = "Str.InvoiceDate", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreatePurchaseInvoiceDto.SupplierId), LabelKey = "Str.Supplier", Kind = FieldKind.Picker, PickerType = "Supplier", IsRequired = true },
                        new() { Key = nameof(CreatePurchaseInvoiceDto.WarehouseId), LabelKey = "Str.Warehouse", Kind = FieldKind.Picker, PickerType = "Warehouse", IsRequired = true, FlowScope = FlowScope.SimplifiedOnly },
                        new() { Key = nameof(CreatePurchaseInvoiceDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300 },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(CreatePurchaseInvoiceLineDto.ProductCode), Header = LocalizationService.Get("Str.Product"), Kind = FieldKind.Picker, Width = 220, IsRequired = true, PickerType = "Product" },
                        new() { Key = nameof(CreatePurchaseInvoiceLineDto.Qty), Header = LocalizationService.Get("Str.Qty"), Kind = FieldKind.Number, Width = 90, IsRequired = true },
                        new() { Key = nameof(CreatePurchaseInvoiceLineDto.UnitPrice), Header = LocalizationService.Get("Str.UnitPrice"), Kind = FieldKind.Number, Width = 100, IsRequired = true },
                        new() { Key = nameof(CreatePurchaseInvoiceLineDto.DiscountPercent), Header = "خصم %", Kind = FieldKind.Number, Width = 70 },
                        new() { Key = nameof(CreatePurchaseInvoiceLineDto.VatPercent), Header = "ق.مضافة %", Kind = FieldKind.Number, Width = 80 },
                        new() { Key = nameof(CreatePurchaseInvoiceLineDto.WithholdingPercent), Header = "خ.إضافة %", Kind = FieldKind.Number, Width = 80 },
                        TradePaper.NetColumn(),
                        new() { Key = nameof(CreatePurchaseInvoiceLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 160 },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "SalesReturns", TitleKey = "Str.Module.SalesReturns", PermissionPrefix = "Sales", ViewModelType = typeof(SalesReturnsViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.InvoiceNo"), Binding = nameof(SalesReturnDto.ReturnNo), Width = 110 },
                    new() { Header = LocalizationService.Get("Str.InvoiceDate"), Binding = nameof(SalesReturnDto.ReturnDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Customer"), Binding = nameof(SalesReturnDto.CustomerName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.NetTotal"), Binding = nameof(SalesReturnDto.NetTotal), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    PrintTitle = "مرتجع مبيعات",
                    PrintColumns = TradePaper.Columns(),
                    PrintTotals = TradePaper.Totals(),
                    LineMath = TradePaper.LineMath(),
                    TitleKey = "Str.SalesReturns.Add", TitleEditKey = "Str.SalesReturns.Edit",
                    ServiceType = typeof(ISalesReturnService), DtoType = typeof(CreateSalesReturnDto), LineDtoType = typeof(CreateSalesReturnLineDto),
                    LinesPropertyName = nameof(CreateSalesReturnDto.Lines),
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreateSalesReturnDto.ReturnDate), LabelKey = "Str.InvoiceDate", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateSalesReturnDto.CustomerId), LabelKey = "Str.Customer", Kind = FieldKind.Picker, PickerType = "Customer", IsRequired = true },
                        new() { Key = nameof(CreateSalesReturnDto.WarehouseId), LabelKey = "Str.Warehouse", Kind = FieldKind.Picker, PickerType = "Warehouse", IsRequired = true, FlowScope = FlowScope.SimplifiedOnly },
                        new() { Key = nameof(CreateSalesReturnDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300 },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(CreateSalesReturnLineDto.ProductCode), Header = LocalizationService.Get("Str.Product"), Kind = FieldKind.Picker, Width = 220, IsRequired = true, PickerType = "Product" },
                        new() { Key = nameof(CreateSalesReturnLineDto.Qty), Header = LocalizationService.Get("Str.Qty"), Kind = FieldKind.Number, Width = 90, IsRequired = true },
                        new() { Key = nameof(CreateSalesReturnLineDto.UnitPrice), Header = LocalizationService.Get("Str.UnitPrice"), Kind = FieldKind.Number, Width = 100, IsRequired = true },
                        new() { Key = nameof(CreateSalesReturnLineDto.DiscountPercent), Header = "خصم %", Kind = FieldKind.Number, Width = 70 },
                        new() { Key = nameof(CreateSalesReturnLineDto.VatPercent), Header = "ق.مضافة %", Kind = FieldKind.Number, Width = 80 },
                        new() { Key = nameof(CreateSalesReturnLineDto.WithholdingPercent), Header = "خ.إضافة %", Kind = FieldKind.Number, Width = 80 },
                        TradePaper.NetColumn(),
                        new() { Key = nameof(CreateSalesReturnLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 160 },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "PurchaseReturns", TitleKey = "Str.Module.PurchaseReturns", PermissionPrefix = "Purchases", ViewModelType = typeof(PurchaseReturnsViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.InvoiceNo"), Binding = nameof(PurchaseReturnDto.ReturnNo), Width = 110 },
                    new() { Header = LocalizationService.Get("Str.InvoiceDate"), Binding = nameof(PurchaseReturnDto.ReturnDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Supplier"), Binding = nameof(PurchaseReturnDto.SupplierName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.NetTotal"), Binding = nameof(PurchaseReturnDto.NetTotal), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    PrintTitle = "مرتجع مشتريات",
                    PrintColumns = TradePaper.Columns(),
                    PrintTotals = TradePaper.Totals(),
                    LineMath = TradePaper.LineMath(),
                    TitleKey = "Str.PurchaseReturns.Add", TitleEditKey = "Str.PurchaseReturns.Edit",
                    ServiceType = typeof(IPurchaseReturnService), DtoType = typeof(CreatePurchaseReturnDto), LineDtoType = typeof(CreatePurchaseReturnLineDto),
                    LinesPropertyName = nameof(CreatePurchaseReturnDto.Lines),
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreatePurchaseReturnDto.ReturnDate), LabelKey = "Str.InvoiceDate", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreatePurchaseReturnDto.SupplierId), LabelKey = "Str.Supplier", Kind = FieldKind.Picker, PickerType = "Supplier", IsRequired = true },
                        new() { Key = nameof(CreatePurchaseReturnDto.WarehouseId), LabelKey = "Str.Warehouse", Kind = FieldKind.Picker, PickerType = "Warehouse", IsRequired = true, FlowScope = FlowScope.SimplifiedOnly },
                        new() { Key = nameof(CreatePurchaseReturnDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300 },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(CreatePurchaseReturnLineDto.ProductCode), Header = LocalizationService.Get("Str.Product"), Kind = FieldKind.Picker, Width = 220, IsRequired = true, PickerType = "Product" },
                        new() { Key = nameof(CreatePurchaseReturnLineDto.Qty), Header = LocalizationService.Get("Str.Qty"), Kind = FieldKind.Number, Width = 90, IsRequired = true },
                        new() { Key = nameof(CreatePurchaseReturnLineDto.UnitPrice), Header = LocalizationService.Get("Str.UnitPrice"), Kind = FieldKind.Number, Width = 100, IsRequired = true },
                        new() { Key = nameof(CreatePurchaseReturnLineDto.DiscountPercent), Header = "خصم %", Kind = FieldKind.Number, Width = 70 },
                        new() { Key = nameof(CreatePurchaseReturnLineDto.VatPercent), Header = "ق.مضافة %", Kind = FieldKind.Number, Width = 80 },
                        new() { Key = nameof(CreatePurchaseReturnLineDto.WithholdingPercent), Header = "خ.إضافة %", Kind = FieldKind.Number, Width = 80 },
                        TradePaper.NetColumn(),
                        new() { Key = nameof(CreatePurchaseReturnLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 160 },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "StockIn", TitleKey = "Str.Module.StockIn", PermissionPrefix = "Inventory", ViewModelType = typeof(StockInViewModel),
                FlowScope = FlowScope.SimplifiedOnly,
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.DocNo"), Binding = nameof(StockAdjustmentDto.DocNo), Width = 110 },
                    new() { Header = LocalizationService.Get("Str.InvoiceDate"), Binding = nameof(StockAdjustmentDto.MovementDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Warehouse"), Binding = nameof(StockAdjustmentDto.WarehouseName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Qty"), Binding = nameof(StockAdjustmentDto.TotalQty), Width = 100, Align = ColumnAlign.Center, Format = "N2" },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    PrintTitle = "إذن إضافة مخزني",
                    TitleKey = "Str.StockIn.Add", TitleEditKey = "Str.StockIn.Edit",
                    ServiceType = typeof(IStockInService), DtoType = typeof(CreateStockAdjustmentDto), LineDtoType = typeof(CreateStockAdjustmentLineDto),
                    LinesPropertyName = nameof(CreateStockAdjustmentDto.Lines),
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreateStockAdjustmentDto.MovementDate), LabelKey = "Str.InvoiceDate", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateStockAdjustmentDto.WarehouseId), LabelKey = "Str.Warehouse", Kind = FieldKind.Picker, PickerType = "Warehouse", IsRequired = true },
                        new() { Key = nameof(CreateStockAdjustmentDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300 },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(CreateStockAdjustmentLineDto.ProductCode), Header = LocalizationService.Get("Str.Product"), Kind = FieldKind.Picker, Width = 220, IsRequired = true, PickerType = "Product" },
                        new() { Key = nameof(CreateStockAdjustmentLineDto.Qty), Header = LocalizationService.Get("Str.Qty"), Kind = FieldKind.Number, Width = 90, IsRequired = true },
                        new() { Key = nameof(CreateStockAdjustmentLineDto.UnitCost), Header = LocalizationService.Get("Str.UnitCost"), Kind = FieldKind.Number, Width = 100 },
                        new() { Key = nameof(CreateStockAdjustmentLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 160 },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "StockOut", TitleKey = "Str.Module.StockOut", PermissionPrefix = "Inventory", ViewModelType = typeof(StockOutViewModel),
                FlowScope = FlowScope.SimplifiedOnly,
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.DocNo"), Binding = nameof(StockAdjustmentDto.DocNo), Width = 110 },
                    new() { Header = LocalizationService.Get("Str.InvoiceDate"), Binding = nameof(StockAdjustmentDto.MovementDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Warehouse"), Binding = nameof(StockAdjustmentDto.WarehouseName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Qty"), Binding = nameof(StockAdjustmentDto.TotalQty), Width = 100, Align = ColumnAlign.Center, Format = "N2" },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    PrintTitle = "إذن صرف مخزني",
                    TitleKey = "Str.StockOut.Add", TitleEditKey = "Str.StockOut.Edit",
                    ServiceType = typeof(IStockOutService), DtoType = typeof(CreateStockAdjustmentDto), LineDtoType = typeof(CreateStockAdjustmentLineDto),
                    LinesPropertyName = nameof(CreateStockAdjustmentDto.Lines),
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreateStockAdjustmentDto.MovementDate), LabelKey = "Str.InvoiceDate", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateStockAdjustmentDto.WarehouseId), LabelKey = "Str.Warehouse", Kind = FieldKind.Picker, PickerType = "Warehouse", IsRequired = true },
                        new() { Key = nameof(CreateStockAdjustmentDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300 },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(CreateStockAdjustmentLineDto.ProductCode), Header = LocalizationService.Get("Str.Product"), Kind = FieldKind.Picker, Width = 220, IsRequired = true, PickerType = "Product" },
                        new() { Key = nameof(CreateStockAdjustmentLineDto.Qty), Header = LocalizationService.Get("Str.Qty"), Kind = FieldKind.Number, Width = 90, IsRequired = true },
                        new() { Key = nameof(CreateStockAdjustmentLineDto.UnitCost), Header = LocalizationService.Get("Str.UnitCost"), Kind = FieldKind.Number, Width = 100 },
                        new() { Key = nameof(CreateStockAdjustmentLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 160 },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "StockTransfer", TitleKey = "Str.Module.StockTransfer", PermissionPrefix = "Inventory", ViewModelType = typeof(StockTransferViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.DocNo"), Binding = nameof(StockTransferDto.DocNo), Width = 110 },
                    new() { Header = LocalizationService.Get("Str.InvoiceDate"), Binding = nameof(StockTransferDto.MovementDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.FromWarehouse"), Binding = nameof(StockTransferDto.FromWarehouseName), Width = 160 },
                    new() { Header = LocalizationService.Get("Str.ToWarehouse"), Binding = nameof(StockTransferDto.ToWarehouseName), Width = 160, IsStarWidth = true },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    PrintTitle = "إذن تحويل مخزني",
                    TitleKey = "Str.StockTransfer.Add", TitleEditKey = "Str.StockTransfer.Edit",
                    ServiceType = typeof(IStockTransferService), DtoType = typeof(CreateStockTransferDto), LineDtoType = typeof(CreateStockTransferLineDto),
                    LinesPropertyName = nameof(CreateStockTransferDto.Lines),
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreateStockTransferDto.MovementDate), LabelKey = "Str.InvoiceDate", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateStockTransferDto.FromWarehouseId), LabelKey = "Str.FromWarehouse", Kind = FieldKind.Picker, PickerType = "Warehouse", IsRequired = true },
                        new() { Key = nameof(CreateStockTransferDto.ToWarehouseId), LabelKey = "Str.ToWarehouse", Kind = FieldKind.Picker, PickerType = "Warehouse", IsRequired = true },
                        new() { Key = nameof(CreateStockTransferDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300 },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(CreateStockTransferLineDto.ProductCode), Header = LocalizationService.Get("Str.Product"), Kind = FieldKind.Picker, Width = 220, IsRequired = true, PickerType = "Product" },
                        new() { Key = nameof(CreateStockTransferLineDto.Qty), Header = LocalizationService.Get("Str.Qty"), Kind = FieldKind.Number, Width = 90, IsRequired = true },
                        new() { Key = nameof(CreateStockTransferLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 160 },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Payroll", TitleKey = "Str.Module.Payroll", PermissionPrefix = "HR", ViewModelType = typeof(PayrollViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.DocNo"), Binding = nameof(PayrollDto.PayrollNo), Width = 110 },
                    new() { Header = LocalizationService.Get("Str.PaymentDate"), Binding = nameof(PayrollDto.PaymentDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.NetTotal"), Binding = nameof(PayrollDto.NetTotal), Width = 130, Align = ColumnAlign.Center, Format = "N2" },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    PrintTitle = "كشف رواتب",
                    TitleKey = "Str.Payroll.Add", TitleEditKey = "Str.Payroll.Edit",
                    ServiceType = typeof(IPayrollService), DtoType = typeof(CreatePayrollDto), LineDtoType = typeof(CreatePayrollLineDto),
                    LinesPropertyName = nameof(CreatePayrollDto.Lines),
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreatePayrollDto.PeriodStart), LabelKey = "Str.PeriodStart", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreatePayrollDto.PeriodEnd), LabelKey = "Str.PeriodEnd", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreatePayrollDto.PaymentDate), LabelKey = "Str.PaymentDate", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreatePayrollDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300 },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(CreatePayrollLineDto.EmployeeCode), Header = LocalizationService.Get("Str.Employee"), Kind = FieldKind.Picker, Width = 200, IsRequired = true, PickerType = "Employee" },
                        new() { Key = nameof(CreatePayrollLineDto.BasicSalary), Header = LocalizationService.Get("Str.BasicSalary"), Kind = FieldKind.Number, Width = 110, IsRequired = true },
                        new() { Key = nameof(CreatePayrollLineDto.Allowances), Header = LocalizationService.Get("Str.Allowances"), Kind = FieldKind.Number, Width = 100 },
                        new() { Key = nameof(CreatePayrollLineDto.Deductions), Header = LocalizationService.Get("Str.Deductions"), Kind = FieldKind.Number, Width = 100 },
                        new() { Key = nameof(CreatePayrollLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 140 },
                    }
                }
            });

            // الأرصدة الافتتاحية: نفس محرِّر القيد بالضبط — الفرق أن الخدمة تضبط المصدر وتُلحق سطر الفرق.
            registry.Register(new ModuleDefinition
            {
                Key = "OpeningBalances", TitleKey = "Str.Module.OpeningBalances", PermissionPrefix = "Journal",
                ViewModelType = typeof(OpeningBalancesViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.EntryNo"), Binding = nameof(JournalEntryDto.EntryNo), Width = 130 },
                    new() { Header = LocalizationService.Get("Str.EntryDate"), Binding = nameof(JournalEntryDto.EntryDate), Width = 110 },
                    new() { Header = LocalizationService.Get("Str.Description"), Binding = nameof(JournalEntryDto.Description), Width = 300, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Debit"), Binding = nameof(JournalEntryDto.TotalDebit), Width = 120, Align = ColumnAlign.Center, Format = "N2", Footer = FooterAggregate.Sum },
                    new() { Header = LocalizationService.Get("Str.Status"), Binding = nameof(JournalEntryDto.StatusText), Width = 100, Align = ColumnAlign.Center },
                },
                RowActions = new()
                {
                    new()
                    {
                        Label = "ترحيل", Variant = "primary", PermissionKey = PermissionKeys.Journal.Post,
                        AppliesTo = item => item.GetType().GetProperty("IsPosted")?.GetValue(item) is false,
                        Execute = (services, item) => services.GetRequiredService<IJournalService>()
                            .Post((int)item.GetType().GetProperty("Id").GetValue(item))
                    }
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    PrintTitle = "قيد أرصدة افتتاحية",
                    TitleKey = "Str.Module.OpeningBalances", TitleEditKey = "Str.Module.OpeningBalances",
                    ServiceType = typeof(PrimeERP.Application.Services.Accounting.IOpeningBalanceService),
                    DtoType = typeof(CreateJournalDto), LineDtoType = typeof(CreateJournalLineDto),
                    LinesPropertyName = nameof(CreateJournalDto.Lines), DocumentKind = "OpeningBalances",
                    HeaderFields = new()
                    {
                        // التاريخ من إعداد بدء العمل لا من كتابة المستخدم — الخدمة تفرضه عند الحفظ.
                        new() { Key = nameof(CreateJournalDto.EntryDate), LabelKey = "Str.StartDate", Kind = FieldKind.Date, IsReadOnly = true },
                        new() { Key = nameof(CreateJournalDto.Description), LabelKey = "Str.Description", Kind = FieldKind.Text, IsRequired = true, MaxLength = 300 },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(CreateJournalLineDto.AccountCode), Header = LocalizationService.Get("Str.Account"), Kind = FieldKind.Picker, Width = 260, IsRequired = true, PickerType = "Account", PickerLeafOnly = true },
                        new() { Key = nameof(CreateJournalLineDto.Debit), Header = LocalizationService.Get("Str.Debit"), Kind = FieldKind.Number, Width = 120 },
                        new() { Key = nameof(CreateJournalLineDto.Credit), Header = LocalizationService.Get("Str.Credit"), Kind = FieldKind.Number, Width = 120 },
                        new() { Key = nameof(CreateJournalLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 160 },
                    }
                }
            });

            registry.Register(new ModuleDefinition { Key = "Settings", TitleKey = "Str.Module.Settings", PermissionPrefix = "Settings", LayoutKind = LayoutKind.Settings });
        }

        private static void RegisterLookup(IModuleRegistry registry, string moduleKey, string titleKey, string addKey, string editKey, Type viewModelType)
        {
            registry.Register(new ModuleDefinition
            {
                Key = moduleKey,
                TitleKey = titleKey,
                PermissionPrefix = moduleKey,
                ViewModelType = viewModelType,
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(CategoryDto.Name), Width = 220, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.ParentCategory"), Binding = nameof(CategoryDto.ParentName), Width = 160 },
                    new() { Header = LocalizationService.Get("Str.Active"), Binding = nameof(CategoryDto.IsActive), Width = 80, Align = ColumnAlign.Center },
                },
                Dialog = CategoryDialogFactory.Build(moduleKey, "Str." + addKey, "Str." + editKey)
            });
        }
    }
}
