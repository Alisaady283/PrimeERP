using PrimeERP.Application.Legacy.Security;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Legacy.Assets;
using PrimeERP.Application.Legacy.Common;
using PrimeERP.Application.DTOs.Security;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.Legacy.HR;
using PrimeERP.Application.DTOs.Purchasing;
using PrimeERP.Application.Legacy.Purchasing;
using PrimeERP.Application.Legacy.Sales;
using PrimeERP.Application.Legacy.Inventory;
using PrimeERP.Application.Legacy.Parties;
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
    /// <summary>يسجّل كل وحدة عمل فعلية</summary>
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
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(Customer.Code), Width = 110, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(Customer.Name), Width = 220, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Category"), Binding = nameof(Customer.CategoryName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.Phone"), Binding = nameof(Customer.Phone), Width = 130 },
                    new() { Header = LocalizationService.Get("Str.CreditLimit"), Binding = nameof(Customer.CreditLimit), Width = 120, Align = ColumnAlign.Center, Format = "N2", PermissionKey = PermissionKeys.Customers.ColumnCreditLimit },
                    new() { Header = LocalizationService.Get("Str.Balance"), Binding = nameof(Customer.Balance), Width = 120, Align = ColumnAlign.Center, Format = "N2", Footer = FooterAggregate.Sum },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Customers.Add",
                    TitleEditKey = "Str.Customers.Edit",
                    GridColumns = 2,
                    ServiceType = typeof(ICustomerService),
                    CreateDtoType = typeof(Customer),
                    UpdateDtoType = typeof(Customer),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(Customer.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(Customer.Phone), LabelKey = "Str.Phone", Kind = FieldKind.Text, MaxLength = 30 },
                        new() { Key = nameof(Customer.Email), LabelKey = "Str.Email", Kind = FieldKind.Text, MaxLength = 150 },
                        new() { Key = nameof(Customer.CreditLimit), LabelKey = "Str.CreditLimit", Kind = FieldKind.Number },
                        new() { Key = nameof(Customer.CategoryId), LabelKey = "Str.Category", Kind = FieldKind.Picker, PickerType = "Category", PickerCategoryModuleKey = "Customers" },
                        new() { Key = nameof(Customer.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
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
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(Supplier.Code), Width = 110, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(Supplier.Name), Width = 220, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Category"), Binding = nameof(Supplier.CategoryName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.Phone"), Binding = nameof(Supplier.Phone), Width = 130 },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Suppliers.Add",
                    TitleEditKey = "Str.Suppliers.Edit",
                    GridColumns = 2,
                    ServiceType = typeof(ISupplierService),
                    CreateDtoType = typeof(Supplier),
                    UpdateDtoType = typeof(Supplier),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(Supplier.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(Supplier.Phone), LabelKey = "Str.Phone", Kind = FieldKind.Text, MaxLength = 30 },
                        new() { Key = nameof(Supplier.Email), LabelKey = "Str.Email", Kind = FieldKind.Text, MaxLength = 150 },
                        new() { Key = nameof(Supplier.CreditLimit), LabelKey = "Str.CreditLimit", Kind = FieldKind.Number },
                        new() { Key = nameof(Supplier.CategoryId), LabelKey = "Str.Category", Kind = FieldKind.Picker, PickerType = "Category", PickerCategoryModuleKey = "Suppliers" },
                        new() { Key = nameof(Supplier.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
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
                Filters = new()
                {
                    new() { Key = nameof(AccountTreeFilter.TypeFilter), LabelKey = "Str.AccountType", PickerType = "AccountType" },
                },
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
                    },
                    LineTotals = new()
                    {
                        Keys = new() { nameof(CreateJournalLineDto.Debit), nameof(CreateJournalLineDto.Credit) },
                        MustBalance = new[] { nameof(CreateJournalLineDto.Debit), nameof(CreateJournalLineDto.Credit) }
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
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(Product.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(Product.Name), Width = 220, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Category"), Binding = nameof(Product.CategoryName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.CostPrice"), Binding = nameof(Product.CostPrice), Width = 110, Align = ColumnAlign.Center, Format = "N2", PermissionKey = PermissionKeys.Products.ColumnCostPrice },
                    new() { Header = LocalizationService.Get("Str.SalePrice"), Binding = nameof(Product.SalePrice), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
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
                    CreateDtoType = typeof(Product),
                    UpdateDtoType = typeof(Product),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(Product.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(Product.Barcode), LabelKey = "Str.Barcode", Kind = FieldKind.Text, MaxLength = 60 },
                        new() { Key = nameof(Product.CostPrice), LabelKey = "Str.CostPrice", Kind = FieldKind.Number, IsRequired = true },
                        new() { Key = nameof(Product.SalePrice), LabelKey = "Str.SalePrice", Kind = FieldKind.Number, IsRequired = true },
                        new() { Key = nameof(Product.CategoryId), LabelKey = "Str.Category", Kind = FieldKind.Picker, PickerType = "Category", PickerCategoryModuleKey = "Products" },
                        new() { Key = nameof(Product.BrandId), LabelKey = "Str.Brand", Kind = FieldKind.Picker, PickerType = "Category", PickerCategoryModuleKey = "Brands" },
                        new() { Key = nameof(Product.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }.Concat(StandardFields.DialogFields()).ToList()
                }
            });

            RegisterLookup(registry, "Categories", "Str.Module.Categories", "Categories.Add", "Categories.Edit", typeof(CategoriesLookupViewModel));
            RegisterLookup(registry, "Brands", "Str.Module.Brands", "Brands.Add", "Brands.Edit", typeof(BrandsViewModel));
            RegisterLookup(registry, "AssetCategories", "Str.Module.AssetCategories", "AssetCategories.Add", "AssetCategories.Edit", typeof(AssetCategoriesViewModel));

            registry.Register(new ModuleDefinition
            {
                Key = "Departments", TitleKey = "Str.Module.Departments", PermissionPrefix = "Departments",
                ViewModelFactory = s => RowPage.ViewModel<Lookup<Department>>(s, "Departments"),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(Department.Name), Width = 220, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Active"), Binding = nameof(Department.IsActive), Width = 80, Align = ColumnAlign.Center },
                },
                Dialog = RowPage.Dialog<Lookup<Department>>("Str.Departments.Add", new List<FieldDefinition>
                {
                    new() { Key = nameof(Department.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                    new() { Key = nameof(Department.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
                }, "Str.Departments.Edit", gridColumns: 1)
            });

            registry.Register(new ModuleDefinition
            {
                Key = "JobTitles", TitleKey = "Str.Module.JobTitles", PermissionPrefix = "JobTitles",
                ViewModelFactory = s => RowPage.ViewModel<Lookup<JobTitle>>(s, "JobTitles"),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(JobTitle.Name), Width = 220, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Active"), Binding = nameof(JobTitle.IsActive), Width = 80, Align = ColumnAlign.Center },
                },
                Dialog = RowPage.Dialog<Lookup<JobTitle>>("Str.JobTitles.Add", new List<FieldDefinition>
                {
                    new() { Key = nameof(JobTitle.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                    new() { Key = nameof(JobTitle.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
                }, "Str.JobTitles.Edit", gridColumns: 1)
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Units", TitleKey = "Str.Module.Units", PermissionPrefix = "Units",
                ViewModelFactory = s => RowPage.ViewModel<Lookup<Unit>>(s, "Units"),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(Unit.Name), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Symbol"), Binding = nameof(Unit.Symbol), Width = 100 },
                    new() { Header = LocalizationService.Get("Str.Active"), Binding = nameof(Unit.IsActive), Width = 80, Align = ColumnAlign.Center },
                },
                Dialog = RowPage.Dialog<Lookup<Unit>>("Str.Units.Add", new List<FieldDefinition>
                {
                    new() { Key = nameof(Unit.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                    new() { Key = nameof(Unit.Symbol), LabelKey = "Str.Symbol", Kind = FieldKind.Text, MaxLength = 20 },
                    new() { Key = nameof(Unit.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
                }, "Str.Units.Edit", gridColumns: 1)
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Warehouses", TitleKey = "Str.Module.Warehouses", PermissionPrefix = "Warehouses",
                ViewModelFactory = s => RowPage.ViewModel<Lookup<Warehouse>>(s, "Warehouses"),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(Warehouse.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(Warehouse.Name), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Location"), Binding = nameof(Warehouse.Location), Width = 160 },
                    new() { Header = LocalizationService.Get("Str.Active"), Binding = nameof(Warehouse.IsActive), Width = 80, Align = ColumnAlign.Center },
                },
                Dialog = RowPage.Dialog<Lookup<Warehouse>>("Str.Warehouses.Add", new List<FieldDefinition>
                {
                    new() { Key = nameof(Warehouse.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                    new() { Key = nameof(Warehouse.Location), LabelKey = "Str.Location", Kind = FieldKind.Text, MaxLength = 200 },
                    new() { Key = nameof(Warehouse.ManagerName), LabelKey = "Str.ManagerName", Kind = FieldKind.Text, MaxLength = 150 },
                    new() { Key = nameof(Warehouse.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
                }, "Str.Warehouses.Edit", gridColumns: 1)
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Assets",
                TitleKey = "Str.Module.Assets",
                PermissionPrefix = "Assets",
                ViewModelType = typeof(AssetsViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(Asset.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(Asset.Name), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Category"), Binding = nameof(Asset.CategoryName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.PurchaseDate"), Binding = nameof(Asset.PurchaseDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Asset.Cost"), Binding = nameof(Asset.PurchaseCost), Width = 110, Align = ColumnAlign.Center, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.Asset.BookValue"), Binding = nameof(Asset.CurrentValue), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
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
                    CreateDtoType = typeof(Asset),
                    UpdateDtoType = typeof(Asset),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(Asset.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(Asset.CategoryId), LabelKey = "Str.Category", Kind = FieldKind.Picker, PickerType = "Category", PickerCategoryModuleKey = "AssetCategories" },
                        new() { Key = nameof(Asset.PurchaseDate), LabelKey = "Str.PurchaseDate", Kind = FieldKind.Date },
                        new() { Key = nameof(Asset.PurchaseCost), LabelKey = "Str.PurchaseCost", Kind = FieldKind.Number, IsRequired = true, Min = 0 },
                        new() { Key = nameof(Asset.AcquisitionMethod), LabelKey = "Str.Asset.Acquisition", Kind = FieldKind.Picker, IsRequired = true, PickerType = "AssetAcquisition" },
                        new() { Key = nameof(Asset.FundingId), LabelKey = "Str.Asset.Funding", Kind = FieldKind.Picker, IsRequired = true, PickerType = "AssetFunding",
                                PickerFilterField = nameof(Asset.AcquisitionMethod) },
                        new() { Key = nameof(Asset.UsefulLifeYears), LabelKey = "Str.Asset.Life", Kind = FieldKind.Number, Min = 0, Max = 100 },
                        new() { Key = nameof(Asset.SalvageValue), LabelKey = "Str.Asset.Salvage", Kind = FieldKind.Number, Min = 0 },
                        new() { Key = nameof(Asset.Location), LabelKey = "Str.Location", Kind = FieldKind.Text, MaxLength = 200 },
                        new() { Key = nameof(Asset.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }.Concat(StandardFields.DialogFields()).ToList()
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "AssetDepreciations",
                TitleKey = "Str.Module.AssetDepreciations",
                PermissionPrefix = "Assets",
                ViewModelType = typeof(AssetDepreciationsViewModel),
                RowActions = new()
                {
                    new()
                    {
                        Label = "احتساب الإهلاك", Variant = "primary", PermissionKey = "Assets.Create",
                        RequiresSelection = false,
                        Execute = (services, _) => services
                            .GetRequiredService<PrimeERP.Application.Legacy.Assets.IAssetDepreciationService>()
                            .RunFor(DateTime.Today)
                    }
                },
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(AssetDepreciationDto.PeriodDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(AssetDepreciationDto.AssetCode), Width = 110, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(AssetDepreciationDto.AssetName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Description"), Binding = nameof(AssetDepreciationDto.Notes), Width = 240 },
                    new() { Header = LocalizationService.Get("Str.Amount"), Binding = nameof(AssetDepreciationDto.Amount), Width = 130, Align = ColumnAlign.Center, Format = "N2", Footer = FooterAggregate.Sum },
                },
                Filters = new()
                {
                    new() { Key = nameof(AssetDepreciationFilter.AssetId), LabelKey = "Str.Assets", PickerType = "Asset" },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Module.AssetDepreciations",
                    TitleEditKey = "Str.Module.AssetDepreciations",
                    GridColumns = 2,
                    ServiceType = typeof(IAssetDepreciationService),
                    CreateDtoType = typeof(CreateAssetDepreciationDto),
                    UpdateDtoType = typeof(UpdateAssetDepreciationDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateAssetDepreciationDto.AssetId), LabelKey = "Str.Assets", Kind = FieldKind.Picker, IsRequired = true, PickerType = "Asset" },
                        new() { Key = nameof(CreateAssetDepreciationDto.PeriodDate), LabelKey = "Str.Date", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateAssetDepreciationDto.Amount), LabelKey = "Str.Amount", Kind = FieldKind.Number, IsRequired = true, Min = 0 },
                        new() { Key = nameof(CreateAssetDepreciationDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "AssetRevaluations",
                TitleKey = "Str.Module.AssetRevaluations",
                PermissionPrefix = "Assets",
                ViewModelType = typeof(AssetRevaluationsViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(AssetRevaluationDto.RevaluationDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(AssetRevaluationDto.AssetCode), Width = 100, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(AssetRevaluationDto.AssetName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Asset.OldValue"), Binding = nameof(AssetRevaluationDto.OldValue), Width = 130, Align = ColumnAlign.Center, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.Asset.NewValue"), Binding = nameof(AssetRevaluationDto.NewValue), Width = 140, Align = ColumnAlign.Center, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.Asset.Difference"), Binding = nameof(AssetRevaluationDto.Difference), Width = 110, Align = ColumnAlign.Center, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.Asset.RevaluationKind"), Binding = nameof(AssetRevaluationDto.KindText), Width = 90, Align = ColumnAlign.Center },
                },
                Filters = new()
                {
                    new() { Key = nameof(AssetRevaluationFilter.AssetId), LabelKey = "Str.Assets", PickerType = "Asset" },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Asset.Revaluation",
                    TitleEditKey = "Str.Asset.Revaluation",
                    GridColumns = 2,
                    ServiceType = typeof(IAssetRevaluationService),
                    CreateDtoType = typeof(CreateAssetRevaluationDto),
                    UpdateDtoType = typeof(UpdateAssetRevaluationDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateAssetRevaluationDto.AssetId), LabelKey = "Str.Assets", Kind = FieldKind.Picker, IsRequired = true, PickerType = "Asset" },
                        new() { Key = nameof(CreateAssetRevaluationDto.RevaluationDate), LabelKey = "Str.Date", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateAssetRevaluationDto.NewValue), LabelKey = "Str.Asset.NewValue", Kind = FieldKind.Number, IsRequired = true, Min = 0 },
                        new() { Key = nameof(CreateAssetRevaluationDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "AssetDisposals",
                TitleKey = "Str.Module.AssetDisposals",
                PermissionPrefix = "Assets",
                ViewModelType = typeof(AssetDisposalsViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(AssetDisposalDto.DisposalDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(AssetDisposalDto.AssetCode), Width = 100, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(AssetDisposalDto.AssetName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Treasury"), Binding = nameof(AssetDisposalDto.TreasuryName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.Asset.SalePrice"), Binding = nameof(AssetDisposalDto.SalePrice), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.Asset.BookValue"), Binding = nameof(AssetDisposalDto.BookValue), Width = 130, Align = ColumnAlign.Center, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.Asset.GainOrLoss"), Binding = nameof(AssetDisposalDto.GainOrLoss), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.Asset.DisposalKind"), Binding = nameof(AssetDisposalDto.KindText), Width = 90, Align = ColumnAlign.Center },
                },
                Filters = new()
                {
                    new() { Key = nameof(AssetDisposalFilter.AssetId), LabelKey = "Str.Assets", PickerType = "Asset" },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Asset.Disposal",
                    TitleEditKey = "Str.Asset.Disposal",
                    GridColumns = 2,
                    ServiceType = typeof(IAssetDisposalService),
                    CreateDtoType = typeof(CreateAssetDisposalDto),
                    UpdateDtoType = typeof(UpdateAssetDisposalDto),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(CreateAssetDisposalDto.AssetId), LabelKey = "Str.Assets", Kind = FieldKind.Picker, IsRequired = true, PickerType = "Asset" },
                        new() { Key = nameof(CreateAssetDisposalDto.DisposalDate), LabelKey = "Str.Date", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateAssetDisposalDto.TreasuryId), LabelKey = "Str.Treasury", Kind = FieldKind.Picker, IsRequired = true, PickerType = "Treasury" },
                        new() { Key = nameof(CreateAssetDisposalDto.SalePrice), LabelKey = "Str.Asset.SalePrice", Kind = FieldKind.Number, IsRequired = true, Min = 0 },
                        new() { Key = nameof(CreateAssetDisposalDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }
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
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(Employee.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(Employee.Name), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Department"), Binding = nameof(Employee.DepartmentName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.JobTitle"), Binding = nameof(Employee.JobTitleName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.Phone"), Binding = nameof(Employee.Phone), Width = 120 },
                    new() { Header = LocalizationService.Get("Str.Salary"), Binding = nameof(Employee.BasicSalary), Width = 110, Align = ColumnAlign.Center, Format = "N2", PermissionKey = PermissionKeys.HR.ColumnSalary },
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
                    CreateDtoType = typeof(Employee),
                    UpdateDtoType = typeof(Employee),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(Employee.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(Employee.DepartmentId), LabelKey = "Str.Department", Kind = FieldKind.Picker, PickerType = "Department" },
                        new() { Key = nameof(Employee.JobTitleId), LabelKey = "Str.JobTitle", Kind = FieldKind.Picker, PickerType = "JobTitle" },
                        new() { Key = nameof(Employee.Phone), LabelKey = "Str.Phone", Kind = FieldKind.Text, MaxLength = 30 },
                        new() { Key = nameof(Employee.Email), LabelKey = "Str.Email", Kind = FieldKind.Text, MaxLength = 120 },
                        new() { Key = nameof(Employee.HireDate), LabelKey = "Str.HireDate", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(Employee.BasicSalary), LabelKey = "Str.Salary", Kind = FieldKind.Number },
                        new() { Key = nameof(Employee.Status), LabelKey = "Str.Status", Kind = FieldKind.Picker, PickerType = "EmployeeStatus", IsRequired = true,
                                DefaultValue = (int)PrimeERP.Domain.Enums.EmployeeStatus.Active },
                        new() { Key = nameof(Employee.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }.Concat(StandardFields.DialogFields().Where(f => f.Key != "IsActive")).ToList()
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "Roles", TitleKey = "Str.Module.Roles", PermissionPrefix = "Users", ViewModelType = typeof(RolesViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(Role.NameAr), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.RoleNameEn"), Binding = nameof(Role.Name), Width = 160 },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Roles.Add", TitleEditKey = "Str.Roles.Edit", GridColumns = 1,
                    ServiceType = typeof(IRoleService), CreateDtoType = typeof(Role), UpdateDtoType = typeof(Role),
                    Fields = new List<FieldDefinition>
                    {
                        new() { Key = nameof(Role.NameAr), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 100 },
                        new() { Key = nameof(Role.Name), LabelKey = "Str.RoleNameEn", Kind = FieldKind.Text, IsRequired = true, MaxLength = 100 },
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
                    PullSources = CycleFlow.IntoSalesInvoice(),
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
                    PullSources = CycleFlow.IntoPurchaseInvoice(),
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
                    PullSources = CycleFlow.IntoSalesReturn(),
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
                    PullSources = CycleFlow.IntoPurchaseReturn(),
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

            StockDocumentFactory.Register(registry, "StockIn", "Str.Module.StockIn", typeof(StockInViewModel), typeof(IStockInService),
                FlowScope.SimplifiedOnly, printTitle: "إذن إضافة مخزني", addTitleKey: "Str.StockIn.Add", editTitleKey: "Str.StockIn.Edit");

            StockDocumentFactory.Register(registry, "StockOut", "Str.Module.StockOut", typeof(StockOutViewModel), typeof(IStockOutService),
                FlowScope.SimplifiedOnly, printTitle: "إذن صرف مخزني", addTitleKey: "Str.StockOut.Add", editTitleKey: "Str.StockOut.Edit");

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
                    new() { Header = LocalizationService.Get("Str.Status"), Binding = nameof(PayrollDto.StatusText), Width = 100, Align = ColumnAlign.Center },
                },
                RowActions = PayrollRowActions(),
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
                        new() { Key = nameof(CreatePayrollLineDto.BasicSalary), Header = LocalizationService.Get("Str.BasicSalary"), Kind = FieldKind.Number, Width = 100, IsRequired = true },
                        new() { Key = nameof(CreatePayrollLineDto.Allowances), Header = LocalizationService.Get("Str.Allowances"), Kind = FieldKind.Number, Width = 90 },
                        new() { Key = nameof(CreatePayrollLineDto.Overtime), Header = "الإضافي", Kind = FieldKind.Number, Width = 90 },
                        new() { Key = nameof(CreatePayrollLineDto.Deductions), Header = LocalizationService.Get("Str.Deductions"), Kind = FieldKind.Number, Width = 90 },
                        new() { Key = nameof(CreatePayrollLineDto.Advances), Header = "السلف", Kind = FieldKind.Number, Width = 90 },
                        new() { Key = nameof(CreatePayrollLineDto.Insurance), Header = "التأمينات", Kind = FieldKind.Number, Width = 90 },
                        new() { Key = nameof(CreatePayrollLineDto.Tax), Header = "الضرائب", Kind = FieldKind.Number, Width = 90 },
                        new() { Key = nameof(CreatePayrollLineDto.NetSalary), Header = "صافي المبلغ", Kind = FieldKind.ReadOnly, Width = 110 },
                        new() { Key = nameof(CreatePayrollLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 120 },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "OpeningBalances", TitleKey = "Str.Module.OpeningBalances", PermissionPrefix = "Journal",
                ViewModelType = typeof(OpeningBalancesViewModel),
                SingleRecord = true,
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
                        Label = "أرصدة الأصناف الافتتاحية", Variant = "secondary",
                        PermissionKey = PermissionKeys.Inventory.OpeningStock,
                        RequiresSelection = false,
                        Execute = (services, _) =>
                        {
                            PrimeERP.Composition.Renderers.DocumentRenderer.ShowAndSave(OpeningStockDialog(), services,
                                services.GetRequiredService<PrimeERP.UI.Services.IToastService>());
                            return PrimeERP.Domain.Results.Result.Ok();
                        }
                    }
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    PrintTitle = "قيد أرصدة افتتاحية",
                    TitleKey = "Str.Module.OpeningBalances", TitleEditKey = "Str.Module.OpeningBalances",
                    ServiceType = typeof(PrimeERP.Application.Legacy.Accounting.IOpeningBalanceService),
                    DtoType = typeof(CreateJournalDto), LineDtoType = typeof(CreateJournalLineDto),
                    LinesPropertyName = nameof(CreateJournalDto.Lines), DocumentKind = "OpeningBalances",
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreateJournalDto.EntryDate), LabelKey = "Str.StartDate", Kind = FieldKind.Date, IsReadOnly = true },
                        new() { Key = nameof(CreateJournalDto.Description), LabelKey = "Str.Description", Kind = FieldKind.Text, IsReadOnly = true,
                                DefaultValue = PrimeERP.Application.Legacy.Accounting.OpeningBalanceService.FixedDescription },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(CreateJournalLineDto.AccountCode), Header = LocalizationService.Get("Str.Account"), Kind = FieldKind.Picker, Width = 260, IsRequired = true, PickerType = "Account", PickerLeafOnly = true },
                        new() { Key = nameof(CreateJournalLineDto.Debit), Header = LocalizationService.Get("Str.Debit"), Kind = FieldKind.Number, Width = 120 },
                        new() { Key = nameof(CreateJournalLineDto.Credit), Header = LocalizationService.Get("Str.Credit"), Kind = FieldKind.Number, Width = 120 },
                        new() { Key = nameof(CreateJournalLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 160 },
                    },
                    LineTotals = new()
                    {
                        Keys = new() { nameof(CreateJournalLineDto.Debit), nameof(CreateJournalLineDto.Credit) },
                        MustBalance = new[] { nameof(CreateJournalLineDto.Debit), nameof(CreateJournalLineDto.Credit) }
                    }
                }
            });

            registry.Register(new ModuleDefinition { Key = "Settings", TitleKey = "Str.Module.Settings", PermissionPrefix = "Settings", LayoutKind = LayoutKind.Settings });
        }

        private static DocumentDialogDefinition OpeningStockDialog() => new()
        {
            TitleKey = "أرصدة الأصناف الافتتاحية", TitleEditKey = "أرصدة الأصناف الافتتاحية",
            ServiceType = typeof(PrimeERP.Application.Legacy.Inventory.IOpeningStockService),
            DtoType = typeof(CreateOpeningStockDto), LineDtoType = typeof(CreateOpeningStockLineDto),
            LinesPropertyName = nameof(CreateOpeningStockDto.Lines), DocumentKind = "OpeningStock",
            AllowPost = false,
            HeaderFields = new()
            {
                new() { Key = nameof(CreateOpeningStockDto.Date), LabelKey = "Str.StartDate", Kind = FieldKind.Date, IsRequired = true },
                new() { Key = nameof(CreateOpeningStockDto.WarehouseId), LabelKey = "Str.Warehouse", Kind = FieldKind.Picker, PickerType = "Warehouse", IsRequired = true },
                new() { Key = nameof(CreateOpeningStockDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300 },
            },
            LineFields = new()
            {
                new() { Key = nameof(CreateOpeningStockLineDto.ProductId), Header = LocalizationService.Get("Str.Product"), Kind = FieldKind.Picker, Width = 260, IsRequired = true, PickerType = "Product" },
                new() { Key = nameof(CreateOpeningStockLineDto.Qty), Header = LocalizationService.Get("Str.Qty"), Kind = FieldKind.Number, Width = 120 },
                new() { Key = nameof(CreateOpeningStockLineDto.UnitCost), Header = "سعر التكلفة", Kind = FieldKind.Number, Width = 120 },
                new() { Key = nameof(CreateOpeningStockLineDto.Value), Header = LocalizationService.Get("Str.Balance"), Kind = FieldKind.ReadOnly, Width = 120 },
                new() { Key = nameof(CreateOpeningStockLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 160 },
            },
            LineMath = new() { QtyKey = nameof(CreateOpeningStockLineDto.Qty), PriceKey = nameof(CreateOpeningStockLineDto.UnitCost), NetKey = nameof(CreateOpeningStockLineDto.Value) },
            LineTotals = new() { Keys = new() { nameof(CreateOpeningStockLineDto.Value) } }
        };


        private static List<RowAction> PayrollRowActions() => new()
        {
            new()
            {
                Label = "ترحيل", Variant = "primary", PermissionKey = PermissionKeys.HR.PaySalary,
                AppliesTo = item => IsPosted(item) is false,
                Execute = (services, item) => services.GetRequiredService<IPayrollService>().Post(IdOf(item))
            },
            new()
            {
                Label = "إلغاء الترحيل", Variant = "secondary", PermissionKey = PermissionKeys.HR.PaySalary,
                AppliesTo = item => IsPosted(item) is true,
                Execute = (services, item) => services.GetRequiredService<IPayrollService>().Unpost(IdOf(item))
            }
        };

        private static bool? IsPosted(object item) => item.GetType().GetProperty("IsPosted")?.GetValue(item) as bool?;

        private static int IdOf(object item) => (int)item.GetType().GetProperty("Id").GetValue(item);

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
