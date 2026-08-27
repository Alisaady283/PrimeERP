using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.Services.Accounting;
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
        }
    }
}
