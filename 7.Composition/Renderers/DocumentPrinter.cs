using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Print;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Print;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Services;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>يجلب المستند الكامل من خدمته ثم يطبعه عبر القالب العام — نقطة واحدة تستهلكها شاشة القائمة
    /// وصفحة المستند معاً.</summary>
    public static class DocumentPrinter
    {
        private const string ReceiptNarrative = "استلمنا من السيد / السادة: {Party}";
        private const string PaymentNarrative = "ادفعوا بموجب هذا الأمر إلى السيد / السادة: {Party}";

        private static IPrintable VoucherDocument(PrimeERP.Application.DTOs.Vouchers.VoucherDetailDto voucher,
            bool isReceipt, PrimeERP.Platform.Settings.ISettingsProvider settings)
        {
            var details = new System.Collections.Generic.Dictionary<string, string>
            {
                ["طريقة الدفع"] = voucher.MethodName,
                ["الخزينة / البنك"] = voucher.TreasuryName,
                ["وذلك عن"] = string.IsNullOrWhiteSpace(voucher.Notes) ? "………………" : voucher.Notes,
            };

            if (!string.IsNullOrWhiteSpace(voucher.Reference))
                details[voucher.Method == PrimeERP.Domain.Enums.PaymentMethod.Cheque ? "رقم الشيك" : "المرجع"] = voucher.Reference;

            return PrintDocuments.Narrative(new NarrativeDocument
            {
                Title = isReceipt ? "سند قبض" : "سند صرف",
                Number = voucher.VoucherNo,
                Template = isReceipt ? ReceiptNarrative : PaymentNarrative,
                Values = new() { ["Party"] = string.IsNullOrWhiteSpace(voucher.PartyName) ? "………………" : voucher.PartyName },
                Amount = voucher.Amount,
                Currency = settings.Get(PrimeERP.Platform.Settings.SettingKeys.Financial.CurrencyName, "جنيه"),
                SubUnit = settings.Get(PrimeERP.Platform.Settings.SettingKeys.Financial.CurrencySubUnit, "قرش"),
                Header = new()
                {
                    ["رقم السند"] = voucher.VoucherNo,
                    ["التاريخ"] = voucher.VoucherDate.ToString("yyyy-MM-dd"),
                },
                Details = details,
                Signatures = isReceipt
                    ? new() { "المستلِم", "المحاسب", "المدير" }
                    : new() { "المستلِم", "أمين الخزينة", "المحاسب", "المدير" }
            });
        }


        public static void PrintSelected(ModuleDefinition definition, IServiceProvider services, object item)
        {
            var toast = services.GetRequiredService<IToastService>();
            var def = definition.DocumentDialog;

            if (def == null) { toast.Error("لا مستند قابل للطباعة في هذه الشاشة"); return; }
            if (item == null) { toast.Error("اختر مستنداً أولاً"); return; }

            var id = item.GetType().GetProperty("Id")?.GetValue(item);
            if (id == null) { toast.Error("المستند بلا معرّف"); return; }

            var settings = services.GetRequiredService<PrimeERP.Platform.Settings.ISettingsProvider>();
            var service = services.GetRequiredService(def.ServiceType);
            var getById = DialogRenderer.FindMethod(def.ServiceType, "GetById", typeof(int));
            if (getById == null) { toast.Error($"الخدمة {def.ServiceType.Name} بلا GetById(int)"); return; }

            var result = (Result)getById.Invoke(service, new object[] { (int)id });
            if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return; }

            var document = result.GetType().GetProperty("Value").GetValue(result);
            // اسم المستند لا عنوان نموذجه: "فاتورة مبيعات" لا "إضافة فاتورة بيع".
            var title = def.PrintTitle ?? LocalizationService.Get(definition.TitleKey);

            var printable = document is PrimeERP.Application.DTOs.Vouchers.VoucherDetailDto voucher
                ? VoucherDocument(voucher, definition.Key == "Receipts", settings)
                : PrintDocuments.Trade(def, title, document);

            var printed = services.GetRequiredService<IPrintService>().PrintPreview(printable);

            if (printed.IsFailure) toast.Error(printed.ErrorMessage);
        }
    }
}
