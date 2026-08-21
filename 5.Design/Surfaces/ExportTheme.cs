using PrimeERP.Domain.Results;

namespace PrimeERP.Design.Surfaces
{
    /// <summary>
    /// كل قيمة بصرية تستخدمها التصديرات (Excel/CSV/PDF عبر ClosedXML وQuestPDF) — ثوابت C# صرفة لأن Excel
    /// لا يقرأ XAML. ثابتة دائماً (لا فاتح/داكن — ورقة/ملف مُصدَّر لا "يتبدّل" مع ثيم التطبيق).
    /// نفس المجموعات الدلالية الست الموجودة في Resources/Themes وResources/Print، بنفس القيم الفعلية
    /// (القيم الفاتحة من Colors.xaml) — لا اختراع ألوان جديدة هنا.
    /// </summary>
    public static class ExportTheme
    {
        public const string FontFamily     = "Arial";
        public const int TitleFontSize     = 15;
        public const int HeaderFontSize    = 10;
        public const int BodyFontSize      = 9;

        public const string TextPrimaryHex   = "#0F172A";
        public const string TextSecondaryHex = "#475569";
        public const string OutlineHex       = "#E2E8F0";
        public const string HeaderBackgroundHex = "#F1F5F9";

        public static string SolidHex(StatusVariant variant) => variant switch
        {
            StatusVariant.Neutral => "#64748B",
            StatusVariant.Info    => "#0891B2",
            StatusVariant.Brand   => "#2563EB",
            StatusVariant.Success => "#16A34A",
            StatusVariant.Warning => "#D97706",
            StatusVariant.Danger  => "#DC2626",
            _                     => "#64748B"
        };

        public static string SoftHex(StatusVariant variant) => variant switch
        {
            StatusVariant.Neutral => "#F1F5F9",
            StatusVariant.Info    => "#ECFEFF",
            StatusVariant.Brand   => "#EFF6FF",
            StatusVariant.Success => "#F0FDF4",
            StatusVariant.Warning => "#FFFBEB",
            StatusVariant.Danger  => "#FEF2F2",
            _                     => "#F1F5F9"
        };

        public static string SoftTextHex(StatusVariant variant) => variant switch
        {
            StatusVariant.Neutral => "#475569",
            StatusVariant.Info    => "#0E7490",
            StatusVariant.Brand   => "#1D4ED8",
            StatusVariant.Success => "#15803D",
            StatusVariant.Warning => "#B45309",
            StatusVariant.Danger  => "#B91C1C",
            _                     => "#475569"
        };
    }
}
