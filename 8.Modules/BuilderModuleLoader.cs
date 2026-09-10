using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Builder;
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
    /// <summary>
    /// وصفُ ما بناه المستخدم ← ModuleDefinition ← تسجيل. لا مُصيِّر جديد ولا نمط جديد: الوحدة المبنيّة
    /// تصل إلى PageRenderer بنفس شكل الوحدات المكتوبة، فتأخذ الشبكة والحوار والأزرار والفلاتر والصلاحيات
    /// والتدقيق كما تأخذها أي شاشة. المصنعان (ViewModelFactory/ServiceFactory) هما ما يجعل نسخةً واحدة
    /// من الخدمة تعرف جدولها.
    /// </summary>
    public static class BuilderModuleLoader
    {
        public static void RegisterAll(IModuleRegistry registry, IServiceProvider services)
        {
            var repo = services.GetRequiredService<IBuilderCatalog>();

            // أقسام الكود غير المحميّة تصير صفوفاً تُعدَّل — نسخٌ من الخريطة لا بناء.
            repo.SeedSections(NavigationMap.Coded, NavigationMap.Protected);

            // وصفحاتها كذلك: كل صفحة مكتوبة تصير صفّاً بأعمدتها وأزرارها وفلاترها — تُقرأ من تسجيلها
            // نفسه لا تُكتب هنا. المبذور مؤشَّر IsCoded فلا يُسجَّل ثانيةً فوق أصله ولا يُنشأ له جدول.
            repo.SeedModules(Coded(repo, registry));

            var modules = repo.Modules().Where(m => m.IsActive).ToList();
            // فلترٌ يشير إلى جدولٍ مبنيّ يصير قائمةَ صفوفه؛ وما عداه نوعُ قائمةٍ من قوائم النظام كما هو.
            var built = modules.Where(m => !m.IsCoded).Select(m => m.Key).ToHashSet();

            foreach (var module in modules)
            {
                var actions = repo.Actions(module.Id);
                var filters = repo.Filters(module.Id);

                var columns = repo.Columns(module.Id);

                if (module.IsCoded)
                {
                    Overlay(registry, module, columns, actions, filters, built);
                    continue;
                }

                // الترحيل يتبع النوع: التقرير بلا جدول فلا شيء يُنشأ، والسجل والحركة يُنشآن ويُتابَعان
                // في كل إقلاع — CreateTable آمنة للتكرار (IF NOT EXISTS)، فترقية النسخة لا تُسقط جدولاً.
                repo.EnsureBuiltTable(module, columns);

                // الوحدة المبنيّة تدخل شجرة الصلاحيات كأي وحدة مكتوبة.
                PermissionKeys.RegisterBuilt(module.Key);

                registry.Register(Build(module, columns, actions, filters, built));
            }
        }

        /// <summary>
        /// الصفحة المكتوبة تبقى صفحتها — نموذج عرضها وحوارها من الكود — ووصفُها يعلو على أعمدتها
        /// وأزرارها وفلاترها: تعديل عمود أو إضافته أو حذف زرّ يظهر في الصفحة الحقيقية وفي أي نسخة منها.
        /// إعادة التسجيل تستبدل بالمفتاح، فلا نسخة ثانية ولا مسار تصييرٍ ثانٍ.
        /// </summary>
        private static void Overlay(IModuleRegistry registry, BuilderModule module, List<BuilderColumn> columns,
            List<BuilderAction> actions, List<BuilderFilter> filters, HashSet<string> built)
        {
            var coded = registry.Get(module.Key);
            if (coded == null) return;

            registry.Register(coded with
            {
                Columns = Columns(columns, coded.Columns),
                EnabledActions = actions.Select(a => a.ActionKey).ToArray(),
                Filters = Filters(filters, built, coded)
            });
        }

        /// <summary>صفحات الكود كما تصفها تسجيلاتها: القسم من الخريطة، والنوع من حوارها، والقطع من إعلانها.</summary>
        private static List<CodedPage> Coded(IBuilderCatalog repo, IModuleRegistry registry)
        {
            var pages = new List<CodedPage>();

            // ترتيب القسم ثم ترتيب مفاتيحه فيه — فترتيب الشريط الجانبي يبقى كما هو بعد البذر.
            foreach (var section in repo.Sections().OrderBy(s => s.SortOrder))
                foreach (var key in (section.Modules ?? "")
                             .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var module = registry.Get(key);
                    if (module == null) continue;

                    pages.Add(new CodedPage(section.Key, module.Key, LocalizationService.Get(module.TitleKey),
                        KindOf(module), CodedColumns(module), CodedActions(module), CodedFilters(module)));
                }

            return pages;
        }

        private static BuilderKind KindOf(ModuleDefinition m) =>
            m.LayoutKind == LayoutKind.Report ? BuilderKind.Report
            : m.DocumentDialog != null        ? BuilderKind.Movement
                                              : BuilderKind.Record;

        /// <summary>عكسُ Columns أدناه: الشكل المُعلَن يعود وصفاً، فما يُعرَض هو ما يُحرَّر.</summary>
        private static List<BuilderColumn> CodedColumns(ModuleDefinition m) =>
            (m.Columns ?? new List<GridColumn>()).Select((c, i) => new BuilderColumn
            {
                Name = c.Binding, Header = c.Header, Width = c.Width, SortOrder = (i + 1) * 10,
                DataType = c.Format == "yyyy-MM-dd" ? BuilderDataType.Date
                         : c.Format == "N2"         ? BuilderDataType.Money
                                                    : BuilderDataType.Text,
                Footer = c.Footer == FooterAggregate.None ? null : c.Footer.ToString(),
                ShowInGrid = c.IsVisible, ShowInForm = false
            }).ToList();

        /// <summary>بلا تأشير = كل أزرار الكتالوج، وهو ما يقرؤه ActionToolbar فعلاً — فيُبذَر كما هو.</summary>
        private static List<BuilderAction> CodedActions(ModuleDefinition m) =>
            (m.EnabledActions ?? ToolbarAction.Catalogue.Keys.ToArray())
                .Select((key, i) => new BuilderAction { ActionKey = key, SortOrder = (i + 1) * 10 })
                .ToList();

        private static List<BuilderFilter> CodedFilters(ModuleDefinition m) =>
            (m.Filters ?? new List<FilterDefinition>()).Select((f, i) => new BuilderFilter
            {
                Key = f.Key, Label = LocalizationService.Get(f.LabelKey), Kind = f.Kind.ToString(),
                RefModule = f.PickerType, SortOrder = (i + 1) * 10
            }).ToList();

        private static ModuleDefinition Build(BuilderModule module, List<BuilderColumn> columns,
            List<BuilderAction> actions, List<BuilderFilter> filters, HashSet<string> built)
        {
            DynamicEntityService Service(IServiceProvider s) => new(module, columns,
                s.GetRequiredService<IPermissionService>(), s.GetRequiredService<ISettingsProvider>(),
                s.GetRequiredService<ILocalizationService>(), s.GetRequiredService<IAuditLogger>());

            // التقرير المبنيّ يمرّ من ReportRenderer نفسه: خدمة ودالة ووسائط وأعمدة — لا مسار ثانٍ.
            if (module.Kind == BuilderKind.Report)
                return new ModuleDefinition
                {
                    Key = module.Key, TitleKey = module.Title, PermissionPrefix = module.Key,
                    LayoutKind = LayoutKind.Report,
                    Report = new ReportDefinition
                    {
                        Key = module.Key, TitleKey = module.Title, PermissionKey = $"{module.Key}.View",
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
                TitleKey = module.Title,
                PermissionPrefix = module.Key,
                ViewModelFactory = s => new DynamicViewModel(Service(s), module.Key,
                    s.GetRequiredService<IPermissionService>(),
                    s.GetRequiredService<UI.Services.IToastService>(),
                    s.GetRequiredService<UI.Services.IDialogService>()),
                Columns = Columns(columns),
                Filters = Filters(filters, built, null),
                // بلا تأشير = كل أزرار الكتالوج، وهو حال كل وحدة مكتوبة.
                EnabledActions = actions.Count == 0 ? null : actions.Select(a => a.ActionKey).ToArray(),
                // السجل حوارُ حقول، والحركة رأسٌ وسطور، والتقرير بلا حوار — النوع يحكم.
                Dialog = module.Kind != BuilderKind.Record ? null : new DialogDefinition
                {
                    TitleKey = module.Title, TitleEditKey = module.Title,
                    ServiceType = typeof(DynamicEntityService),
                    ServiceFactory = s => Service(s),
                    // الصفّ قاموسٌ لا كيان، فنوعا الإنشاء والتعديل واحد — ApplyFields تكتب بالاسم لا بالخاصية.
                    CreateDtoType = typeof(ExpandoObject),
                    UpdateDtoType = typeof(ExpandoObject),
                    Fields = Fields(columns)
                },
                DocumentDialog = module.Kind != BuilderKind.Movement ? null : new DocumentDialogDefinition
                {
                    TitleKey = module.Title, TitleEditKey = module.Title,
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

        /// <summary>
        /// أعمدة الصفحة المكتوبة كما وصفها المستخدم: عمودٌ له صفّ يأخذ منه عنوانه وعرضه وإجماليه
        /// ويحتفظ بما لا يصفه الوصف (قالب الخلية، المحاذاة، العرض النجمي)؛ وصفٌّ بلا عمودٍ في الكود
        /// عمودٌ أضافه المستخدم؛ وعمودٌ بلا صفّ حذفه. والترتيب ترتيب الصفوف.
        /// </summary>
        private static List<GridColumn> Columns(List<BuilderColumn> columns, List<GridColumn> coded) =>
            Grid(columns, coded);

        private static List<GridColumn> Grid(List<BuilderColumn> columns, List<GridColumn> coded)
        {
            var written = (coded ?? new List<GridColumn>())
                .Where(c => c.Binding != null)
                .ToDictionary(c => c.Binding, c => c);

            var rows = Shown(columns).ToList();

            // النِّسب من خدمتها لا محسوبةً هنا: الجدول نسبيٌّ إن أُدخلت نسبة، والعرض النجميّ يوزّع
            // المتاح بها فيبقى التناسب واحداً على أي عرض شاشة وفي أي نسخة.
            var proportional = ColumnWidths.Proportional(rows);
            var shares = ColumnWidths.Shares(rows);

            return rows.Select(row =>
            {
                var width = proportional ? shares[row.Id] : row.Width;

                return written.TryGetValue(row.Name ?? "", out var column)
                    ? column with
                      {
                          Header = row.Header, Width = width, Footer = Footer(row.Footer),
                          IsStarWidth = proportional || column.IsStarWidth
                      }
                    : Column(row) with { Width = width, IsStarWidth = proportional };
            }).ToList();
        }

        private static IEnumerable<BuilderColumn> Shown(List<BuilderColumn> columns) =>
            columns.Where(c => c.ShowInGrid && !c.IsLine).OrderBy(c => c.SortOrder);

        private static GridColumn Column(BuilderColumn c) => new()
        {
            Header = c.Header,
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
                    LabelKey = c.Header,
                    Kind = Kind(c.DataType),
                    IsRequired = c.IsRequired,
                    MaxLength = c.MaxLength ?? 0,
                    // «من جدول»: نوع قائمة واحد عام يقرأ جدوله من اسمه، بدل فرعٍ مكتوب لكل جدول.
                    PickerType = c.DataType == BuilderDataType.Reference ? $"Table:{c.RefModule}:{c.RefDisplay}" : null
                }).ToList();

        /// <summary>سطور الحركة: نفس أعمدة الوصف المؤشَّرة سطراً، بعناوينها وعرضها.</summary>
        private static List<LineFieldDefinition> LineFields(List<BuilderColumn> columns) =>
            columns.Where(c => c.IsLine && c.Aggregate == BuilderAggregate.None)
                .Select(c => new LineFieldDefinition
                {
                    Key = c.Name,
                    Header = c.Header,
                    Kind = Kind(c.DataType),
                    Width = c.Width,
                    IsRequired = c.IsRequired,
                    PickerType = c.DataType == BuilderDataType.Reference ? $"Table:{c.RefModule}:{c.RefDisplay}" : null
                }).ToList();

        /// <summary>إجماليات حيّة أسفل السطور لكل عمود أُعلن له إجمالي — نفس LineTotals في القيود.</summary>
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

        /// <summary>
        /// قائمة الفلتر تُستورَد من الصفحة نفسها: العمود الذي له حقلٌ مُعلَن في حوارها يأخذ قائمة ذلك
        /// الحقل وعنوانه — فالفلتر يقرأ من مصدر الشاشة ولا يُعلن مصدراً ثانياً. وإن لم يكن للعمود حقل،
        /// فالمرجع المخزَّن: جدولٌ مبنيّ يصير قائمةَ صفوفه، وما عداه نوعُ قائمةٍ من قوائم النظام كما هو.
        /// </summary>
        private static FilterDefinition Filter(BuilderFilter filter, HashSet<string> built, ModuleDefinition page)
        {
            var declared = Declared(page).FirstOrDefault(field => field.Key == filter.Key);

            return new FilterDefinition
            {
                Key = filter.Key,
                LabelKey = string.IsNullOrWhiteSpace(filter.Label) ? declared?.LabelKey : filter.Label,
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
