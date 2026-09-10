using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Builder;

namespace PrimeERP.Composition.Registry
{
    /// <summary>
    /// أقسام الشريط الجانبي كما يراها المستخدم: المحميّ من الكود والباقي من الوصف. مصدرٌ واحد تقرؤه
    /// النافذة الرئيسية وشاشة إنشاء البرنامج — ولو تعدّد لاختلف ما يُصدَّر عمّا يُعرَض.
    /// </summary>
    public static class NavigationSource
    {
        public static (string Key, string Text, string IconKey, string[] Keys)[] Groups(IServiceProvider services)
        {
            var catalog = services.GetRequiredService<IBuilderCatalog>();
            var modules = catalog.Modules().Where(m => m.IsActive).ToList();

            // البذر جرى إن وُجد صفٌّ واحد. عندها الصفوف هي المصدر وحدها — وقسمٌ حُذفت كل صفحاته يبقى
            // فارغاً فيختفي، ولا تعود مفاتيح الكود تُحييه. ونصّ العمود احتياطُ قاعدةٍ لم تُبذَر بعد.
            var seeded = modules.Count > 0;

            var built = catalog.Sections()
                .OrderBy(section => section.SortOrder)
                .Select(section =>
                {
                    var rows = modules.Where(m => m.SectionId == section.Id).Select(m => m.Key).ToArray();

                    return (section.Key, section.Title, section.IconKey ?? "IconSettings",
                        seeded ? rows
                            : (section.Modules ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                })
                .Where(g => g.Item4.Length > 0)
                .ToList();

            return NavigationMap.Groups(built);
        }
    }
}
