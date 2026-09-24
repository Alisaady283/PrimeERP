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
    /// <summary>تسجيل سندات الدورة</summary>
    public static class CycleVoucherRegistrations
    {
        public static void RegisterAll(IModuleRegistry registry)
        {
            Register(registry, "GoodsReceipt", "Str.Module.GoodsReceipt", typeof(GoodsReceiptViewModel), typeof(IGoodsReceiptService),
                StockEffect.In, CycleFlow.IntoStockVoucher("PurchaseOrder", "سحب من أمر شراء"));

            Register(registry, "GoodsIssue", "Str.Module.GoodsIssue", typeof(GoodsIssueViewModel), typeof(IGoodsIssueService),
                StockEffect.Out, CycleFlow.IntoStockVoucher("PurchaseReturns", "سحب من مرتجع شراء"));

            Register(registry, "DeliveryNote", "Str.Module.DeliveryNote", typeof(DeliveryNoteViewModel), typeof(IDeliveryNoteService),
                StockEffect.Out, CycleFlow.IntoStockVoucher("SalesOrder", "سحب من أمر توريد"));

            Register(registry, "SalesReceipt", "Str.Module.SalesReceipt", typeof(SalesReceiptViewModel), typeof(ISalesReceiptService),
                StockEffect.In, CycleFlow.IntoStockVoucher("SalesReturns", "سحب من مرتجع بيع"));
        }

        private static void Register(IModuleRegistry registry, string key, string title, Type viewModel, Type service,
            StockEffect stock, PullSource pullSource) =>
            StockDocumentFactory.Register(registry, key, title, viewModel, service,
                FlowScope.FullCycleOnly, stock, pullSource, allowPost: false);
    }
}
