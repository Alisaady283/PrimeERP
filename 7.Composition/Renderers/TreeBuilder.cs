using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>يحوّل قائمة مسطَّحة (أي TDto) لشجرة TreeNodeViewModel عبر Reflection على أسماء الحقول في
    /// TreeLayoutOptions — منطق واحد يخدم أي كيان هرمي مستقبلي (Departments/Categories...) بلا تكرار نمط
    /// AccountPicker.BuildTree اليدوي (نفس الفكرة، مُعمَّمة). لا حالة — دالة نقية واحدة.</summary>
    internal static class TreeBuilder
    {
        private static readonly Regex PlaceholderPattern = new(@"\{(\w+)(:[^}]+)?\}", RegexOptions.Compiled);

        public static List<TreeNodeViewModel> Build(IEnumerable items, TreeLayoutOptions options)
        {
            var itemList = new List<object>();
            foreach (var item in items) itemList.Add(item);

            if (itemList.Count == 0) return new List<TreeNodeViewModel>();

            var type = itemList[0].GetType();
            var idProp       = type.GetProperty(options.IdField);
            var parentIdProp = type.GetProperty(options.ParentIdField);
            var codeProp     = options.CodeField != null ? type.GetProperty(options.CodeField) : null;
            var nameProp     = options.NameField != null ? type.GetProperty(options.NameField) : null;
            var leafProp     = options.LeafFlagField != null ? type.GetProperty(options.LeafFlagField) : null;

            var nodeById = new Dictionary<object, TreeNodeViewModel>();

            foreach (var item in itemList)
            {
                var id = idProp.GetValue(item);
                if (id == null) continue;

                var display = FormatTemplate(options.DisplayTemplate, item);
                if (!string.IsNullOrEmpty(options.ExtraInfoTemplate))
                    display += " " + FormatTemplate(options.ExtraInfoTemplate, item);

                nodeById[id] = new TreeNodeViewModel
                {
                    Id          = id.ToString(),
                    Code        = codeProp?.GetValue(item)?.ToString(),
                    Name        = nameProp?.GetValue(item)?.ToString(),
                    DisplayText = display,
                    IsLeaf      = leafProp != null && leafProp.GetValue(item) is bool isLeaf && isLeaf,
                    IsSelectable = true,
                    Data        = item
                };
            }

            var roots = new List<TreeNodeViewModel>();

            foreach (var item in itemList)
            {
                var id = idProp.GetValue(item);
                if (id == null || !nodeById.TryGetValue(id, out var node)) continue;

                var parentId = parentIdProp.GetValue(item);
                if (parentId != null && nodeById.TryGetValue(parentId, out var parent))
                    parent.AddChild(node);
                else
                    roots.Add(node);
            }

            if (options.SelectableRule == SelectableRule.LeafOnly)
                foreach (var node in nodeById.Values)
                    node.IsSelectable = node.IsLeaf;

            var expandLevel = options.ExpandToLevel > 0 ? options.ExpandToLevel : (options.ExpandRootsByDefault ? 1 : 0);
            if (expandLevel > 0)
                ExpandToLevel(roots, expandLevel, 1);

            return roots;
        }

        private static void ExpandToLevel(IEnumerable<TreeNodeViewModel> nodes, int maxLevel, int currentLevel)
        {
            if (currentLevel > maxLevel) return;

            foreach (var node in nodes)
            {
                node.IsExpanded = true;
                ExpandToLevel(node.Children, maxLevel, currentLevel + 1);
            }
        }

        private static string FormatTemplate(string template, object item)
        {
            if (string.IsNullOrEmpty(template)) return "";

            return PlaceholderPattern.Replace(template, match =>
            {
                var prop = item.GetType().GetProperty(match.Groups[1].Value);
                if (prop == null) return "";

                var value = prop.GetValue(item);
                if (value == null) return "";

                var format = match.Groups[2].Success ? match.Groups[2].Value.TrimStart(':') : null;
                return value is System.IFormattable formattable && format != null
                    ? formattable.ToString(format, System.Globalization.CultureInfo.InvariantCulture)
                    : value.ToString();
            });
        }
    }
}
