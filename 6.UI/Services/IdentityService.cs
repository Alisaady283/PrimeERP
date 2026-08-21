using PrimeERP.Platform.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Settings;
using ThemeMode = PrimeERP.Platform.Design.ThemeMode;

namespace PrimeERP.UI.Services
{
    /// <summary>
    /// راجع IIdentityService للتوثيق.
    ///
    /// ⚠️ آلية التبديل في Apply — مُثبتة تجريبياً لا افتراضاً (راجع IdentityServiceTests + ⚠️ توقف 3 و⚠️ توقف 5
    /// في ARCHITECTURE.md لتاريخ المحاولات الفاشلة والمشكلة الجذرية). القاعدتان الحرجتان معاً:
    ///
    /// (1) تعيين Application.Resources لقاموس **فارغ** جديد أولاً (فيصبح حياً)، ثم بناء MergedDictionaries
    ///     الخاصة به كخطوة منفصلة لاحقة — لا قاموس جاهز مسبقاً بالكامل يُعيَّن دفعة واحدة.
    /// (2) ملفات حزمة الهوية الستة (Identity/{key}/Primitives.*.xaml) يجب أن تُدرَج **قبل** Theme.xaml في تلك
    ///     الخطوة، لا بعده ولا كاستبدال لاحق — Theme.xaml (وبالتالي Semantic.Light.xaml) يُجمِّد أي مورد مُركَّب
    ///     يشير لمفتاح L1 متداخل (مثال DropShadowEffect "ShadowMd"، BlurRadius فيه = DynamicResource
    ///     P.Shadow.Md.Blur) على أول قيمة يراها عند أول تحميل له — لا يُعيد تقييمها أبداً لاحقاً مهما تغيّرت
    ///     حزمة الهوية بعد ذلك، بخلاف خاصية محلية مباشرة على FrameworkElement (تُعاد دائماً بشكل صحيح). فلو
    ///     حُمِّل Theme.xaml بحزمة قديمة ولو للحظة قبل استبدالها، تتجمّد "ShadowMd" (وأي مورد مُركَّب مشابه)
    ///     على الهوية الخاطئة للأبد — لا يُصلحها InvalidateProperty ولا SetResourceReference لاحق مهما حاولت.
    /// </summary>
    public class IdentityService : IIdentityService
    {
        private readonly ISettingsService _settings;
        private const string ThemeDictSuffix = "Theme.xaml";

        private static readonly string[] PrimitiveFiles =
        {
            "Primitives.Color.xaml",
            "Primitives.Type.xaml",
            "Primitives.Space.xaml",
            "Primitives.Shape.xaml",
            "Primitives.Motion.xaml",
            "Primitives.Elevation.xaml"
        };

        public IdentityService(ISettingsService settings) => _settings = settings;

        // القيم Str.* هنا مفاتيح ترجمة (تُحلّ في Available فقط، وقت الاستخدام) لا نصاً نهائياً — الحزمة الثابتة تحمل المفاتيح فقط.
        private static readonly List<(string Key, string NameArKey, string NameEn)> Packs = new()
        {
            ("Default",   "Str.Identity.Default",   "Default"),
            ("Corporate", "Str.Identity.Corporate", "Corporate")
        };

        public string CurrentIdentity { get; private set; } = "Default";

        public ThemeMode CurrentMode { get; private set; } = ThemeMode.Light;

        public event Action IdentityChanged;

        public List<IdentityPack> Available() => Packs
            .Select(p => new IdentityPack { Key = p.Key, DisplayNameAr = LocalizationService.Get(p.NameArKey), DisplayNameEn = p.NameEn })
            .ToList();

        public Result Apply(string identityKey)
        {
            if (Packs.All(p => p.Key != identityKey))
                return Result.Fail(LocalizationService.Get("Str.Identity.NotFound"), ErrorCode.NotFound);

            var app = System.Windows.Application.Current;
            if (app != null)
            {
                // Uri مطلق (pack://application:,,,/{asm};component/...) لا نسبي عمداً — نفس اصطلاح PrintService.Theme:
                // Uri نسبي يعتمد على Application.ResourceAssembly (يُضبط تلقائياً وصحيحاً في PrimeERP.exe الحقيقي، لكن
                // خاصية أحادية الضبط لعمر العملية بالكامل — أي مضيف اختباري ينشئ Application قبل هذه الخدمة يجمّدها
                // على تجميعة خاطئة بلا طريقة لتصحيحها لاحقاً؛ المطلق يتجاوز هذه الهشاشة كلياً في كل بيئة.
                var asmName = typeof(IdentityService).Assembly.GetName().Name;

                // القواميس الأخرى المدموجة سابقاً فوق Theme.xaml (نصوص اللغة، تراكب الوضع الداكن) — تُحفظ لإعادة
                // إضافتها بعد إعادة البناء أدناه. ⚠️ يجب استبعاد ملفات حزمة الهوية القديمة أيضاً هنا لا Theme.xaml
                // فقط، وإلا تُعاد إضافتها هي نفسها في النهاية (أعلى أولوية) فتطغى على الحزمة الجديدة بالكامل.
                var preserved = app.Resources.MergedDictionaries
                    .Where(d => d.Source == null ||
                                (!d.Source.OriginalString.EndsWith(ThemeDictSuffix) &&
                                 !PrimitiveFiles.Any(f => d.Source.OriginalString.EndsWith(f))))
                    .ToList();

                // ⚠️ الترتيب هنا حرج بجزأيه — راجع تعليق التوثيق أعلى الكلاس: (1) قاموس فارغ جديد أولاً،
                // (2) الهوية تُدرَج قبل Theme.xaml داخل نفس القاموس الحيّ، لا كاستبدال لاحق على قاموس Theme
                // جاهز مسبقاً.
                app.Resources = new ResourceDictionary();
                var dicts = app.Resources.MergedDictionaries;

                foreach (var file in PrimitiveFiles)
                {
                    dicts.Add(new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/{asmName};component/5.Design/Identity/{identityKey}/{file}", UriKind.Absolute)
                    });
                }

                dicts.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/{asmName};component/5.Design/Theme.xaml", UriKind.Absolute) });

                foreach (var old in preserved)
                    dicts.Add(old);
            }

            CurrentIdentity = identityKey;
            _settings.Set(SettingKeys.UI.Identity, identityKey);
            IdentityChanged?.Invoke();
            return Result.Ok();
        }

        public Result ApplyMode(ThemeMode mode)
        {
            var resolved = mode == ThemeMode.Auto ? ResolveSystemMode() : mode == ThemeMode.Dark ? AppThemeMode.Dark : AppThemeMode.Light;
            ThemeService.Apply(resolved);

            CurrentMode = mode;
            _settings.Set(SettingKeys.UI.Theme, mode.ToString());
            IdentityChanged?.Invoke();
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
                // القيم الافتراضية للخاصيتين CurrentIdentity/CurrentMode تبقى سارية بصمت — راجع تعليق التوثيق أعلاه.
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
