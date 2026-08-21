using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Services.Design
{
    /// <summary>
    /// يثبت أن تبديل حزمة الهوية (IdentityService.Apply) يغيّر فعلياً الألوان التي تحلّها عناصر واجهة حيّة —
    /// لا مجرد أن القاموس يحمل القيمة الصحيحة نظرياً. هذا بالضبط القيد التقني الذي طلب التوجيه اختباره تجريبياً
    /// قبل التعميم (راجع تعليق التوثيق في IdentityService.cs): محاولتان أوليتان (تعديل قاموس متداخل داخل
    /// Theme.xaml، ثم إضافة قاموس بديل لمستوى Application.Resources.MergedDictionaries الأعلى بلا استبدال
    /// Resources نفسه) فشلتا تجريبياً — عنصر Border حيّ متصل بنافذة معروضة فعلياً لم يُحدِّث لونه المُخزَّن رغم
    /// نجاح القراءة المباشرة من القاموس. الحل الوحيد الذي نجح: استبدال Application.Resources بالكامل — هذا ما
    /// ينفّذه IdentityService.Apply فعلياً الآن، ويثبته الاختبار الحيّ أدناه.
    ///
    /// [Collection] يمنع تشغيل هذا الاختبار بالتوازي مع أي اختبار آخر ينشئ System.Windows.Application — WPF
    /// يسمح بمثيل واحد فقط لكل عملية.
    /// </summary>
    [Collection("WpfApplication")]
    public class IdentityServiceTests : IDisposable
    {
        // Apply يستدعي ISettingsService.Set (يحفظ الاختيار) — يحتاج جدول AppSettings حقيقياً، لا مجرد Application حية.
        private readonly TestDatabaseFixture _db = new();
        private readonly IIdentityService _service;

        public IdentityServiceTests() => _service = _db.Services.GetRequiredService<IIdentityService>();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Apply_LiveSwap_ChangesResolvedColorOnAnAlreadyRenderedElement()
        {
            StaThreadHelper.Run(() =>
            {
                if (System.Windows.Application.Current == null) new System.Windows.Application();

                // يطابق بنية App.xaml الحقيقية بالضبط: قاموس علوي فارغ (بلا Source خاص به) يدمج Theme.xaml
                // كعنصر ضمن قائمته — لا قاموس بـ Source=Theme.xaml مباشرة كـ Resources نفسها. الفرق جوهري:
                // IdentityService.Apply يفرّق بين "ما هو Theme.xaml نفسه" و"ما أُضيف فوقه" (نصوص اللغة، الوضع
                // الداكن) بفحص الامتداد "Theme.xaml" على مستوى Resources.MergedDictionaries العلوي تحديداً.
                var asmName = typeof(IdentityService).Assembly.GetName().Name;
                var root = new ResourceDictionary();
                root.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/{asmName};component/5.Design/Theme.xaml", UriKind.Absolute)
                });
                System.Windows.Application.Current.Resources = root;

                var border = new Border();
                border.SetResourceReference(Border.BackgroundProperty, "BrandDefault");

                var window = new Window { Content = border, Width = 50, Height = 50, ShowInTaskbar = false, WindowStyle = WindowStyle.None, ShowActivated = false };
                try
                {
                    window.Show();

                    var defaultColor = ((SolidColorBrush)border.Background).Color;
                    Assert.Equal(Hex("#2563EB"), defaultColor); // Brand.600 من Identity/Default

                    var applied = _service.Apply("Corporate");
                    Assert.True(applied.IsSuccess);

                    // إعادة تقييم مرجع المورد على عنصر حيّ تُنفَّذ عبر Dispatcher لا فوراً بشكل متزامن — تفريغ
                    // الطابور قبل القراءة، وإلا القيمة المقروءة قد تكون سابقة للتبديل (اعتماد على توقيت الجدولة).
                    System.Windows.Application.Current.Dispatcher.Invoke(new Action(() => { }), System.Windows.Threading.DispatcherPriority.Background);

                    var corporateColor = ((SolidColorBrush)border.Background).Color;
                    Assert.Equal(Hex("#7C3AED"), corporateColor); // Brand.600 من Identity/Corporate — نفس العنصر، بلا إعادة إنشائه
                }
                finally
                {
                    _service.Apply("Default"); // يعيد الحالة الافتراضية — لا يترك أثراً لاختبارات لاحقة تُنشئ Application خاصتها
                    window.Close();
                }
            });
        }

        [Fact]
        public void Apply_UnknownIdentityKey_Fails()
        {
            var result = _service.Apply("NoSuchPack");
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Available_ListsDefaultAndCorporate()
        {
            var packs = _service.Available();
            Assert.Contains(packs, p => p.Key == "Default");
            Assert.Contains(packs, p => p.Key == "Corporate");
        }

        private static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    }
}
