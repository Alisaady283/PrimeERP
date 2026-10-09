using PrimeERP.Domain.Calculations;
using PrimeERP.Application.PageServices.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.ViewModels;
using PrimeERP.Application.Reporting;
using PrimeERP.UI.Components.Tree;
using System.Dynamic;

namespace PrimeERP.Modules
{
    /// <summary>وصفُ ما بناه المستخدم ←</summary>
    public static class BuilderModuleLoader
    {
        public static void RegisterAll(IModuleRegistry registry, IServiceProvider services)
        {
            var repo = services.GetRequiredService<IBuilderCatalog>();

            repo.SeedSections(NavigationMap.Sections(), NavigationMap.Protected);

            repo.SeedModules(Coded(repo, registry));

            var modules = repo.Modules().Where(m => m.IsActive).ToList();
            var built = modules.Where(m => !m.IsCoded).Select(m => m.Key).ToHashSet();

            var allActions = repo.Actions(0).ToLookup(a => a.ModuleId);
            var allFilters = repo.Filters(0).ToLookup(f => f.ModuleId);
            var allColumns = repo.Columns(0).ToLookup(c => c.ModuleId);

            foreach (var module in modules)
            {
                var actions = allActions[module.Id].ToList();
                var filters = allFilters[module.Id].ToList();

                var columns = allColumns[module.Id].ToList();

                if (module.IsCoded)
                {
                    Overlay(registry, module, columns, actions, filters, built);
                    continue;
                }

                repo.EnsureBuiltTable(module, columns);

                PermissionKeys.RegisterBuilt(module.Key);

                registry.Register(Build(module, columns, actions, filters, built));
            }
        }

        private static void Overlay(IModuleRegistry registry, BuilderModule module, List<BuilderColumn> columns,
            List<BuilderAction> actions, List<BuilderFilter> filters, HashSet<string> built)
        {
            var coded = registry.Get(module.Key);
            if (coded == null) return;

            var merged = Columns(columns, coded.Report?.Columns ?? coded.Columns);

            registry.Register(coded with
            {
                Columns = merged,
                EnabledActions = actions.Select(a => a.ActionKey)
                    .Where(key => coded.EnabledActions == null || coded.EnabledActions.Contains(key)).ToArray(),
                Filters = coded.Report != null ? coded.Filters : Filters(filters, built, coded),
                Report = coded.Report == null ? null
                    : coded.Report with { Columns = merged, Parameters = Parameters(filters, coded.Report) }
            });
        }

        private static List<CodedPage> Coded(IBuilderCatalog repo, IModuleRegistry registry)
        {
            var pages = new List<CodedPage>();
            var sections = repo.Sections().Select(section => section.Key).ToHashSet();

            foreach (var (key, _, modules) in NavigationMap.Coded)
            {
                if (!sections.Contains(key)) continue;   // قسمٌ محميّ أو حذفه المستخدم

                foreach (var moduleKey in modules)
                {
                    var module = registry.Get(moduleKey);
                    if (module == null) continue;

                    pages.Add(new CodedPage(key, module.Key, LocalizationService.Get(module.TitleKey),
                        KindOf(module), CodedColumns(module), CodedActions(module), CodedFilters(module)));
                }
            }

            return pages;
        }

        private static BuilderKind KindOf(ModuleDefinition m) =>
            m.LayoutKind == LayoutKind.Report ? BuilderKind.Report
            : m.DocumentDialog != null        ? BuilderKind.Movement
                                              : BuilderKind.Record;

        private static List<BuilderColumn> CodedColumns(ModuleDefinition m) =>
            (m.Report?.Columns ?? m.Columns ?? new List<GridColumn>()).Select((c, i) => new BuilderColumn
            {
                Name = c.Binding, Header = c.Header, Width = c.Width, SortOrder = (i + 1) * 10,
                DataType = c.Format == "yyyy-MM-dd" ? BuilderDataType.Date
                         : c.Format == "N2"         ? BuilderDataType.Money
                                                    : BuilderDataType.Text,
                Footer = c.Footer == FooterAggregate.None ? null : c.Footer.ToString(),
                ShowInGrid = c.IsVisible, ShowInForm = false
            }).ToList();

        private static List<BuilderAction> CodedActions(ModuleDefinition m) =>
            (m.EnabledActions ?? ToolbarAction.Catalogue.Keys.ToArray())
                .Select((key, i) => new BuilderAction { ActionKey = key, SortOrder = (i + 1) * 10 })
                .ToList();

        private static List<BuilderFilter> CodedFilters(ModuleDefinition m) =>
            m.Report != null
                ? m.Report.Parameters.Select((p, i) => new BuilderFilter
                  {
                      Key = p.Key, Label = LocalizationService.Get(p.LabelKey), Kind = p.Kind.ToString(),
                      RefModule = p.PickerType, SortOrder = (i + 1) * 10
                  }).ToList()
                : (m.Filters ?? new List<FilterDefinition>()).Select((f, i) => new BuilderFilter
                  {
                      Key = f.Key, Label = LocalizationService.Get(f.LabelKey), Kind = f.Kind.ToString(),
                      RefModule = f.PickerType, SortOrder = (i + 1) * 10
                  }).ToList();

        private static List<ParameterDefinition> Parameters(List<BuilderFilter> filters, ReportDefinition coded)
        {
            var written = coded.Parameters.ToDictionary(p => p.Key, p => p);

            var ordered = filters.OrderBy(f => f.SortOrder)
                .Where(f => written.ContainsKey(f.Key))
                .Select(f => written[f.Key] with { LabelKey = LocalizationService.Pick(f.Label, f.LabelEn, written[f.Key].LabelKey) })
                .ToList();

            return ordered.Concat(coded.Parameters.Where(p => ordered.All(o => o.Key != p.Key))).ToList();
        }

        private static ModuleDefinition Build(BuilderModule module, List<BuilderColumn> columns,
            List<BuilderAction> actions, List<BuilderFilter> filters, HashSet<string> built)
        {
            var title = LocalizationService.Pick(module.Title, module.TitleEn);

            DynamicEntityService Service(IServiceProvider s) => new(module, columns,
                s.GetRequiredService<IPermissionService>(), s.GetRequiredService<ISettingsProvider>(),
                s.GetRequiredService<ILocalizationService>(), s.GetRequiredService<IAuditLogger>());

            if (module.Kind == BuilderKind.Report)
                return new ModuleDefinition
                {
                    Key = module.Key, TitleKey = title, PermissionPrefix = module.Key,
                    LayoutKind = LayoutKind.Report,
                    Report = new ReportDefinition
                    {
                        Key = module.Key, TitleKey = title, PermissionKey = $"{module.Key}.View",
                        ServiceType = typeof(IBuilderReportService),
                        Method = nameof(IBuilderReportService.Rows),
                        Arguments = new[] { "ModuleKey", "From", "To" },
                        FixedArguments = new() { ["ModuleKey"] = module.SourceKey },
                        Parameters = StandardFields.DateRange(),
                        Columns = Columns(columns)
                    }
                };

            return new ModuleDefinition
            {
                Key = module.Key,
                TitleKey = title,
                PermissionPrefix = module.Key,
                ViewModelFactory = s => new DynamicViewModel(Service(s), module.Key,
                    s.GetRequiredService<IPermissionService>(),
                    s.GetRequiredService<UI.Services.IToastService>(),
                    s.GetRequiredService<UI.Services.IDialogService>()),
                Columns = Columns(columns),
                Filters = Filters(filters, built, null),
                EnabledActions = actions.Count == 0 ? null : actions.Select(a => a.ActionKey).ToArray(),
                Dialog = module.Kind != BuilderKind.Record ? null : new DialogDefinition
                {
                    TitleKey = title, TitleEditKey = title,
                    ServiceType = typeof(DynamicEntityService),
                    ServiceFactory = s => Service(s),
                    CreateDtoType = typeof(ExpandoObject),
                    UpdateDtoType = typeof(ExpandoObject),
                    Fields = Fields(columns)
                },
                DocumentDialog = module.Kind != BuilderKind.Movement ? null : new DocumentDialogDefinition
                {
                    TitleKey = title, TitleEditKey = title,
                    ServiceType = typeof(DynamicEntityService),
                    ServiceFactory = s => Service(s),
                    DtoType = typeof(ExpandoObject),
                    LineDtoType = typeof(ExpandoObject),
                    LinesPropertyName = "Lines",
                    HeaderFields = Fields(columns),
                    LineFields = LineFields(columns),
                    LineTotals = Totals(columns)
                }
            };
        }

        private static List<GridColumn> Columns(List<BuilderColumn> columns) => Grid(columns, null);

        private static List<GridColumn> Columns(List<BuilderColumn> columns, List<GridColumn> coded) =>
            Grid(columns, coded);

        private static List<GridColumn> Grid(List<BuilderColumn> columns, List<GridColumn> coded)
        {
            var written = (coded ?? new List<GridColumn>())
                .Where(c => c.Binding != null)
                .ToDictionary(c => c.Binding, c => c);

            var rows = Shown(columns).ToList();

            var proportional = LayoutCalc.Proportional(rows);
            var shares = LayoutCalc.Shares(rows);

            return rows.Select(row =>
            {
                var width = proportional ? shares[row.Id] : row.Width;

                return written.TryGetValue(row.Name ?? "", out var column)
                    ? column with
                      {
                          Header = LocalizationService.CurrentLanguage == AppLanguage.Ar ? row.Header : LocalizationService.Pick(row.Header, row.HeaderEn, column.Header),
                          Width = width, Footer = Footer(row.Footer),
                          IsStarWidth = proportional || column.IsStarWidth
                      }
                    : Column(row) with { Width = width, IsStarWidth = proportional };
            }).ToList();
        }

        private static IEnumerable<BuilderColumn> Shown(List<BuilderColumn> columns) =>
            columns.Where(c => c.ShowInGrid && !c.IsLine).OrderBy(c => c.SortOrder);

        private static GridColumn Column(BuilderColumn c) => new()
        {
            Header = LocalizationService.Pick(c.Header, c.HeaderEn),
            Binding = c.Name,
            Width = c.Width,
            Align = c.DataType is BuilderDataType.Number or BuilderDataType.Money ? ColumnAlign.Center : ColumnAlign.Auto,
            Format = c.DataType is BuilderDataType.Number or BuilderDataType.Money ? "N2"
                   : c.DataType == BuilderDataType.Date ? "yyyy-MM-dd" : null,
            Footer = Footer(c.Footer)
        };

        private static FooterAggregate Footer(string footer) =>
            Enum.TryParse<FooterAggregate>(footer, out var parsed) ? parsed : FooterAggregate.None;

        private static List<FieldDefinition> Fields(List<BuilderColumn> columns) =>
            columns.Where(c => c.ShowInForm && !c.IsLine && c.Aggregate == BuilderAggregate.None)
                .Select(c => new FieldDefinition
                {
                    Key = c.Name,
                    LabelKey = LocalizationService.Pick(c.Header, c.HeaderEn),
                    Kind = Kind(c.DataType),
                    IsRequired = c.IsRequired,
                    MaxLength = c.MaxLength ?? 0,
                    PickerType = c.DataType == BuilderDataType.Reference ? $"Table:{c.RefModule}:{c.RefDisplay}" : null
                }).ToList();

        private static List<LineFieldDefinition> LineFields(List<BuilderColumn> columns) =>
            columns.Where(c => c.IsLine && c.Aggregate == BuilderAggregate.None)
                .Select(c => new LineFieldDefinition
                {
                    Key = c.Name,
                    Header = LocalizationService.Pick(c.Header, c.HeaderEn),
                    Kind = Kind(c.DataType),
                    Width = c.Width,
                    IsRequired = c.IsRequired,
                    PickerType = c.DataType == BuilderDataType.Reference ? $"Table:{c.RefModule}:{c.RefDisplay}" : null
                }).ToList();

        private static LineTotalsDefinition Totals(List<BuilderColumn> columns)
        {
            var keys = columns.Where(c => c.IsLine && !string.IsNullOrWhiteSpace(c.Footer) && c.Footer != "None")
                .Select(c => c.Name).ToList();

            return keys.Count == 0 ? null : new LineTotalsDefinition { Keys = keys };
        }

        private static FieldKind Kind(BuilderDataType type) => type switch
        {
            BuilderDataType.Number or BuilderDataType.Money => FieldKind.Number,
            BuilderDataType.Date      => FieldKind.Date,
            BuilderDataType.Bool      => FieldKind.Check,
            BuilderDataType.LongText  => FieldKind.TextArea,
            BuilderDataType.Reference => FieldKind.Picker,
            _                         => FieldKind.Text
        };

        private static List<FilterDefinition> Filters(List<BuilderFilter> filters, HashSet<string> built,
            ModuleDefinition page) =>
            filters.Count == 0 ? null : filters.Select(f => Filter(f, built, page)).ToList();

        private static FilterDefinition Filter(BuilderFilter filter, HashSet<string> built, ModuleDefinition page)
        {
            var declared = Declared(page).FirstOrDefault(field => field.Key == filter.Key);

            return new FilterDefinition
            {
                Key = filter.Key,
                LabelKey = LocalizationService.Pick(filter.Label, filter.LabelEn, declared?.LabelKey),
                Kind = filter.Kind == "Toggle" ? FilterKind.Toggle : FilterKind.Combo,
                PickerType = declared?.PickerType ?? Reference(filter.RefModule, built),
                PickerCategoryModuleKey = declared?.PickerCategoryModuleKey
            };
        }

        private static IEnumerable<FieldDefinition> Declared(ModuleDefinition page) =>
            page?.Dialog?.Fields ?? page?.DocumentDialog?.HeaderFields ?? Enumerable.Empty<FieldDefinition>();

        private static string Reference(string reference, HashSet<string> built) =>
            string.IsNullOrWhiteSpace(reference) ? null
            : built.Contains(reference)          ? $"Table:{reference}:Name"
                                                 : reference;

    }
}
