using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Print;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Services;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>يجلب المستند الكامل من خدمته</summary>
    public static class DocumentPrinter
    {
        private static PrintDocuments.PaperOptions PaperFrom(PrimeERP.Platform.Settings.ISettingsProvider settings, object doc)
        {
            var labels = settings.Get(PrimeERP.Platform.Settings.SettingKeys.Print.CopyLabels, "");
            var number = doc?.GetType().GetProperty("DocNo")?.GetValue(doc) as string
                      ?? doc?.GetType().GetProperty("InvoiceNo")?.GetValue(doc) as string;

            return new PrintDocuments.PaperOptions
            {
                CopyLabels = labels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                LinesPerPage = settings.Get(PrimeERP.Platform.Settings.SettingKeys.Print.LinesPerPage, 0),
                Terms = settings.Get(PrimeERP.Platform.Settings.SettingKeys.Print.Terms, ""),
                BarcodeText = number
            };
        }

        public static IPrintable VoucherDocument(PrimeERP.Application.DTOs.Vouchers.VoucherDetailDto voucher,
            bool isReceipt, PrimeERP.Platform.Settings.ISettingsProvider settings)
        {
            var isCheque = voucher.Method == PrimeERP.Domain.Enums.PaymentMethod.Cheque;
            var words = PrimeERP.Domain.Helpers.ArabicNumberToWords.Convert(voucher.Amount,
                settings.Get(PrimeERP.Platform.Settings.SettingKeys.Financial.CurrencyName, "جنيه"),
                settings.Get(PrimeERP.Platform.Settings.SettingKeys.Financial.CurrencySubUnit, "قرش"));

            var sections = new System.Collections.Generic.List<PrintSection>
            {
                new() { Type = PrintSectionType.Callout, Text = $"{voucher.Amount:N2}", Variant = StatusVariant.Neutral },

                Line("التاريخ", voucher.VoucherDate.ToString("yyyy-MM-dd")),
                Line(isReceipt ? "استلمنا من السيد" : "صرفنا إلى السيد", voucher.PartyName),
                Line("مبلغاً وقدره", $"{words} لا غير"),
                Pair("نقداً / شيك رقم", isCheque ? voucher.Reference : null,
                     "مسحوب على بنك", voucher.TreasuryName),
                Line("وذلك عن", voucher.Notes),
            };

            return new VoucherPaper
            {
                Title = isReceipt ? "سند قبض" : "سند صرف",
                Subtitle = voucher.VoucherNo,
                Sections = sections,
                Signatures = isReceipt
                    ? new() { "المحاسب", "الاعتماد" }
                    : new() { "المستلِم", "المحاسب", "الاعتماد" }
            };
        }

        public static System.Collections.Generic.List<string> StockSignatures(DocumentDialogDefinition def)
        {
            if (def.AffectsStock == StockEffect.None) return null;

            return def.AffectsStock == StockEffect.Out
                ? new System.Collections.Generic.List<string> { "المستلِم", "أمين المخزن", "الاعتماد" }
                : new System.Collections.Generic.List<string> { "أمين المخزن", "الاعتماد" };
        }

        private static PrintSection Line(string label, string value) => new()
        {
            Type = PrintSectionType.Text,
            FillParts = new System.Collections.Generic.List<string> { Written(label, value) },
            FillShares = new System.Collections.Generic.List<double> { 1 },
        };

        private static PrintSection Pair(string firstLabel, string firstValue, string secondLabel, string secondValue) => new()
        {
            Type = PrintSectionType.Text,
            FillParts = new System.Collections.Generic.List<string>
            { Written(firstLabel, firstValue), Written(secondLabel, secondValue) },
            FillShares = new System.Collections.Generic.List<double> { 2.0 / 3, 1.0 / 3 },
        };

        private static string Written(string label, string value) =>
            $"{label} : {(string.IsNullOrWhiteSpace(value) ? "" : "...." + value + " ")}";

        /// <summary>ورقة السند</summary>
        private class VoucherPaper : IPrintable
        {
            public string Title { get; init; }
            public string Subtitle { get; init; }
            public System.Collections.Generic.List<PrintSection> Sections { get; init; }
            public System.Collections.Generic.List<string> Signatures { get; init; }

            public string DocumentTitle => Title;
            public string DocumentSubtitle => Subtitle;
            public PrintOrientation Orientation => PrintOrientation.Portrait;

            public System.Collections.Generic.Dictionary<string, string> HeaderFields => null;
            public System.Collections.Generic.Dictionary<string, string> FooterFields => null;

            public bool ShowCompanyHeader => true;
            public bool ShowPageNumbers => false;
            public bool ShowSignatures => true;
            public System.Collections.Generic.List<string> SignatureLabels => Signatures;

            public bool Framed => true;
            public bool HalfPage => true;

            public System.Collections.Generic.List<PrintSection> BuildSections() => Sections;
        }


        public static void PrintSelected(ModuleDefinition definition, IServiceProvider services, object item) =>
            WithDocument(definition, services, item, (printable, toast) =>
            {
                var printed = services.GetRequiredService<IPrintService>().PrintPreview(printable);
                if (printed.IsFailure) toast.Error(printed.ErrorMessage);
            });

        public static void ExportSelected(ModuleDefinition definition, IServiceProvider services, object item) =>
            WithDocument(definition, services, item, (printable, toast) =>
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"{printable.DocumentTitle}-{printable.DocumentSubtitle}",
                    Filter = "PDF (*.pdf)|*.pdf"
                };
                if (dialog.ShowDialog() != true) return;

                var exported = services.GetRequiredService<IPrintService>().ExportToPdf(printable, dialog.FileName);
                if (exported.IsFailure) { toast.Error(exported.ErrorMessage); return; }

                toast.Success($"تم التصدير إلى {System.IO.Path.GetFileName(dialog.FileName)}");
            });

        private static void WithDocument(ModuleDefinition definition, IServiceProvider services, object item,
            Action<IPrintable, IToastService> use)
        {
            var toast = services.GetRequiredService<IToastService>();
            var def = definition.DocumentDialog;

            if (def == null) { toast.Error("لا مستند قابل للطباعة في هذه الشاشة"); return; }
            if (item == null) { toast.Error("اختر مستنداً أولاً"); return; }

            var id = item.GetType().GetProperty("Id")?.GetValue(item);
            if (id == null) { toast.Error("المستند بلا معرّف"); return; }

            var settings = services.GetRequiredService<PrimeERP.Platform.Settings.ISettingsProvider>();
            var service = Resolve.Service(def, services);
            var getById = DialogRenderer.FindMethod(def.ServiceType, "GetById", typeof(int));
            if (getById == null) { toast.Error($"الخدمة {def.ServiceType.Name} بلا GetById(int)"); return; }

            var result = (Result)getById.Invoke(service, new object[] { (int)id });
            if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return; }

            var document = result.GetType().GetProperty("Value").GetValue(result);
            var title = def.PrintTitle ?? LocalizationService.Get(definition.TitleKey);

            var printable = document is PrimeERP.Application.DTOs.Vouchers.VoucherDetailDto voucher
                ? VoucherDocument(voucher, definition.Key == "Receipts", settings)
                : PrintDocuments.Trade(def, title, document, PaperFrom(settings, doc: document),
                    StockSignatures(def));

            use(printable, toast);
        }
    }
}
