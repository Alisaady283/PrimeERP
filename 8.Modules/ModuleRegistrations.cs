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
using PrimeERP.Application.Services.HR;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Localization;
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
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(CustomerDto.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(CustomerDto.Name), Width = 220, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Phone"), Binding = nameof(CustomerDto.Phone), Width = 130 },
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
                        new() { Key = nameof(CreateCustomerDto.CategoryId), LabelKey = "Str.ParentCategory", Kind = FieldKind.Picker, PickerType = "Category", PickerCategoryModuleKey = "Customers" },
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
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(SupplierDto.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(SupplierDto.Name), Width = 220, IsStarWidth = true },
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
                        new() { Key = nameof(CreateSupplierDto.CategoryId), LabelKey = "Str.ParentCategory", Kind = FieldKind.Picker, PickerType = "Category", PickerCategoryModuleKey = "Suppliers" },
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
                    new() { Header = LocalizationService.Get("Str.Balance"), Binding = nameof(AccountDto.Balance), Format = "N2" },
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
                        new() { Key = nameof(CreateAccountDto.IsLeaf), LabelKey = "Str.AcceptsEntries", Kind = FieldKind.Check },
                        new() { Key = nameof(CreateAccountDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea },
                    }.Concat(StandardFields.DialogFields()).ToList()
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Journals",
                TitleKey = "Str.Module.Journals",
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
                    new() { Header = LocalizationService.Get("Str.SalePrice"), Binding = nameof(ProductDto.SalePrice), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.Category"), Binding = nameof(ProductDto.CategoryName), Width = 140 },
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
                        new() { Key = nameof(CreateProductDto.CategoryId), LabelKey = "Str.ParentCategory", Kind = FieldKind.Picker, PickerType = "Category", PickerCategoryModuleKey = "Products" },
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
                        new() { Key = nameof(CreateAssetDto.PurchaseCost), LabelKey = "Str.PurchaseCost", Kind = FieldKind.Number, IsRequired = true },
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
