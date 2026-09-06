using System;
using System.Windows;

namespace PrimeERP.Application.Services.Print
{
    /// <summary>
    /// قيم الورق كلها من PrintTheme.xaml — الطباعة والتصدير يقرآن الرقم نفسه فلا تختلف إزاحة عن أخرى.
    /// الورق لا يتبدّل فاتح/داكن، فالقاموس مفرد ثابت يُحمَّل مرة ويُجمَّد.
    /// </summary>
    public static class PaperTheme
    {
        private static ResourceDictionary _theme;

        private static ResourceDictionary Theme
        {
            get
            {
                if (_theme == null)
                {
                    // ⚠️ توقف 11 — مخطَّط pack:// (تسجّله System.Windows.Application ضمن مُنشئها الساكن) لم يكن
                    // مسجَّلاً بعد لو كانت هذه أول لمسة لأي System.Windows.* في العملية كلها (ترتيب اختبارات
                    // xUnit غير حتمي — راجع ARCHITECTURE.md). الضمان الصريح هنا (لا الاعتماد على ترتيب تشغيل
                    // اختبار آخر يلمسها أولاً بالصدفة) يجعل بناء pack:// يعمل دائماً بصرف النظر عمّا سبقه.
                    if (System.Windows.Application.Current == null) new System.Windows.Application();

                    // pack URI صريحة باسم التجميعة — لا تعتمد على Application.ResourceAssembly (قد يكون مضبوطاً
                    // خطأً في مضيف اختبار أنشأ Application قبلها) بخلاف Uri نسبية بسيطة.
                    var asmName = typeof(PaperTheme).Assembly.GetName().Name;
                    var dict = new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/{asmName};component/5.Design/Surfaces/PrintTheme.xaml", UriKind.Absolute)
                    };

                    // _theme مفرد ثابت واحد يُشارَك بين كل الخيوط التي تبني مستند طباعة (STA منفصلة متعددة عبر
                    // StaThreadHelper في الاختبارات، أو نافذة طباعة لاحقة في التطبيق) — Freezable (SolidColorBrush)
                    // يرتبط ضمنياً بخيط إنشائه ما لم يُجمَّد. بلا Freeze هنا، أول استخدام على خيط غير خيط أول تحميل
                    // للثيم يرمي "Cannot use a DependencyObject that belongs to a different thread" — اكتُشف فعلياً
                    // عند إضافة اختبار طباعة ثانٍ (F.2.4) يعمل على خيط STA مختلف عن أول اختبار طباعة (F.1.3).
                    foreach (var value in dict.Values)
                        if (value is Freezable freezable && freezable.CanFreeze)
                            freezable.Freeze();

                    _theme = dict;
                }
                return _theme;
            }
        }

        public static T Value<T>(string key) => (T)Theme[key];

        public static object Raw(string key) => Theme[key];

        /// <summary>القيمة نفسها بنقاط PDF (72 لكل بوصة) بدل وحدة الشاشة (96) — بلا هذا التحويل يبدو
        /// الرقمان مختلفَين وهما متساويان فعلياً، فيُقدَّر لكلٍّ رقم وتفترق الإزاحات.</summary>
        public static float Points(string key) => (float)(Value<double>(key) * 0.75);
    }
}
