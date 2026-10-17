using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>شجرة الصلاحيات للدسكتوب من PermissionCatalog — المصدر نفسه الذي يرسله الخادم للويب والأندرويد</summary>
    public static class PermissionTreeFactory
    {
        public static List<TreeNodeViewModel> Build(PrimeERP.Composition.Registry.IModuleRegistry registry = null)
        {
            var pages = registry?.All().SelectMany(m => new[] { m.Key, m.PermissionPrefix }).ToHashSet();
            return PermissionCatalog.Groups(pages == null ? null : pages.Contains).Select(group =>
            {
                var moduleNode = new TreeNodeViewModel { Id = group.Key, Name = group.Name, DisplayText = group.Name, IsCheckable = false, IsExpanded = false };
                foreach (var action in group.Actions)
                    moduleNode.AddChild(new TreeNodeViewModel { Id = action.Key, Name = action.Name, DisplayText = action.Name, IsLeaf = true, Data = action.Key });
                return moduleNode;
            }).ToList();
        }

        public static IEnumerable<TreeNodeViewModel> KeyNodes(IEnumerable<TreeNodeViewModel> roots) =>
            roots.SelectMany(r => r.Children);
    }
}
