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

        private static Geometry Icon_(string key) => System.Windows.Application.Current?.TryFindResource(key) as Geometry;

        public static ToolbarAction SeparatorItem() => new() { Separator = true };

        public static ToolbarAction New(ICommand command, string permissionKey = null) =>
            Build("new", "جديد", "IconAdd", "primary", command, permissionKey, "Ctrl+N", "إضافة عنصر جديد");

        public static ToolbarAction Edit(ICommand command, string permissionKey = null) =>
            Build("edit", "تعديل", "IconEdit", "secondary", command, permissionKey, "Ctrl+E", "تعديل العنصر المحدد");

        public static ToolbarAction Delete(ICommand command, string permissionKey = null) =>
            Build("delete", "حذف", "IconDelete", "danger", command, permissionKey, "Delete", "حذف العنصر المحدد");

        public static ToolbarAction Save(ICommand command, string permissionKey = null) =>
            Build("save", "حفظ", "IconSave", "primary", command, permissionKey, "Ctrl+S", "حفظ التغييرات");

        public static ToolbarAction Cancel(ICommand command, string permissionKey = null) =>
            Build("cancel", "إلغاء", "IconCancel", "ghost", command, permissionKey, null, "إلغاء العملية");

        public static ToolbarAction Print(ICommand command, string permissionKey = null) =>
            Build("print", "طباعة", "IconPrint", "secondary", command, permissionKey, "Ctrl+P", "طباعة");

        public static ToolbarAction Export(ICommand command, string permissionKey = null) =>
            Build("export", "تصدير", "IconExport", "secondary", command, permissionKey, null, "تصدير البيانات");

        public static ToolbarAction Refresh(ICommand command, string permissionKey = null) =>
            Build("refresh", "تحديث", "IconRefresh", "ghost", command, permissionKey, "F5", "تحديث البيانات");

        public static ToolbarAction Post(ICommand command, string permissionKey = null) =>
            Build("post", "ترحيل", "IconCheck", "success", command, permissionKey, null, "ترحيل المستند");

        public static ToolbarAction Unpost(ICommand command, string permissionKey = null) =>
            Build("unpost", "إلغاء ترحيل", "IconCancel", "warning", command, permissionKey, null, "إلغاء ترحيل المستند");

        private static ToolbarAction Build(string key, string text, string iconKey, string variant,
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
