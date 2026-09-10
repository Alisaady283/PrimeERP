using System.Collections.Generic;
using System.Linq;
using PrimeERP.Composition.Definitions;
using PrimeERP.UI.Components.Actions;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>
    /// أزرار الصفحة بعد ترشيحها بما أعلنته الوحدة — موضعٌ واحد يستورده كل مُصيِّر يبني شريط أزرار،
    /// وإلا سرى حذفُ زرّ في صفحة قائمة وسقط في شجرة.
    /// </summary>
    public static class ToolbarActions
    {
        /// <summary>
        /// ترشيح أزرار الكتالوج بما أعلنته الوحدة. مفتاح الطباعة والتصدير مُفرَّد بنصّه ("print:طباعة
        /// المستند") فيُقارَن جذره. null = الكل، فلا تتأثر أي وحدة مكتوبة لم تُعلن ترشيحاً.
        /// </summary>
        public static List<ToolbarAction> Enabled(ModuleDefinition definition, List<ToolbarAction> actions)
        {
            if (definition.EnabledActions == null || actions == null) return actions;

            return actions
                .Where(a => a.Separator || definition.EnabledActions.Contains(a.Key?.Split(':')[0]))
                .ToList();
        }
    }
}
