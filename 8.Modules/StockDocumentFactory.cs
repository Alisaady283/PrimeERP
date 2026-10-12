using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.Modules
{
    /// <summary>رأس المستند المخزني وسطوره</summary>
    internal static class StockDocumentFactory
    {
        internal static void Register(IModuleRegistry registry, string key, string titleKey, Type viewModel, Type service,
            FlowScope scope, StockEffect stock = StockEffect.None, List<PullSource> pullSources = null,
            string printTitleKey = null, string addTitleKey = null, string editTitleKey = null, bool allowPost = true,
            Func<IServiceProvider, object> viewModelFactory = null, string permissionPrefix = "Inventory")
        {
            registry.Register(new ModuleDefinition
            {
                Key = key, TitleKey = titleKey, PermissionPrefix = permissionPrefix, ViewModelType = viewModel, ViewModelFactory = viewModelFactory,
                FlowScope = scope,
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.DocNo"), Binding = nameof(StockAdjustmentDto.DocNo), Width = 110 },
                    new() { Header = LocalizationService.Get("Str.InvoiceDate"), Binding = nameof(StockAdjustmentDto.MovementDate), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Warehouse"), Binding = nameof(StockAdjustmentDto.WarehouseName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Qty"), Binding = nameof(StockAdjustmentDto.TotalQty), Width = 100, Align = ColumnAlign.Center, Format = "N2" },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    PrintTitleKey = printTitleKey,
                    TitleKey = addTitleKey ?? titleKey, TitleEditKey = editTitleKey ?? titleKey,
                    DocumentKind = pullSources == null ? null : key,
                    ServiceType = service, DtoType = typeof(CreateStockAdjustmentDto), LineDtoType = typeof(CreateStockAdjustmentLineDto),
                    LinesPropertyName = nameof(CreateStockAdjustmentDto.Lines),
                    AllowPost = allowPost,
                    AffectsStock = stock,
                    PullSources = pullSources ?? new List<PullSource>(),
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreateStockAdjustmentDto.MovementDate), LabelKey = "Str.InvoiceDate", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateStockAdjustmentDto.WarehouseId), LabelKey = "Str.Warehouse", Kind = FieldKind.Picker, PickerType = "Warehouse", IsRequired = true },
                        new() { Key = nameof(CreateStockAdjustmentDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300 },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(CreateStockAdjustmentLineDto.ProductCode), Header = LocalizationService.Get("Str.Product"), Kind = FieldKind.Picker, Width = 220, IsRequired = true, PickerType = "Product" },
                        StandardFields.Qty(),
                        StandardFields.Batch(),
                        StandardFields.Expiry(),
                        new() { Key = nameof(CreateStockAdjustmentLineDto.UnitCost), Header = LocalizationService.Get("Str.UnitCost"), Kind = FieldKind.Number, Width = 100 },
                        new() { Key = nameof(CreateStockAdjustmentLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 160 },
                    }
                }
            });
        }
    }
}
