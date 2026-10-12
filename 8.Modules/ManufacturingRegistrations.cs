using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Manufacturing;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.PageServices.Manufacturing;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.Modules
{
    public static class ManufacturingRegistrations
    {
        public static void RegisterAll(IModuleRegistry registry)
        {
            registry.Register(new ModuleDefinition
            {
                Key = "WorkCenters", TitleKey = "Str.Module.WorkCenters", PermissionPrefix = "Manufacturing",
                ViewModelFactory = s => RowPage.ViewModel<Lookup<WorkCenter>>(s, "Manufacturing"),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(WorkCenter.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(WorkCenter.Name), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.WorkCenter.CostPerHour"), Binding = nameof(WorkCenter.CostPerHour), Width = 130, Align = ColumnAlign.Center, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.Active"), Binding = nameof(WorkCenter.IsActive), Width = 80, Align = ColumnAlign.Center },
                },
                Dialog = RowPage.Dialog<Lookup<WorkCenter>>("Str.WorkCenters.Add", new List<FieldDefinition>
                {
                    new() { Key = nameof(WorkCenter.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                    new() { Key = nameof(WorkCenter.CostPerHour), LabelKey = "Str.WorkCenter.CostPerHour", Kind = FieldKind.Number },
                    new() { Key = nameof(WorkCenter.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
                }, "Str.WorkCenters.Edit", gridColumns: 1)
            });

            registry.Register(new ModuleDefinition
            {
                Key = "BillsOfMaterials", TitleKey = "Str.Module.BillsOfMaterials", PermissionPrefix = "Manufacturing",
                ViewModelFactory = s => Documents<BomDto, BomFilter, IBillOfMaterialsService>(s, (svc, p, n, f) => svc.GetPaged(p, n, f), (svc, id) => svc.Delete(id), d => d.Id, (f, t) => f.SearchText = t),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(BomDto.Code), Width = 100, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(BomDto.Name), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Bom.Product"), Binding = nameof(BomDto.ProductName), Width = 200 },
                    new() { Header = LocalizationService.Get("Str.Qty"), Binding = nameof(BomDto.Qty), Width = 90, Align = ColumnAlign.Center, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.Active"), Binding = nameof(BomDto.IsActive), Width = 80, Align = ColumnAlign.Center },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    TitleKey = "Str.Bom.Add", TitleEditKey = "Str.Bom.Edit",
                    ServiceType = typeof(IBillOfMaterialsService), DtoType = typeof(CreateBomDto), LineDtoType = typeof(CreateBomLineDto),
                    LinesPropertyName = nameof(CreateBomDto.Lines),
                    HeaderFields = new()
                    {
                        new() { Key = nameof(CreateBomDto.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                        new() { Key = nameof(CreateBomDto.ProductCode), LabelKey = "Str.Bom.Product", Kind = FieldKind.Picker, PickerType = "Product", IsRequired = true },
                        new() { Key = nameof(CreateBomDto.Qty), LabelKey = "Str.Bom.Qty", Kind = FieldKind.Number, Positive = true, DefaultValue = 1m },
                        new() { Key = nameof(CreateBomDto.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
                        new() { Key = nameof(CreateBomDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300 },
                    },
                    LineFields = Lines(waste: true)
                }
            });

            RegisterOrders(registry, "ProductionOrders", "Str.Module.ProductionOrders", "Str.Production", typeof(IProductionOrderService),
                s => Documents<ProductionOrderDto, ProductionOrderFilter, IProductionOrderService>(s, (svc, p, n, f) => svc.GetPaged(p, n, f), (svc, id) => svc.Delete(id), d => d.Id, (f, t) => f.SearchText = t));
            RegisterOrders(registry, "Unbuild", "Str.Module.Unbuild", "Str.Unbuild", typeof(IUnbuildService),
                s => Documents<ProductionOrderDto, ProductionOrderFilter, IUnbuildService>(s, (svc, p, n, f) => svc.GetPaged(p, n, f), (svc, id) => svc.Delete(id), d => d.Id, (f, t) => f.SearchText = t));

            StockDocumentFactory.Register(registry, "Scrap", "Str.Module.Scrap", null, typeof(IScrapService), FlowScope.Both,
                addTitleKey: "Str.Scrap.Add", editTitleKey: "Str.Scrap.Edit",
                viewModelFactory: s => Documents<StockAdjustmentDto, StockAdjustmentFilter, IScrapService>(s, (svc, p, n, f) => svc.GetPaged(p, n, f), (svc, id) => svc.Delete(id), d => d.Id, (f, t) => f.SearchText = t),
                permissionPrefix: "Manufacturing");
        }

        private static void RegisterOrders(IModuleRegistry registry, string key, string titleKey, string strings, System.Type service,
            System.Func<System.IServiceProvider, object> viewModel) => registry.Register(new ModuleDefinition
        {
            Key = key, TitleKey = titleKey, PermissionPrefix = "Manufacturing",
            ViewModelFactory = viewModel,
            Columns = new()
            {
                new() { Header = LocalizationService.Get("Str.DocNo"), Binding = nameof(ProductionOrderDto.OrderNo), Width = 110 },
                new() { Header = LocalizationService.Get("Str.InvoiceDate"), Binding = nameof(ProductionOrderDto.OrderDate), Width = 110, Format = "yyyy-MM-dd" },
                new() { Header = LocalizationService.Get("Str.Bom.Product"), Binding = nameof(ProductionOrderDto.ProductName), Width = 200, IsStarWidth = true },
                new() { Header = LocalizationService.Get("Str.Qty"), Binding = nameof(ProductionOrderDto.Qty), Width = 90, Align = ColumnAlign.Center, Format = "N2" },
                new() { Header = LocalizationService.Get("Str.Production.Materials"), Binding = nameof(ProductionOrderDto.MaterialsCost), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
                new() { Header = LocalizationService.Get("Str.Production.Operations"), Binding = nameof(ProductionOrderDto.OperationsCost), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
                new() { Header = LocalizationService.Get("Str.Production.Total"), Binding = nameof(ProductionOrderDto.TotalCost), Width = 120, Align = ColumnAlign.Center, Format = "N2" },
            },
            DocumentDialog = new DocumentDialogDefinition
            {
                TitleKey = $"{strings}.Add", TitleEditKey = $"{strings}.Add",
                ServiceType = service, DtoType = typeof(CreateProductionOrderDto), LineDtoType = typeof(CreateProductionOrderLineDto),
                LinesPropertyName = nameof(CreateProductionOrderDto.Lines),
                OpenBy = new[] { nameof(CreateProductionOrderDto.BomId), nameof(CreateProductionOrderDto.Qty) },
                AffectsStock = StockEffect.In,
                HeaderFields = new()
                {
                    new() { Key = nameof(CreateProductionOrderDto.OrderDate), LabelKey = "Str.InvoiceDate", Kind = FieldKind.Date, IsRequired = true },
                    new() { Key = nameof(CreateProductionOrderDto.BomId), LabelKey = "Str.Production.Bom", Kind = FieldKind.Picker, PickerType = "Bom", IsRequired = true },
                    new() { Key = nameof(CreateProductionOrderDto.Qty), LabelKey = "Str.Qty", Kind = FieldKind.Number, Positive = true },
                    new() { Key = nameof(CreateProductionOrderDto.RawWarehouseId), LabelKey = "Str.Production.RawWarehouse", Kind = FieldKind.Picker, PickerType = "Warehouse", IsRequired = true },
                    new() { Key = nameof(CreateProductionOrderDto.FinishedWarehouseId), LabelKey = "Str.Production.FinishedWarehouse", Kind = FieldKind.Picker, PickerType = "Warehouse", IsRequired = true },
                    new() { Key = nameof(CreateProductionOrderDto.BatchNo), LabelKey = "Str.Batch", Kind = FieldKind.Text, MaxLength = 60, ShownWhen = Editions.TracksExpiry },
                    new() { Key = nameof(CreateProductionOrderDto.ExpiryDate), LabelKey = "Str.ExpiryDate", Kind = FieldKind.Date, ShownWhen = Editions.TracksExpiry },
                    new() { Key = nameof(CreateProductionOrderDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300 },
                },
                LineFields = Lines(waste: false)
            }
        });

        private static List<LineFieldDefinition> Lines(bool waste)
        {
            var lines = new List<LineFieldDefinition>
            {
                new() { Key = "Kind", Header = LocalizationService.Get("Str.Bom.Line"), Kind = FieldKind.Picker, PickerType = "BomLineKind", Width = 120, IsRequired = true },
                new() { Key = "ProductCode", Header = LocalizationService.Get("Str.Product"), Kind = FieldKind.Picker, PickerType = "Product", Width = 220 },
                new() { Key = "WorkCenterId", Header = LocalizationService.Get("Str.Module.WorkCenters"), Kind = FieldKind.Picker, PickerType = "WorkCenter", Width = 160, ShownWhen = Editions.ManufacturingOperations },
                StandardFields.Qty(),
            };
            if (waste) lines.Add(new() { Key = "WastePercent", Header = LocalizationService.Get("Str.Bom.Waste"), Kind = FieldKind.Number, Width = 90 });
            else lines.AddRange(new[] { StandardFields.Batch(), StandardFields.Expiry() });
            lines.Add(new() { Key = "Notes", Header = LocalizationService.Get("Str.Notes"), Kind = FieldKind.Text, Width = 160 });
            return lines;
        }

        private static object Documents<TDto, TFilter, TService>(System.IServiceProvider services,
            System.Func<TService, int, int, TFilter, PrimeERP.Domain.Results.Result<PrimeERP.Domain.Results.PagedResult<TDto>>> page,
            System.Func<TService, int, PrimeERP.Domain.Results.Result> delete, System.Func<TDto, int> id, System.Action<TFilter, string> search)
            where TFilter : new()
        {
            var service = services.GetRequiredService<TService>();
            return RowPage.Documents<TDto, TFilter>(services, "Manufacturing", (p, n, f) => page(service, p, n, f), i => delete(service, i), id, search);
        }
    }
}
