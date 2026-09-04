using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.ViewModels;

namespace PrimeERP.Modules
{
    public static class CycleVoucherRegistrations
    {
        public static void RegisterAll(IModuleRegistry registry)
        {
            Register(registry, "GoodsReceipt", "Str.Module.GoodsReceipt", typeof(GoodsReceiptViewModel), typeof(IGoodsReceiptService),
                StockEffect.In, Pull("PurchaseOrder", "سحب من أمر شراء"));

            Register(registry, "GoodsIssue", "Str.Module.GoodsIssue", typeof(GoodsIssueViewModel), typeof(IGoodsIssueService),
                StockEffect.Out, Pull("PurchaseReturns", "سحب من مرتجع شراء"));

            Register(registry, "DeliveryNote", "Str.Module.DeliveryNote", typeof(DeliveryNoteViewModel), typeof(IDeliveryNoteService),
                StockEffect.Out, Pull("SalesOrder", "سحب من أمر توريد"));

            Register(registry, "SalesReceipt", "Str.Module.SalesReceipt", typeof(SalesReceiptViewModel), typeof(ISalesReceiptService),
                StockEffect.In, Pull("SalesReturns", "سحب من مرتجع بيع"));
        }

        private static PullSource Pull(string sourceKind, string label) => new()
        {
            SourceKind = sourceKind,
            Label = label,
            PermissionKey = "Inventory.Create",
            MatchFields = new(),   // أذون المخزن تُسحب من مستندات بلا مخزن (أمر شراء/توريد) — لا حقل مطابقة
        };

        private static void Register(IModuleRegistry registry, string key, string title, Type viewModel, Type service,
            StockEffect stock, PullSource pullSource)
        {
            registry.Register(new ModuleDefinition
            {
                Key = key, TitleKey = title, PermissionPrefix = "Inventory", ViewModelType = viewModel,
                FlowScope = FlowScope.FullCycleOnly,
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.DocNo"), Binding = nameof(StockAdjustmentDto.DocNo), Width = 110 },
                    new() { Header = LocalizationService.Get("Str.InvoiceDate"), Binding = nameof(StockAdjustmentDto.MovementDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Warehouse"), Binding = nameof(StockAdjustmentDto.WarehouseName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Qty"), Binding = nameof(StockAdjustmentDto.TotalQty), Width = 100, Align = ColumnAlign.Center, Format = "N2" },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    TitleKey = title, TitleEditKey = title,
                    DocumentKind = key,
                    ServiceType = service, DtoType = typeof(CreateStockAdjustmentDto), LineDtoType = typeof(CreateStockAdjustmentLineDto),
                    LinesPropertyName = nameof(CreateStockAdjustmentDto.Lines),
                    AllowPost = false,
                    AffectsStock = stock,
                    PullSources = new List<PullSource> { pullSource },
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
        }
    }
}
