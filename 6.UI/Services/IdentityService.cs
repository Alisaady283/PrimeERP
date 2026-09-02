using PrimeERP.Platform.Localization;
using PrimeERP.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
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
        private const string ThemeSemanticDictSuffix = "Theme.Semantic.xaml";
        private const string DarkDictSuffix = "Semantic.Dark.xaml";

        private static readonly string[] PrimitiveFiles =
        {
            "Primitives.Color.xaml",
            "Primitives.Type.xaml",
            "Primitives.Space.xaml",
            "Primitives.Shape.xaml",
            "Primitives.Motion.xaml",
            "Primitives.Elevation.xaml"
        };

        /// <summary>
        /// ⚠️ توقف 6 — راجع ARCHITECTURE.md: WPF لا يسمح بإعادة تصدير Color حيّة من مفتاح Brush عبر
        /// DynamicResource (يحتاج DependencyProperty على DependencyObject يستضيفه، و`SolidColorBrush.Color`
        /// نفسها تحتاج مصدراً Color بالفعل — Brush لا يُحوَّل تلقائياً). 5.Design/Components/Tokens.*.xaml (L3)
        /// يحتاج Color خام لعدة أسماء L2 (BrandDefault, SurfaceDefault...) المُعرَّفة هناك كـ Brush فقط. الحل
        /// الوحيد العامل فعلياً: استخراج Color من كل Brush برمجياً هنا بعد اكتمال بناء الشجرة (هوية + وضع)،
        /// وحقنها كقيمة Color خام مباشرة تحت مفتاح "{Name}.Color" — لا DynamicResource متداخل، فلا مشكلة تجميد
        /// (توقف 5) ولا خطأ نوع. يُعاد استدعاؤها من Apply وApplyMode معاً — أي تغيير هوية أو وضع يُحدِّثها فوراً.
        /// </summary>
        /// <summary>كل مفتاح Brush في الشجرة المدموجة يحصل على "{Name}.Color" — لا قائمة أسماء يدوية: أي مفتاح
        /// L2 جديد كان يحتاج إضافة يدوية هنا وإلا حُلَّ لونه لشفاف صامت (هيدر الجدول ظهر أبيض على أبيض بهذا
        /// السبب بالضبط).</summary>
        private static void RefreshDerivedColors()
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            var derived = new Dictionary<string, Color>();
            CollectBrushColors(app.Resources, derived);
            foreach (var (name, color) in derived)
                app.Resources[$"{name}.Color"] = color;
        }

        private static void CollectBrushColors(ResourceDictionary dict, Dictionary<string, Color> into)
        {
            foreach (var merged in dict.MergedDictionaries)
                CollectBrushColors(merged, into);

            foreach (var key in dict.Keys)
            {
                if (key is not string name || name.EndsWith(".Color")) continue;
                if (dict[key] is SolidColorBrush brush) into[name] = brush.Color;
            }
        }

        /// <summary>
        /// نفس مشكلة ⚠️ توقف 6 لكن لأنواع قيمة (CornerRadius/double) لا Brush: WPF لا يسمح بإعادة تصدير مورد
        /// من نوع قيمة عبر DynamicResource متسلسل (يُحوَّل من نص عبر TypeConverter عند التحميل، لا مرجعاً حياً) —
        /// فأي مفتاح L3 يريد "متابعة" قيمة L1 تتغيّر مع الهوية (P.Radius.Md: 6 Default / 3 Corporate،
        /// P.Font.Size.300: 12 Default / 13 Corporate) لا يمكن أن يكون مجرد `&lt;CornerRadius x:Key="C.Input.Radius"&gt;
        /// {DynamicResource P.Radius.Md}&lt;/CornerRadius&gt;` — نفس الحل: نسخ القيمة المُحلولة فعلياً برمجياً هنا
        /// تحت اسم L3، تُعاد كل Apply (لا ApplyMode — هذه قيم تتبع الهوية لا الوضع الفاتح/الداكن).</summary>
        private static readonly (string L1Key, string L3Key)[] DerivedDimensions =
        {
            ("P.Radius.Md",     "C.Input.Radius"),
            ("P.Font.Size.300", "C.Input.FontSize"),
        };

        private static void RefreshDerivedDimensions()
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            foreach (var (l1Key, l3Key) in DerivedDimensions)
            {
                var value = app.TryFindResource(l1Key);
                if (value != null)
                    app.Resources[l3Key] = value;
            }
        }

        public IdentityService(ISettingsService settings) => _settings = settings;

        // القيم Str.* هنا مفاتيح ترجمة (تُحلّ في Available فقط، وقت الاستخدام) لا نصاً نهائياً — الحزمة الثابتة تحمل المفاتيح فقط.
        private static readonly List<(string Key, string NameArKey, string NameEn)> Packs = new()
        {
            ("Signature", "Str.Identity.Signature", "Signature"),
            ("Default",   "Str.Identity.Default",   "Default"),
            ("Corporate", "Str.Identity.Corporate", "Corporate")
        };

        private const string IdentityBaseline = "Signature";

        public string CurrentIdentity { get; private set; } = IdentityBaseline;

        public ThemeMode CurrentMode { get; private set; } = ThemeMode.Light;

        public event Action IdentityChanged;

        public List<IdentityPack> Available() => Packs
            .Select(p => new IdentityPack { Key = p.Key, DisplayNameAr = LocalizationService.Get(p.NameArKey), DisplayNameEn = p.NameEn })
            .ToList();

        public Result Apply(string identityKey)
        {
            if (Packs.All(p => p.Key != identityKey))
                return Result.Fail(LocalizationService.Get("Str.Identity.NotFound"), ErrorCode.NotFound);

            BuildResourceTree(identityKey, ResolvedMode());

            CurrentIdentity = identityKey;
            _settings.Set(SettingKeys.UI.Identity, identityKey);
            IdentityChanged?.Invoke();
            return Result.Ok();
        }

        private AppThemeMode ResolvedMode() =>
            CurrentMode == ThemeMode.Auto ? ResolveSystemMode()
                                          : CurrentMode == ThemeMode.Dark ? AppThemeMode.Dark : AppThemeMode.Light;

        // الوضع جزء من بناء الشجرة لا طبقة تُضاف فوقها: رموز L3 تُبنى من مفاتيح ".Color" المُشتقة، فإضافة
        // Semantic.Dark بعد L3 تترك تلك الرموز مجمَّدة على قيم الفاتح — وهو سبب أن الوضع الليلي كان بلا أثر.
        private void BuildResourceTree(string identityKey, AppThemeMode mode)
        {
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
                                 !d.Source.OriginalString.EndsWith(ThemeSemanticDictSuffix) &&
                                 !d.Source.OriginalString.EndsWith(DarkDictSuffix) &&
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

                RefreshDerivedDimensions();

                // L2 وحدها أولاً (⚠️ توقف 6) — RefreshDerivedColors تحتاج Brush الدلالية مستقرة قبل L3، وL3 جزء
                // من Theme.xaml التالي مباشرة؛ لو دُمج L2+L3 معاً هنا (كما كان قبل توقف 6) قد يُقيَّم مورد L3
                // المُركَّب (SolidColorBrush.Color) قبل استقرار مفاتيح ".Color" فيتجمَّد على قيمة ناقصة/خاطئة.
                dicts.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/{asmName};component/5.Design/Theme.Semantic.xaml", UriKind.Absolute) });

                if (mode == AppThemeMode.Dark)
                    dicts.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/{asmName};component/5.Design/Semantic/Semantic.Dark.xaml", UriKind.Absolute) });

                RefreshDerivedColors();

                dicts.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/{asmName};component/5.Design/Theme.xaml", UriKind.Absolute) });

                foreach (var old in preserved)
                    dicts.Add(old);
            }

            ThemeService.SetMode(mode);
        }

        public Result ApplyMode(ThemeMode mode)
        {
            var resolved = mode == ThemeMode.Auto ? ResolveSystemMode() : mode == ThemeMode.Dark ? AppThemeMode.Dark : AppThemeMode.Light;
            BuildResourceTree(CurrentIdentity, resolved);

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
                // SettingSeeder لا يستبدل قيمة قائمة، فالقاعدة القديمة تبقى على هويتها بلا هذه الترقية.
                if (_settings.Get(SettingKeys.UI.IdentityBaseline, "") != IdentityBaseline)
                {
                    _settings.Set(SettingKeys.UI.Identity, IdentityBaseline);
                    _settings.Set(SettingKeys.UI.IdentityBaseline, IdentityBaseline);
                }

                var identityKey = _settings.Get(SettingKeys.UI.Identity, IdentityBaseline);
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
