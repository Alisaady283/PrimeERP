using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.UI.Services
{
    /// <summary>
    /// نقطة وصول واحدة لحاوية DI من داخل قطع الواجهة (Views/Controls) — لا من الخدمات. قيد WPF حقيقي لا
    /// اختيار: عناصر XAML (UserControl/Window) يبنيها محلّل XAML بمُنشئ بلا معاملات، فلا يمكن حقن تبعياتها
    /// عبر الـ constructor كالخدمات. هذا **ليس ServiceLocator بصيغة أخرى**: الفرق الجوهري أن كل خدمة أعمال
    /// (4.Application/1.Platform/6.UI.Services) تُعلن تبعياتها صراحة في الـ constructor (لا تستدعي هذا
    /// الكلاس إطلاقاً) — الاستخدام هنا محصور بحدود طبقة العرض فقط، عند نقطة الاتصال بـ WPF التي لا بديل
    /// آخر لها بلا استثمار إطار IoC أكبر (Prism ونحوه) غير مطلوب هنا. يُهيَّأ مرة واحدة من App.xaml.cs.OnStartup.
    /// </summary>
    public static class UIServices
    {
        public static IServiceProvider Provider { get; private set; }

        public static void Initialize(IServiceProvider provider) => Provider = provider;

        /// <summary>الأكثر استهلاكاً من قطع الواجهة (إظهار/إخفاء أزرار حسب الصلاحية) — اختصار مباشر بدل Provider.GetRequiredService في كل موضع.</summary>
        public static IPermissionService Permissions => Provider.GetRequiredService<IPermissionService>();

        public static PrimeERP.Platform.Design.IIdentityService Identity =>
            Provider?.GetService(typeof(PrimeERP.Platform.Design.IIdentityService)) as PrimeERP.Platform.Design.IIdentityService;
    }
}
