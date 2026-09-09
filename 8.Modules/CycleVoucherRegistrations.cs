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
            StockEffect stock, PullSource pullSource) =>
            StockDocumentFactory.Register(registry, key, title, viewModel, service,
                FlowScope.FullCycleOnly, stock, pullSource, allowPost: false);
    }
}
