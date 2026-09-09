using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.Modules
{
    /// <summary>
    /// كل مستند مخزني رأسه (تاريخ/مخزن/بيان) وسطوره (صنف/كمية/تكلفة/بيان) — أذون الدورة الأربعة وإذنا
    /// الإضافة والصرف المبسّطان سواء. يُسجَّل من هنا لا بنسخةٍ لكل وحدة: كان إذنا المبسّط مكتوبين يدوياً
    /// نسخةً من هذا الشكل، فأي تعديل على الأعمدة أو الحقول كان يلزمه ثلاثة مواضع.
    /// </summary>
    internal static class StockDocumentFactory
    {
        internal static void Register(IModuleRegistry registry, string key, string titleKey, Type viewModel, Type service,
            FlowScope scope, StockEffect stock = StockEffect.None, PullSource pullSource = null,
            string printTitle = null, string addTitleKey = null, string editTitleKey = null, bool allowPost = true)
        {
            registry.Register(new ModuleDefinition
            {
                Key = key, TitleKey = titleKey, PermissionPrefix = "Inventory", ViewModelType = viewModel,
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
                    PrintTitle = printTitle,
                    TitleKey = addTitleKey ?? titleKey, TitleEditKey = editTitleKey ?? titleKey,
                    DocumentKind = pullSource == null ? null : key,
                    ServiceType = service, DtoType = typeof(CreateStockAdjustmentDto), LineDtoType = typeof(CreateStockAdjustmentLineDto),
                    LinesPropertyName = nameof(CreateStockAdjustmentDto.Lines),
                    AllowPost = allowPost,
                    AffectsStock = stock,
                    PullSources = pullSource == null ? new List<PullSource>() : new List<PullSource> { pullSource },
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
