using System;
using System.Windows;
using PrimeERP.Composition.Definitions;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>نقطة التوزيع الوحيدة حسب ModuleDefinition.LayoutKind</summary>
    public static class PageRenderer
    {
        /// <summary>search: نصّ بحثٍ تبدأ به صفحة القائمة (التفصيل من لوحة التحليل مُصفّى)</summary>
        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services, string search = null) =>
            definition.LayoutKind switch
            {
                LayoutKind.Grid => CrudPageRenderer.Render(definition, services, search),
                LayoutKind.Tree or LayoutKind.TreeSplit => TreeRenderer.Render(definition, services),
                LayoutKind.Report => ReportRenderer.Render(definition, services),
                LayoutKind.Dashboard => DashboardRenderer.Render(definition, services),
                LayoutKind.Settings => SettingsPageRenderer.Render(definition, services),
                LayoutKind.TreeCheckList => TreeCheckListRenderer.Render(definition, services),
                LayoutKind.DocumentPage => DocumentPageRenderer.Render(definition, services),
                LayoutKind.ChequeBoard => ChequeBoardRenderer.Render(definition, services),
                LayoutKind.Tabs => TabsPageRenderer.Render(definition, services),
                LayoutKind.Pos => PosRenderer.Render(definition, services),
                _ => throw new NotSupportedException($"LayoutKind غير مدعوم: {definition.LayoutKind}")
            };
    }
}
