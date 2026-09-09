using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Actions
{
    /// <summary>تعريف زر شريط أدوات — يُبنى من كود C# ويُستهلك عبر ActionToolbar.ButtonsSource.</summary>
    public class ToolbarAction
    {
        public string   Key           { get; set; }
        public string   Text          { get; set; }
        public Geometry Icon          { get; set; }
        public string   Variant       { get; set; } = "secondary";
        public string   PermissionKey { get; set; }
        public ICommand Command       { get; set; }
        public bool     IsEnabled     { get; set; } = true;
        public bool     IsVisible     { get; set; } = true;
        public bool     Separator     { get; set; }
        public string   Tooltip       { get; set; }

        /// <summary>مثل "Ctrl+N" أو "F5" أو "Delete" — يُسجَّل تلقائياً كـ KeyBinding في نافذة ActionToolbar المضيفة.</summary>
        public string   Shortcut      { get; set; }

        // إجراء مُعلَن بلا أيقونة وارد (إجراءات الوحدات النصّية) — TryFindResource ترمي على مفتاح فارغ.
        private static Geometry Icon_(string key) =>
            string.IsNullOrEmpty(key) ? null : System.Windows.Application.Current?.TryFindResource(key) as Geometry;

        /// <summary>
        /// كتالوج الأزرار القياسية: مفتاحٌ ← اسمه وأيقونته وشكله واختصاره وتلميحه. مصدرٌ واحد تقرأ منه
        /// المصانع أدناه، ويقرأ منه معالج البناء ليعرض ما يملكه النظام — فأي زرّ يُضاف هنا يظهر في
        /// المعالج بلا تعديل ثانٍ، ولا تتكرّر قيمه في موضعين.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, (string Text, string IconKey, string Variant, string Shortcut, string Tooltip)> Catalogue =
            new Dictionary<string, (string, string, string, string, string)>
            {
                ["new"]         = ("جديد",        "IconAdd",         "primary",   "Ctrl+N", "إضافة عنصر جديد"),
                ["edit"]        = ("تعديل",       "IconEdit",        "secondary", "Ctrl+E", "تعديل العنصر المحدد"),
                ["delete"]      = ("حذف",         "IconDelete",      "danger",    "Delete", "حذف العنصر المحدد"),
                ["save"]        = ("حفظ",         "IconSave",        "primary",   "Ctrl+S", "حفظ التغييرات"),
                ["cancel"]      = ("إلغاء",       "IconCancel",      "ghost",     null,     "إلغاء العملية"),
                ["print"]       = ("طباعة",       "IconPrint",       "secondary", null,     "طباعة"),
                ["export"]      = ("تصدير",       "IconExport",      "secondary", null,     "تصدير"),
                ["refresh"]     = ("تحديث",       "IconRefresh",     "ghost",     "F5",     "تحديث البيانات"),
                ["post"]        = ("ترحيل",       "IconCheck",       "success",   null,     "ترحيل المستند"),
                ["unpost"]      = ("إلغاء ترحيل", "IconCancel",      "warning",   null,     "إلغاء ترحيل المستند"),
                ["expandAll"]   = ("توسيع الكل",  "IconChevronDown", "ghost",     null,     "توسيع كل العقد"),
                ["collapseAll"] = ("طي الكل",     "IconChevronUp",   "ghost",     null,     "طي كل العقد"),
            };

        /// <summary>
        /// يبني زراً من الكتالوج. text غير الفارغ يُخصّص الاسم ويُفرِّد المفتاح — صفحة المستندات فيها
        /// زرّا طباعة (تقرير ومستند)، ولولا التفريد لتصادما في قائمة "المزيد" وفي الاختصارات.
        /// </summary>
        private static ToolbarAction FromCatalogue(string key, ICommand command, string permissionKey, string text = null)
        {
            var spec = Catalogue[key];
            return Build(text == null ? key : $"{key}:{text}", text ?? spec.Text, spec.IconKey, spec.Variant,
                command, permissionKey, spec.Shortcut, text ?? spec.Tooltip);
        }

        public static ToolbarAction SeparatorItem() => new() { Separator = true };


        public static ToolbarAction New(ICommand command, string permissionKey = null) =>
            FromCatalogue("new", command, permissionKey);

        public static ToolbarAction Edit(ICommand command, string permissionKey = null) =>
            FromCatalogue("edit", command, permissionKey);

        public static ToolbarAction Delete(ICommand command, string permissionKey = null) =>
            FromCatalogue("delete", command, permissionKey);

        public static ToolbarAction Save(ICommand command, string permissionKey = null) =>
            FromCatalogue("save", command, permissionKey);

        public static ToolbarAction Cancel(ICommand command, string permissionKey = null) =>
            FromCatalogue("cancel", command, permissionKey);

        public static ToolbarAction Print(ICommand command, string permissionKey = null, string text = null) =>
            FromCatalogue("print", command, permissionKey, text);

        public static ToolbarAction Export(ICommand command, string permissionKey = null, string text = null) =>
            FromCatalogue("export", command, permissionKey, text);

        public static ToolbarAction Refresh(ICommand command, string permissionKey = null) =>
            FromCatalogue("refresh", command, permissionKey);

        public static ToolbarAction Post(ICommand command, string permissionKey = null) =>
            FromCatalogue("post", command, permissionKey);

        public static ToolbarAction Unpost(ICommand command, string permissionKey = null) =>
            FromCatalogue("unpost", command, permissionKey);

        public static ToolbarAction ExpandAll(ICommand command) =>
            FromCatalogue("expandAll", command, null);

        public static ToolbarAction CollapseAll(ICommand command) =>
            FromCatalogue("collapseAll", command, null);

        public static ToolbarAction Build(string key, string text, string iconKey, string variant,
                                           ICommand command, string permissionKey, string shortcut, string tooltip) => new()
        {
            Key = key,
            Text = text,
            Icon = Icon_(iconKey),
            Variant = variant,
            Command = command,
            PermissionKey = permissionKey,
            Shortcut = shortcut,
            Tooltip = tooltip
        };
    }
}
