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

            return actions.Where(a => a.Separator || Kept(definition, Root(a.Key))).ToList();
        }

        /// <summary>
        /// الترشيح يخصّ أزرار الكتالوج وحدها. إجراء الوحدة المُعلَن (احتساب الإهلاك، تحريك شيك) مفتاحه
        /// اسمُه لا مفتاحُ كتالوج، ولا يُختار في وحدة البناء — فترشيحه بقائمة الكتالوج كان يُخفيه دائماً.
        /// </summary>
        private static bool Kept(ModuleDefinition definition, string key) =>
            !ToolbarAction.Catalogue.ContainsKey(key) || definition.EnabledActions.Contains(key);

        private static string Root(string key) => key?.Split(':')[0] ?? "";
    }
}
