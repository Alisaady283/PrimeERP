using System;
using System.Collections.Generic;
using PrimeERP.Domain.Results;

namespace PrimeERP.Platform.Design
{
    public enum ThemeMode
    {
        Light,
        Dark,

        /// <summary>يُحلّ لـ Light/Dark وقت التطبيق عبر إعداد نظام التشغيل (سجلّ Windows AppsUseLightTheme) — لا يُخزَّن كوضع "حي" يتابع تغيّر إعداد النظام لاحقاً بلا إعادة تطبيق.</summary>
        Auto
    }

    public class IdentityPack
    {
        /// <summary>اسم المجلد تحت 5.Design/Identity — يُستخدم حرفياً في بناء مسار الملف، لا اسم عرض.</summary>
        public string Key { get; init; }
        public string DisplayNameAr { get; init; }
        public string DisplayNameEn { get; init; }
    }

    /// <summary>
    /// المالك الوحيد لتبديل حزمة الهوية البصرية (L1 — كل ملفات Primitives.* الستة تحت 5.Design/Identity/{Key})
    /// ووضع الإضاءة (فاتح/داكن). كلاهما يعمل عبر استبدال قاموس موارد في Application.Resources.MergedDictionaries
    /// وقت التشغيل — كل عنصر مرتبط بـ DynamicResource يتحدّث تلقائياً بلا إعادة تحميل أي صفحة أو قطعة.
    /// الواجهة هنا في Platform (لا تعرف WPF)؛ التنفيذ في 6.UI/Services (يحتاج System.Windows.Application).
    /// </summary>
    public interface IIdentityService
    {
        List<IdentityPack> Available();

        /// <summary>يستبدل حزمة الهوية الحالية بحزمة أخرى (اسم مجلد من Available) ويحفظ الاختيار في الإعدادات.</summary>
        Result Apply(string identityKey);

        /// <summary>يبدّل وضع الإضاءة (فاتح/داكن/تلقائي) ويحفظ الاختيار في الإعدادات.</summary>
        Result ApplyMode(ThemeMode mode);

        /// <summary>مفتاح حزمة الهوية الحالية.</summary>
        string CurrentIdentity { get; }

        ThemeMode CurrentMode { get; }

        /// <summary>يُطلَق بعد كل Apply/ApplyMode ناجح — مستهلكوه محدودون عمداً بحدود code-behind (راجع UIServices).</summary>
        event Action IdentityChanged;

        /// <summary>يقرأ الاختيار المحفوظ (حزمة الهوية + الوضع) من الإعدادات ويطبّقه — يُستدعى مرة واحدة عند إقلاع التطبيق.</summary>
        void Initialize();
    }
}
