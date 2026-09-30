using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Legacy.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Registry;
using PrimeERP.Domain.Enums;
using PrimeERP.UI.Components.Display;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Actions;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>قوائم تعدادات النظام وكتالوج أزراره</summary>
    internal static class BuilderPickers
    {
        internal static List<DialogRenderer.PickerRow> Rows(string pickerType, IServiceProvider services,
            object filterValue = null) => pickerType switch
        {
            "AccountType"       => Enum<AccountType>("Str.AccountType"),
            "AssetAcquisition"  => Enum<AssetAcquisition>("Str.Asset"),
            "BuilderKind"       => Enum<BuilderKind>("Str.Builder.Kind"),
            "BuilderDataType"   => Enum<BuilderDataType>("Str.Builder.Type"),
            "BuilderAggregate"  => Enum<BuilderAggregate>("Str.Builder.Agg"),
            "FooterAggregate"   => Enum<FooterAggregate>("Str.Builder.Agg", byName: true),
            "BuilderFilterKind" => Named("Str.Builder.Filter", "Combo", "Toggle", "DateRange"),
            "ToolbarAction"     => ToolbarAction.Catalogue
                                     .Select((entry, i) => new DialogRenderer.PickerRow
                                     { Id = i + 1, Code = entry.Key, Display = LocalizationService.Get(entry.Value.TextKey) }).ToList(),
            "BuilderSection"    => services.GetRequiredService<IBuilderCatalog>().Sections()
                                     .Select(x => new DialogRenderer.PickerRow { Id = x.Id, Code = x.Key, Display = x.Title }).ToList(),
            "BuilderModule"     => services.GetRequiredService<IBuilderCatalog>().Modules()
                                     .Where(x => Section(filterValue) is not int section || x.SectionId == section)
                                     .Select(x => new DialogRenderer.PickerRow { Id = x.Id, Code = x.Key, Display = x.Title }).ToList(),
            "AnyModule"         => services.GetRequiredService<IModuleRegistry>().All()
                                     .Select((m, i) => new DialogRenderer.PickerRow
                                     { Id = i + 1, Code = m.Key, Display = LocalizationService.Get(m.TitleKey) }).ToList(),
            _                   => null
        };

        private static object Section(object filterValue) =>
            filterValue == null || filterValue is string { Length: 0 } ? null : Convert.ToInt32(filterValue);

        private static List<DialogRenderer.PickerRow> Enum<T>(string prefix, bool byName = false) where T : struct, System.Enum =>
            System.Enum.GetValues<T>()
                .Select(v => new DialogRenderer.PickerRow
                {
                    Id = Convert.ToInt32(v),
                    Code = byName ? v.ToString() : Convert.ToInt32(v).ToString(),
                    Display = LocalizationService.Get($"{prefix}.{v}")
                }).ToList();

        private static List<DialogRenderer.PickerRow> Named(string prefix, params string[] keys) =>
            keys.Select((k, i) => new DialogRenderer.PickerRow
            { Id = i + 1, Code = k, Display = LocalizationService.Get($"{prefix}.{k}") }).ToList();
    }
}
