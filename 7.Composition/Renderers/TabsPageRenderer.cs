using System;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.Composition.Renderers
{
    public static class TabsPageRenderer
    {
        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services)
        {
            var registry = services.GetRequiredService<IModuleRegistry>();
            var permissions = services.GetRequiredService<IPermissionService>();

            var tabs = definition.TabModules
                .Select(registry.Get)
                .Where(page => page != null && permissions.Can($"{page.PermissionPrefix}.View"))
                .Select(page => new AppTabItem { Header = LocalizationService.Get(page.TitleKey), Content = PageRenderer.Render(page, services) })
                .ToList();

            return new AppTabControl { Tabs = tabs };
        }
    }
}
