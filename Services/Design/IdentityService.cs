using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using PrimeERP.Core.Common;
using PrimeERP.Services.Settings;

namespace PrimeERP.Services.Design
{
    /// <summary>
    /// راجع IIdentityService للتوثيق.
    ///
    /// ⚠️ آلية التبديل في Apply — مُثبتة تجريبياً لا افتراضاً (راجع IdentityServiceTests + قسم توقف 1 في
    /// MIGRATION_INVENTORY.md). محاولتان أوليتان فشلتا تجريبياً بلا استثناء: (1) تعديل قاموس Primitives.Color.
    /// xaml في مكانه داخل Theme.xaml، و(2) حتى استبدال Application.Resources بالكامل بشجرة **جاهزة مسبقاً
    /// بالكامل** قبل التعيين. السبب: SolidColorBrush كائن مورد مستقل لا FrameworkElement — DynamicResource
    /// المتداخل في Color الخاصة به (لمفتاح P.Color.* داخل Semantic.Light.xaml) لا يُعاد تقييمه لمجرد أن القاموس
    /// الذي يعرّف ذلك المفتاح تغيّر في مكان ما من الشجرة، حتى لو كانت الشجرة النهائية صحيحة القيمة حرفياً.
    /// الترتيب الوحيد الذي أثبت عمله على عنصر حيّ متصل بنافذة معروضة فعلياً: تعيين Application.Resources لقاموس
    /// جديد أولاً (فيصبح حياً)، ثم تعديل MergedDictionaries الخاصة بذلك القاموس نفسه كخطوة منفصلة لاحقة.
    /// </summary>
    public class IdentityService : IIdentityService
    {
        public static readonly IdentityService Instance = new();

        private readonly ISettingsService _settings = SettingsService.Instance;
        private const string ColorDictSuffix = "Primitives.Color.xaml";
        private const string ThemeDictSuffix = "Theme.xaml";

        // القيم Str.* هنا مفاتيح ترجمة (تُحلّ في Available فقط، وقت الاستخدام) لا نصاً نهائياً — الحزمة الثابتة تحمل المفاتيح فقط.
        private static readonly List<(string Key, string NameArKey, string NameEn)> Packs = new()
        {
            ("Default",   "Str.Identity.Default",   "Default"),
            ("Corporate", "Str.Identity.Corporate", "Corporate")
        };

        public string Current { get; private set; } = "Default";

        public ThemeMode CurrentMode { get; private set; } = ThemeMode.Light;

        public List<IdentityPack> Available() => Packs
            .Select(p => new IdentityPack { Key = p.Key, DisplayNameAr = LocalizationService.Get(p.NameArKey), DisplayNameEn = p.NameEn })
            .ToList();

        public Result Apply(string identityKey)
        {
            if (Packs.All(p => p.Key != identityKey))
                return Result.Fail(LocalizationService.Get("Str.Identity.NotFound"), ErrorCode.NotFound);

            var app = Application.Current;
            if (app != null)
            {
                // Uri مطلق (pack://application:,,,/{asm};component/...) لا نسبي عمداً — نفس اصطلاح PrintService.Theme:
                // Uri نسبي يعتمد على Application.ResourceAssembly (يُضبط تلقائياً وصحيحاً في PrimeERP.exe الحقيقي، لكن
                // خاصية أحادية الضبط لعمر العملية بالكامل — أي مضيف اختباري ينشئ Application قبل هذه الخدمة يجمّدها
                // على تجميعة خاطئة بلا طريقة لتصحيحها لاحقاً؛ المطلق يتجاوز هذه الهشاشة كلياً في كل بيئة.
                var asmName = typeof(IdentityService).Assembly.GetName().Name;

                // القواميس الأخرى المدموجة سابقاً فوق Theme.xaml (نصوص اللغة، تراكب الوضع الداكن) — تُحفظ لإعادة
                // إضافتها بعد إعادة البناء أدناه.
                var preserved = app.Resources.MergedDictionaries
                    .Where(d => d.Source == null || !d.Source.OriginalString.EndsWith(ThemeDictSuffix))
                    .ToList();

                // ⚠️ الترتيب هنا حرج، مُثبت تجريبياً لا افتراضاً: يجب أولاً تعيين Application.Resources لقاموس
                // جديد (يجعله "حياً" فعلياً)، ثم تعديل MergedDictionaries الخاصة به كخطوة منفصلة لاحقة — لا بناء
                // الشجرة كاملة أولاً ثم تعيينها دفعة واحدة. التعيين دفعة واحدة (شجرة جاهزة مسبقاً) لا يُعيد تقييم
                // فرش SolidColorBrush المُخزَّنة سلفاً على عناصر متصلة (BrandDefault وغيرها) رغم صحة القيمة داخل
                // القاموس نفسه؛ التعديل على مجموعة MergedDictionaries بعد أن تصبح Resources الفعلية هو وحده ما
                // يُطلق إشعار التغيّر الذي تلتقطه عناصر الواجهة الحيّة. راجع IdentityServiceTests للاختبار الذي أثبت هذا.
                app.Resources = new ResourceDictionary { Source = new Uri($"pack://application:,,,/{asmName};component/Resources/Design/Theme.xaml", UriKind.Absolute) };

                var dicts = app.Resources.MergedDictionaries;
                var existingColor = dicts.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.EndsWith(ColorDictSuffix));
                if (existingColor != null)
                    dicts.Remove(existingColor);
                dicts.Insert(0, new ResourceDictionary { Source = new Uri($"pack://application:,,,/{asmName};component/Resources/Design/Identity/{identityKey}/Primitives.Color.xaml", UriKind.Absolute) });

                foreach (var old in preserved)
                    dicts.Add(old);
            }

            Current = identityKey;
            _settings.Set(SettingKeys.UI.Identity, identityKey);
            return Result.Ok();
        }

        public Result ApplyMode(ThemeMode mode)
        {
            var resolved = mode == ThemeMode.Auto ? ResolveSystemMode() : mode == ThemeMode.Dark ? AppThemeMode.Dark : AppThemeMode.Light;
            ThemeService.Apply(resolved);

            CurrentMode = mode;
            _settings.Set(SettingKeys.UI.Theme, mode.ToString());
            return Result.Ok();
        }

        /// <summary>
        /// بلا رمي — يُستدعى من App.xaml.cs.OnStartup قبل أي ضمان أن قاعدة البيانات (وجدول AppSettings)
        /// جاهزة فعلياً؛ فشل القراءة هنا يعني الإبقاء على الافتراضي (Default/Light، القيم الابتدائية للخاصيتين
        /// أعلاه أصلاً) بدل تعطيل إقلاع التطبيق كاملاً بسبب تفضيل هوية بصرية غير حرج.
        /// </summary>
        public void Initialize()
        {
            try
            {
                var identityKey = _settings.Get(SettingKeys.UI.Identity, "Default");
                Apply(identityKey);

                var modeText = _settings.Get(SettingKeys.UI.Theme, "Light");
                var mode = Enum.TryParse<ThemeMode>(modeText, ignoreCase: true, out var parsed) ? parsed : ThemeMode.Light;
                ApplyMode(mode);
            }
            catch
            {
                // القيم الافتراضية للخاصيتين Current/CurrentMode تبقى سارية بصمت — راجع تعليق التوثيق أعلاه.
            }
        }

        /// <summary>يقرأ تفضيل الوضع الفاتح/الداكن لنظام Windows الحالي (سجلّ AppsUseLightTheme) — لقطة وقت التطبيق، لا متابعة حيّة لتغيّره لاحقاً.</summary>
        private static AppThemeMode ResolveSystemMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int i && i == 0 ? AppThemeMode.Dark : AppThemeMode.Light;
            }
            catch
            {
                return AppThemeMode.Light;
            }
        }
    }
}
