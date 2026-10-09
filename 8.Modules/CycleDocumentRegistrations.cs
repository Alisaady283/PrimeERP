using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Application.PageServices.Documents;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.ViewModels;

namespace PrimeERP.Modules
{
    /// <summary>تسجيل مستندات الدورة</summary>
    public static class CycleDocumentRegistrations
    {
        public static void RegisterAll(IModuleRegistry registry)
        {
            Register(registry, "PurchaseRequest", "Str.Module.PurchaseRequest", typeof(PurchaseRequestViewModel), typeof(IPurchaseRequestService),
                "Purchases", "Str.Supplier", "Supplier", partyRequired: false, showPrices: false, pullSources: new());

            Register(registry, "PurchaseOrder", "Str.Module.PurchaseOrder", typeof(PurchaseOrderViewModel), typeof(IPurchaseOrderService),
                "Purchases", "Str.Supplier", "Supplier", partyRequired: true, showPrices: true,
                pullSources: CycleFlow.IntoPurchaseOrder());

            Register(registry, "Quotation", "Str.Module.Quotation", typeof(QuotationViewModel), typeof(IQuotationService),
                "Sales", "Str.Customer", "Customer", partyRequired: false, showPrices: true, pullSources: new());

            Register(registry, "SalesOrder", "Str.Module.SalesOrder", typeof(SalesOrderViewModel), typeof(ISalesOrderService),
                "Sales", "Str.Customer", "Customer", partyRequired: true, showPrices: true,
                pullSources: CycleFlow.IntoSalesOrder());
        }

        private static void Register(IModuleRegistry registry, string key, string title, Type viewModel, Type service,
            string permissionPrefix, string partyLabelKey, string partyPickerType, bool partyRequired, bool showPrices,
            List<PullSource> pullSources)
        {
            var lineFields = new List<LineFieldDefinition>
            {
                new() { Key = nameof(CreateCycleDocumentLineDto.ProductCode), Header = LocalizationService.Get("Str.Product"), Kind = FieldKind.Picker, Width = 220, IsRequired = true, PickerType = "Product" },
                StandardFields.Qty(),
            };

            if (showPrices)
                lineFields.Add(StandardFields.Price());

            lineFields.Add(new() { Key = nameof(CreateCycleDocumentLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 160 });

            var columns = new List<GridColumn>
            {
                new() { Header = LocalizationService.Get("Str.DocNo"), Binding = nameof(CycleDocumentDto.DocNo), Width = 120 },
                new() { Header = LocalizationService.Get("Str.InvoiceDate"), Binding = nameof(CycleDocumentDto.DocDate), Width = 110, Format = "yyyy-MM-dd" },
                new() { Header = LocalizationService.Get("Str.Qty"), Binding = nameof(CycleDocumentDto.TotalQty), Width = 100, Align = ColumnAlign.Center, Format = "N2" },
            };

            if (showPrices)
                columns.Add(new() { Header = LocalizationService.Get("Str.Total"), Binding = nameof(CycleDocumentDto.Total), Width = 120, Align = ColumnAlign.Center, Format = "N2", Footer = FooterAggregate.Sum });

            columns.Add(new() { Header = LocalizationService.Get("Str.Notes"), Binding = nameof(CycleDocumentDto.Notes), Width = 220, IsStarWidth = true });

            registry.Register(new ModuleDefinition
            {
                Key = key, TitleKey = title, PermissionPrefix = permissionPrefix, ViewModelType = viewModel,
                FlowScope = FlowScope.FullCycleOnly,
                Columns = columns,
                DocumentDialog = new DocumentDialogDefinition
                {
                    TitleKey = title, TitleEditKey = title,
                    DocumentKind = key,
                    ServiceType = service, DtoType = typeof(CreateCycleDocumentDto), LineDtoType = typeof(CreateCycleDocumentLineDto),
                    LinesPropertyName = nameof(CreateCycleDocumentDto.Lines),
                    AllowPost = false,
                    AffectsStock = StockEffect.None,
                    PullSources = pullSources,
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreateCycleDocumentDto.DocDate), LabelKey = "Str.InvoiceDate", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateCycleDocumentDto.PartyId), LabelKey = partyLabelKey, Kind = FieldKind.Picker, PickerType = partyPickerType, IsRequired = partyRequired },
                        new() { Key = nameof(CreateCycleDocumentDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300 },
                    },
                    LineFields = lineFields
                }
            });
        }
    }
}
