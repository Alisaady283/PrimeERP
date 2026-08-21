using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Design;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Services.Design
{
    /// <summary>
    /// يثبت أن تبديل حزمة الهوية (IdentityService.Apply) يغيّر فعلياً الأبعاد الخمسة (لون/خط/مسافة/نصف قطر/ظل)
    /// التي تحلّها عناصر واجهة حيّة — لا مجرد أن القاموس يحمل القيمة الصحيحة نظرياً. هذا بالضبط القيد التقني
    /// الذي طلب التوجيه اختباره تجريبياً قبل التعميم (راجع تعليق التوثيق في IdentityService.cs وARCHITECTURE.md
    /// ⚠️ توقف 3 و⚠️ توقف 5): ثلاث محاولات فشلت تجريبياً قبل الحل الصحيح — (1) تعديل قاموس متداخل داخل
    /// Theme.xaml مباشرة، (2) استبدال Application.Resources بالكامل بشجرة جاهزة مسبقاً دفعة واحدة، (3) تحميل
    /// Theme.xaml بحزمة قديمة ثم استبدال ملفات الهوية كخطوة لاحقة منفصلة (نجح هذا مع خصائص FrameworkElement
    /// المباشرة كـ Background/Padding/CornerRadius، لكنه يُجمِّد أي مورد مُركَّب متداخل كـ DropShadowEffect
    /// "ShadowMd" على أول قيمة يراها للأبد — راجع ⚠️ توقف 5 لتفاصيل هذا القيد في ResourceDictionary نفسها).
    /// الحل الصحيح: تعيين Application.Resources لقاموس فارغ جديد، ثم إدراج ملفات الهوية الستة **قبل**
    /// Theme.xaml داخل نفس القاموس — لا استبدال لاحق أبداً.
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
        public void Apply_LiveSwap_ChangesAllFiveDimensionsOnAnAlreadyRenderedElement()
        {
            StaThreadHelper.Run(() =>
            {
                if (System.Windows.Application.Current == null) new System.Windows.Application();

                // Default هو نفسه ما يبنيه Apply فعلياً — لا بناء يدوي مواز قد ينحرف عن السلوك الحقيقي.
                _service.Apply("Default");

                var border = new Border();
                border.SetResourceReference(Border.BackgroundProperty, "BrandDefault");
                border.SetResourceReference(Border.PaddingProperty, "P.Space.4.Uniform");
                border.SetResourceReference(Border.CornerRadiusProperty, "P.Radius.Md");
                border.SetResourceReference(Border.EffectProperty, "ShadowMd");

                var textStyle = new Style(typeof(TextBlock));
                textStyle.Setters.Add(new Setter(TextBlock.FontFamilyProperty, new DynamicResourceExtension("P.Font.Family.Primary")));
                textStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, new DynamicResourceExtension("P.Font.Size.500")));

                var text = new TextBlock { Style = textStyle };
                border.Child = text;

                var window = new Window { Content = border, Width = 80, Height = 80, ShowInTaskbar = false, WindowStyle = WindowStyle.None, ShowActivated = false };
                try
                {
                    window.Show();

                    Assert.Equal(Hex("#2563EB"), ((SolidColorBrush)border.Background).Color); // P.Color.Brand.600 — Default
                    Assert.Equal(16d, border.Padding.Left); // P.Space.4.Uniform — Default
                    Assert.Equal(6d, border.CornerRadius.TopLeft); // P.Radius.Md — Default
                    Assert.Equal(12d, ((DropShadowEffect)border.Effect).BlurRadius); // P.Shadow.Md.Blur — Default
                    Assert.Equal("Segoe UI, Tahoma, Arial", text.FontFamily.Source); // P.Font.Family.Primary — Default
                    Assert.Equal(14d, text.FontSize); // P.Font.Size.500 — Default

                    var applied = _service.Apply("Corporate");
                    Assert.True(applied.IsSuccess);

                    // إعادة تقييم مرجع المورد على عنصر حيّ تُنفَّذ عبر Dispatcher لا فوراً بشكل متزامن — تفريغ
                    // الطابور قبل القراءة، وإلا القيمة المقروءة قد تكون سابقة للتبديل (اعتماد على توقيت الجدولة).
                    System.Windows.Application.Current.Dispatcher.Invoke(new Action(() => { }), System.Windows.Threading.DispatcherPriority.Background);

                    // ===== الأبعاد الخمسة — بلا إعادة إنشاء border/text، بلا تعديل ملف قطعة واحد =====
                    Assert.Equal(Hex("#7C3AED"), ((SolidColorBrush)border.Background).Color);      // ✓ اللون
                    Assert.Equal(15d, text.FontSize);                                              // ✓ الخط (حجم)
                    Assert.Equal("Calibri, Segoe UI, Tahoma", text.FontFamily.Source);              // ✓ الخط (عائلة)
                    Assert.Equal(20d, border.Padding.Left);                                        // ✓ المسافة
                    Assert.Equal(3d, border.CornerRadius.TopLeft);                                 // ✓ نصف القطر
                    Assert.Equal(16d, ((DropShadowEffect)border.Effect).BlurRadius);                // ✓ الظل
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

        // ===== فحوص نصية على ملفات L2/L3/L4 — لا تحتاج WPF Application حيّة =====

        private static string DesignRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "5.Design")))
                dir = dir.Parent;
            return Path.Combine(dir!.FullName, "5.Design");
        }

        [Fact]
        public void SemanticDark_RedefinesEveryKeyInSemanticLight()
        {
            var lightKeys = ExtractKeys(Path.Combine(DesignRoot(), "Semantic", "Semantic.Light.xaml"));
            var darkKeys = ExtractKeys(Path.Combine(DesignRoot(), "Semantic", "Semantic.Dark.xaml"));

            var missing = lightKeys.Except(darkKeys).ToList();
            Assert.True(missing.Count == 0, $"مفاتيح ناقصة في Semantic.Dark.xaml: {string.Join(", ", missing)}");
        }

        [Fact]
        public void ComponentAndStyleLayers_ContainNoLiteralHexColors()
        {
            var files = Directory.GetFiles(Path.Combine(DesignRoot(), "Components"), "*.xaml")
                .Concat(Directory.GetFiles(Path.Combine(DesignRoot(), "Styles"), "*.xaml"))
                .Concat(Directory.GetFiles(Path.Combine(DesignRoot(), "Semantic"), "*.xaml"));

            var hexPattern = new Regex(@"#[0-9A-Fa-f]{6,8}\b");
            var offenders = new List<string>();

            foreach (var file in files)
            {
                foreach (var line in File.ReadLines(file))
                {
                    if (line.TrimStart().StartsWith("<!--")) continue; // تعليقات توثيقية قد تذكر Hex كمرجع تاريخي
                    if (hexPattern.IsMatch(line))
                        offenders.Add($"{Path.GetFileName(file)}: {line.Trim()}");
                }
            }

            Assert.True(offenders.Count == 0, "قيم Hex حرفية موجودة خارج L1:\n" + string.Join("\n", offenders));
        }

        private static HashSet<string> ExtractKeys(string path) =>
            Regex.Matches(File.ReadAllText(path), "x:Key=\"([^\"]+)\"")
                .Select(m => m.Groups[1].Value)
                .ToHashSet();

        private static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    }
}
