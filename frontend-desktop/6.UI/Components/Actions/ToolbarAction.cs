using System.Collections.Generic;
using PrimeERP.Platform.Localization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Actions
{
    /// <summary>تعريف زر شريط أدوات</summary>
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

        public string   Shortcut      { get; set; }

        private static Geometry Icon_(string key) =>
            string.IsNullOrEmpty(key) ? null : System.Windows.Application.Current?.TryFindResource(key) as Geometry;

        /// <summary>مفاتيح النصوص لا النصوص</summary>
        public static readonly IReadOnlyDictionary<string, (string TextKey, string IconKey, string Variant, string Shortcut, string TooltipKey)> Catalogue =
            new Dictionary<string, (string, string, string, string, string)>
            {
                ["new"]          = ("Str.Action.New", "IconAdd", "primary", "Ctrl+N", "Str.Action.NewTip"),
                ["edit"]         = ("Str.Edit", "IconEdit", "secondary", "Ctrl+E", "Str.Action.EditTip"),
                ["delete"]       = ("Str.Delete", "IconDelete", "danger", "Delete", "Str.Action.DeleteTip"),
                ["save"]         = ("Str.Save", "IconSave", "primary", "Ctrl+S", "Str.Action.SaveTip"),
                ["cancel"]       = ("Str.Cancel", "IconCancel", "ghost", null, "Str.Action.CancelTip"),
                ["print"]        = ("Str.Print", "IconPrint", "secondary", null, "Str.Action.PrintTip"),
                ["export"]       = ("Str.Action.Export", "IconExport", "secondary", null, "Str.Action.ExportTip"),
                ["refresh"]      = ("Str.Action.Refresh", "IconRefresh", "ghost", "F5", "Str.Action.RefreshTip"),
                ["post"]         = ("Str.Action.Post", "IconCheck", "success", null, "Str.Action.PostTip"),
                ["unpost"]       = ("Str.Action.Unpost", "IconCancel", "warning", null, "Str.Action.UnpostTip"),
                ["expandAll"]    = ("Str.Action.ExpandAll", "IconChevronDown", "ghost", null, "Str.Action.ExpandAllTip"),
                ["collapseAll"]  = ("Str.Action.CollapseAll", "IconChevronUp", "ghost", null, "Str.Action.CollapseAllTip"),
                ["moveUp"]       = ("Str.Action.MoveUp", "IconChevronUp", "secondary", null, "Str.Action.MoveUpTip"),
                ["moveDown"]     = ("Str.Action.MoveDown", "IconChevronDown", "secondary", null, "Str.Action.MoveDownTip"),
            };

        private static ToolbarAction FromCatalogue(string key, ICommand command, string permissionKey, string text = null)
        {
            var spec = Catalogue[key];
            return Build(text == null ? key : $"{key}:{text}", text ?? LocalizationService.Get(spec.TextKey),
                spec.IconKey, spec.Variant, command, permissionKey, spec.Shortcut,
                text ?? LocalizationService.Get(spec.TooltipKey));
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

        public static ToolbarAction MoveUp(ICommand command, string permissionKey = null) =>
            FromCatalogue("moveUp", command, permissionKey);

        public static ToolbarAction MoveDown(ICommand command, string permissionKey = null) =>
            FromCatalogue("moveDown", command, permissionKey);

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
