using PrimeERP.Application.DTOs.Cheques;
using PrimeERP.Application.DTOs.Treasury;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Application.Services.Vouchers;
using PrimeERP.Composition.Definitions;
using PrimeERP.Domain.Enums;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Tree;
using PrimeERP.UI.ViewModels;

namespace PrimeERP.Modules
{
    /// <summary>الخزائن والسندات والشيكات. السندان قبض/صرف نفس التعريف بالضبط عدا الطرف والخدمة — تُبنى من
    /// دالة واحدة بدل نسختين متطابقتين.</summary>
    public static class TreasuryRegistrations
    {
        public static void RegisterAll(IModuleRegistry registry)
        {
            registry.Register(new ModuleDefinition
            {
                Key = "Treasuries", TitleKey = "Str.Module.Treasuries", PermissionPrefix = "Treasuries",
                ViewModelType = typeof(TreasuriesViewModel),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(TreasuryDto.Code), Width = 100 },
                    new() { Header = "النوع", Binding = nameof(TreasuryDto.KindName), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = "اسم الخزينة / البنك", Binding = nameof(TreasuryDto.Name), Width = 240, IsStarWidth = true },
                    new() { Header = "الحساب بالشجرة", Binding = nameof(TreasuryDto.AccountCode), Width = 130, Align = ColumnAlign.Center },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Treasuries.Add", TitleEditKey = "Str.Treasuries.Edit", GridColumns = 2,
                    ServiceType = typeof(PrimeERP.Application.Services.Treasury.ITreasuryService),
                    CreateDtoType = typeof(CreateTreasuryDto), UpdateDtoType = typeof(UpdateTreasuryDto),
                    Fields = new()
                    {
                        // الحساب لا يُختار: الخزينة ورقة تحت "الصناديق" والبنك تحت "البنوك"، والنوع وحده يحدّد أيهما.
                        new() { Key = nameof(CreateTreasuryDto.Kind), LabelKey = "النوع", Kind = FieldKind.Picker, PickerType = "TreasuryKind", IsRequired = true, IsReadOnlyOnEdit = true, DefaultValue = (int)TreasuryKind.Cash },
                        new() { Key = nameof(CreateTreasuryDto.Name), LabelKey = "اسم الخزينة / البنك", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(CreateTreasuryDto.AccountNumber), LabelKey = "رقم الحساب بالبنك", Kind = FieldKind.Text, MaxLength = 60,
                                VisibleWhenField = nameof(CreateTreasuryDto.Kind), VisibleWhenValue = (int)TreasuryKind.Bank },
                        new() { Key = nameof(CreateTreasuryDto.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
                        new() { Key = nameof(CreateTreasuryDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }
                }
            });

            RegisterVoucher(registry, "Receipts", "Str.Module.Receipts", typeof(ReceiptVouchersViewModel), typeof(IReceiptVoucherService), "Str.Customer", "Customer", "SalesInvoice");
            RegisterVoucher(registry, "Payments", "Str.Module.Payments", typeof(PaymentVouchersViewModel), typeof(IPaymentVoucherService), "Str.Supplier", "Supplier", "PurchaseInvoice");

            RegisterChequeDocument(registry, "ChequeReceipts", "Str.Module.ChequeReceipts", typeof(ChequeReceiptsViewModel),
                typeof(PrimeERP.Application.Services.Cheques.IChequeReceiptDocumentService), "Str.Customer", "Customer");
            RegisterChequeDocument(registry, "ChequeIssues", "Str.Module.ChequeIssues", typeof(ChequeIssuesViewModel),
                typeof(PrimeERP.Application.Services.Cheques.IChequeIssueDocumentService), "Str.Supplier", "Supplier");

            registry.Register(new ModuleDefinition
            {
                Key = "Cheques", TitleKey = "Str.Module.Cheques", PermissionPrefix = "Cheques",
                ViewModelType = typeof(ChequesViewModel),
                Columns = new()
                {
                    new() { Header = "رقم الشيك", Binding = nameof(ChequeDto.ChequeNo), Width = 110 },
                    new() { Header = "الاتجاه", Binding = nameof(ChequeDto.DirectionName), Width = 80, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(ChequeDto.PartyName), Width = 180, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Amount"), Binding = nameof(ChequeDto.Amount), Width = 110, Align = ColumnAlign.Center, Format = "N2", Footer = FooterAggregate.Sum },
                    new() { Header = "الاستحقاق", Binding = nameof(ChequeDto.DueDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = "البنك", Binding = nameof(ChequeDto.BankName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.Status"), Binding = nameof(ChequeDto.StatusName), Width = 110, Align = ColumnAlign.Center },
                },
                LayoutKind = LayoutKind.ChequeBoard
            });
        }

        // مستند شيكات: رأس (تاريخ/طرف/بيان) وسطور، كل سطر شيك مستقل — نفس محرّر المستندات بلا استثناء.
        private static void RegisterChequeDocument(IModuleRegistry registry, string key, string title,
            System.Type viewModel, System.Type service, string partyLabelKey, string partyPickerType)
        {
            registry.Register(new ModuleDefinition
            {
                Key = key, TitleKey = title, PermissionPrefix = "Cheques", ViewModelType = viewModel,
                Columns = new()
                {
                    new() { Header = "رقم الشيك", Binding = nameof(ChequeDto.ChequeNo), Width = 110 },
                    new() { Header = "البنك", Binding = nameof(ChequeDto.BankName), Width = 160 },
                    new() { Header = "الطرف", Binding = nameof(ChequeDto.PartyName), Width = 180, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Amount"), Binding = nameof(ChequeDto.Amount), Width = 120, Align = ColumnAlign.Center, Format = "N2", Footer = FooterAggregate.Sum },
                    new() { Header = "الاستحقاق", Binding = nameof(ChequeDto.DueDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Status"), Binding = nameof(ChequeDto.StatusName), Width = 110, Align = ColumnAlign.Center },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    TitleKey = title, TitleEditKey = title,
                    ServiceType = service, DtoType = typeof(CreateChequeDocumentDto),
                    LineDtoType = typeof(CreateChequeLineDto), LinesPropertyName = nameof(CreateChequeDocumentDto.Lines),
                    DocumentKind = key,
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreateChequeDocumentDto.DocDate), LabelKey = "Str.Date", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateChequeDocumentDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(CreateChequeLineDto.ChequeNo), Header = "رقم الشيك", Kind = FieldKind.Text, Width = 130, IsRequired = true },
                        new() { Key = nameof(CreateChequeLineDto.BankName), Header = "اسم البنك", Kind = FieldKind.Text, Width = 170 },
                        new() { Key = nameof(CreateChequeLineDto.Amount), Header = LocalizationService.Get("Str.Amount"), Kind = FieldKind.Number, Width = 120, IsRequired = true },
                        new() { Key = nameof(CreateChequeLineDto.PartyId), Header = partyLabelKey == "Str.Customer" ? "العميل" : "المورد", Kind = FieldKind.Picker, Width = 190, PickerType = partyPickerType },
                        new() { Key = nameof(CreateChequeLineDto.DueDate), Header = "الاستحقاق", Kind = FieldKind.Date, Width = 130 },
                        new() { Key = nameof(CreateChequeLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 150 },
                    }
                }
            });
        }

        private static void RegisterVoucher(IModuleRegistry registry, string key, string title, System.Type viewModel,
            System.Type service, string partyLabelKey, string partyPickerType, string invoicePickerType)
        {
            registry.Register(new ModuleDefinition
            {
                Key = key, TitleKey = title, PermissionPrefix = key, ViewModelType = viewModel,
                Columns = new()
                {
                    new() { Header = "رقم السند", Binding = nameof(VoucherDto.VoucherNo), Width = 110 },
                    new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(VoucherDto.VoucherDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get(partyLabelKey), Binding = nameof(VoucherDto.PartyName), Width = 180, IsStarWidth = true },
                    new() { Header = "الخزينة", Binding = nameof(VoucherDto.TreasuryName), Width = 140 },
                    new() { Header = LocalizationService.Get("Str.Amount"), Binding = nameof(VoucherDto.Amount), Width = 110, Align = ColumnAlign.Center, Format = "N2", Footer = FooterAggregate.Sum },
                    new() { Header = "الطريقة", Binding = nameof(VoucherDto.MethodName), Width = 90, Align = ColumnAlign.Center },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    TitleKey = title, TitleEditKey = title, DocumentKind = key,
                    ServiceType = service, DtoType = typeof(CreateVoucherDto), LineDtoType = typeof(CreateVoucherAllocationDto),
                    LinesPropertyName = nameof(CreateVoucherDto.Allocations),
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreateVoucherDto.VoucherDate), LabelKey = "Str.Date", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateVoucherDto.PartyId), LabelKey = partyLabelKey, Kind = FieldKind.Picker, PickerType = partyPickerType, IsRequired = true },
                        new() { Key = nameof(CreateVoucherDto.Method), LabelKey = "طريقة الدفع", Kind = FieldKind.Picker, PickerType = "PaymentMethod", IsRequired = true, DefaultValue = (int)PaymentMethod.Cash },
                        // القائمة تتبع طريقة الدفع: نقداً تعرض الخزن، وتحويلاً أو شيكاً تعرض البنوك.
                        new() { Key = nameof(CreateVoucherDto.TreasuryId), LabelKey = "الخزينة / البنك", Kind = FieldKind.Picker, PickerType = "Treasury", IsRequired = true,
                                PickerFilterField = nameof(CreateVoucherDto.Method) },
                        new() { Key = nameof(CreateVoucherDto.Amount), LabelKey = "Str.Amount", Kind = FieldKind.Number, IsRequired = true },
                        new() { Key = nameof(CreateVoucherDto.Reference), LabelKey = "مرجع", Kind = FieldKind.Text, MaxLength = 100 },
                        // حقول الشيك تظهر فقط عند اختيار طريقة "شيك" — الشرط مُعلَن هنا لا مكتوب في الواجهة.
                        new() { Key = nameof(CreateVoucherDto.ChequeNo), LabelKey = "رقم الشيك", Kind = FieldKind.Text, MaxLength = 40,
                                VisibleWhenField = nameof(CreateVoucherDto.Method), VisibleWhenValue = (int)PaymentMethod.Cheque },
                        new() { Key = nameof(CreateVoucherDto.ChequeDueDate), LabelKey = "استحقاق الشيك", Kind = FieldKind.Date,
                                VisibleWhenField = nameof(CreateVoucherDto.Method), VisibleWhenValue = (int)PaymentMethod.Cheque },
                        new() { Key = nameof(CreateVoucherDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300, ColumnSpan = 2 },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(CreateVoucherAllocationDto.InvoiceNo), Header = "الفاتورة (اختياري)", Kind = FieldKind.Picker, Width = 260, PickerType = invoicePickerType },
                        new() { Key = nameof(CreateVoucherAllocationDto.Amount), Header = LocalizationService.Get("Str.Amount"), Kind = FieldKind.Number, Width = 120 },
                        new() { Key = nameof(CreateVoucherAllocationDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 200 },
                    }
                }
            });
        }
    }
}
