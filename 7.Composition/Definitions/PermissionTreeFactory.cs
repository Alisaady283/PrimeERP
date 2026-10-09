using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>يبني شجرة الصلاحيات من PermissionKeys</summary>
    public static class PermissionTreeFactory
    {
        public static List<TreeNodeViewModel> Build(PrimeERP.Composition.Registry.IModuleRegistry registry = null)
        {
            var pages = registry?.All().Select(m => m.PermissionPrefix).ToHashSet();
            var groups = PermissionKeys.All()
                .Distinct()
                .GroupBy(k => k.Split('.')[0])
                .Where(g => pages == null || pages.Contains(g.Key))
                .OrderBy(g => g.Key);

            var roots = new List<TreeNodeViewModel>();

            foreach (var group in groups)
            {
                var moduleNode = new TreeNodeViewModel
                {
                    Id = group.Key,
                    Name = ModuleName(group.Key),
                    DisplayText = ModuleName(group.Key),
                    IsCheckable = false,
                    IsExpanded = false
                };

                foreach (var key in group.OrderBy(k => k.EndsWith(".View") ? 0 : 1).ThenBy(k => k))
                {
                    moduleNode.AddChild(new TreeNodeViewModel
                    {
                        Id = key,
                        Name = ActionName(key),
                        DisplayText = ActionName(key),
                        IsLeaf = true,
                        Data = key
                    });
                }

                roots.Add(moduleNode);
            }

            return roots;
        }

        public static IEnumerable<TreeNodeViewModel> KeyNodes(IEnumerable<TreeNodeViewModel> roots) =>
            roots.SelectMany(r => r.Children);

        private static string ModuleName(string module) => LocalizationService.GetOr($"Str.Permission.{module}", module);

        private static string ActionName(string key)
        {
            var parts = key.Split('.');
            if (parts.Length >= 3 && parts[1] == "Column")
                return LocalizationService.Get("Str.Permission.Column", ActionOf(parts[2]));

            return ActionOf(parts.Length > 1 ? parts[1] : key);
        }

        private static string ActionOf(string action) => LocalizationService.GetOr($"Str.Permission.Action.{action}", action);
    }
}
