using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Print;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Print;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Services;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>يجلب المستند الكامل من خدمته ثم يطبعه عبر القالب العام — نقطة واحدة تستهلكها شاشة القائمة
    /// وصفحة المستند معاً.</summary>
    public static class DocumentPrinter
    {
        public static void PrintSelected(ModuleDefinition definition, IServiceProvider services, object item)
        {
            var toast = services.GetRequiredService<IToastService>();
            var def = definition.DocumentDialog;

            if (def == null) { toast.Error("لا مستند قابل للطباعة في هذه الشاشة"); return; }
            if (item == null) { toast.Error("اختر مستنداً أولاً"); return; }

            var id = item.GetType().GetProperty("Id")?.GetValue(item);
            if (id == null) { toast.Error("المستند بلا معرّف"); return; }

            var service = services.GetRequiredService(def.ServiceType);
            var getById = DialogRenderer.FindMethod(def.ServiceType, "GetById", typeof(int));
            if (getById == null) { toast.Error($"الخدمة {def.ServiceType.Name} بلا GetById(int)"); return; }

            var result = (Result)getById.Invoke(service, new object[] { (int)id });
            if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return; }

            var document = result.GetType().GetProperty("Value").GetValue(result);
            // اسم المستند لا عنوان نموذجه: "فاتورة مبيعات" لا "إضافة فاتورة بيع".
            var title = def.PrintTitle ?? LocalizationService.Get(definition.TitleKey);

            // السند ليس جدول سطور بل إقرار مكتوب — له قالبه، وأي مستند آخر يستهلك القالب العام.
            var printable = document is PrimeERP.Application.DTOs.Vouchers.VoucherDetailDto voucher
                ? VoucherPrintTemplate.From(voucher, definition.Key == "Receipts")
                : DocumentPrintTemplate.From(def, title, document);

            var printed = services.GetRequiredService<IPrintService>().PrintPreview(printable);

            if (printed.IsFailure) toast.Error(printed.ErrorMessage);
        }
    }
}
