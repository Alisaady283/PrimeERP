using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.PageServices.Inventory;
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
                StockEffect.In, CycleFlow.IntoStockVoucher("PurchaseOrder"));

            Register(registry, "GoodsIssue", "Str.Module.GoodsIssue", typeof(GoodsIssueViewModel), typeof(IGoodsIssueService),
                StockEffect.Out, CycleFlow.IntoStockVoucher("PurchaseReturns"));

            Register(registry, "DeliveryNote", "Str.Module.DeliveryNote", typeof(DeliveryNoteViewModel), typeof(IDeliveryNoteService),
                StockEffect.Out, CycleFlow.IntoStockVoucher("SalesOrder"));

            Register(registry, "SalesReceipt", "Str.Module.SalesReceipt", typeof(SalesReceiptViewModel), typeof(ISalesReceiptService),
                StockEffect.In, CycleFlow.IntoStockVoucher("SalesReturns"));
        }

        private static void Register(IModuleRegistry registry, string key, string title, Type viewModel, Type service,
            StockEffect stock, PullSource pullSource) =>
            StockDocumentFactory.Register(registry, key, title, viewModel, service,
                FlowScope.FullCycleOnly, stock, pullSource, allowPost: false);
    }
}
