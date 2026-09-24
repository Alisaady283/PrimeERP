using System;
using System.Windows;
using PrimeERP.Composition.Definitions;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>نقطة التوزيع الوحيدة حسب ModuleDefinition.LayoutKind</summary>
    public static class PageRenderer
    {
        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services) =>
            definition.LayoutKind switch
            {
                LayoutKind.Grid => CrudPageRenderer.Render(definition, services),
                LayoutKind.Tree or LayoutKind.TreeSplit => TreeRenderer.Render(definition, services),
                LayoutKind.Report => ReportRenderer.Render(definition, services),
                LayoutKind.Settings => SettingsPageRenderer.Render(definition, services),
                LayoutKind.TreeCheckList => TreeCheckListRenderer.Render(definition, services),
                LayoutKind.DocumentPage => DocumentPageRenderer.Render(definition, services),
                LayoutKind.ChequeBoard => ChequeBoardRenderer.Render(definition, services),
                _ => throw new NotSupportedException($"LayoutKind غير مدعوم: {definition.LayoutKind}")
            };
    }
}
