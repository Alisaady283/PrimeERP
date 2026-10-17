using PrimeERP.Application.PageServices.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace PrimeERP.Composition.Registry
{
    /// <summary>أقسام الشريط الجانبي كما يراها</summary>
    public static class NavigationSource
    {
        public static (string Key, string Text, string IconKey, string[] Keys)[] Groups(IServiceProvider services)
        {
            var catalog = services.GetRequiredService<IBuilderCatalog>();
            var modules = catalog.Modules().Where(m => m.IsActive).ToList();

            var seeded = modules.Count > 0;

            var built = catalog.Sections()
                .OrderBy(section => section.SortOrder)
                .Select(section =>
                {
                    var rows = modules.Where(m => m.SectionId == section.Id).Select(m => m.Key).ToArray();

                    var title = PrimeERP.Platform.Localization.LocalizationService.Pick(section.Title, section.TitleEn,
                        PrimeERP.Platform.Localization.LocalizationService.GetOr($"Str.Nav.{section.Key}", null));
                    return (section.Key, title, section.IconKey ?? "IconSettings",
                        seeded ? rows
                            : (section.Modules ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                })
                .Where(g => g.Item4.Length > 0)
                .ToList();

            return NavigationMap.Groups(built);
        }
    }
}
