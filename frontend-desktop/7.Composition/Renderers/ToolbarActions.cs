using System.Collections.Generic;
using System.Linq;
using PrimeERP.Composition.Definitions;
using PrimeERP.UI.Components.Actions;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>أزرار الصفحة بعد ترشيحها بما</summary>
    public static class ToolbarActions
    {
        public static List<ToolbarAction> Enabled(ModuleDefinition definition, List<ToolbarAction> actions)
        {
            if (definition.EnabledActions == null || actions == null) return actions;

            return actions.Where(a => a.Separator || Kept(definition, Root(a.Key))).ToList();
        }

        private static bool Kept(ModuleDefinition definition, string key) =>
            !ToolbarAction.Catalogue.ContainsKey(key) || definition.EnabledActions.Contains(key);

        private static string Root(string key) => key?.Split(':')[0] ?? "";
    }
}
