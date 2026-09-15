using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Builder;
using PrimeERP.Composition.Renderers;
using PrimeERP.Domain.Results;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Tree;
using PrimeERP.UI.ViewModels;
using PrimeERP.Platform.Permissions;
using System.Dynamic;

namespace PrimeERP.Modules
{
    /// <summary>
    /// شاشات وحدة البناء نفسها — إعلانات فوق جداول الوصف، أي أن الوحدة مبنيّةٌ بمفرداتها. لا شاشة
    /// خاصة ولا XAML: نفس CrudPageRenderer الذي يُصيِّر كل شاشة في النظام.
    ///
    /// سلسلة الطلب محفوظة في الحقول: الصفحة تطلب قسماً، والعمود يطلب صفحة، والزرّ يطلب صفحة —
    /// فلا يُنشأ يتيم.
    /// </summary>
    public static class BuilderRegistrations
    {
        public static void RegisterAll(IModuleRegistry registry)
        {
            // شاشات الوحدة وحداتٌ كاملة: لكلٍّ بادئتها ومفاتيحها المولَّدة (View/Create/Edit/Delete).
            // كانت تستعير بادئة "Settings" — وهي بلا Create ولا Delete، فاختفى زرّا الإضافة والحذف.
            foreach (var key in new[] { "BuilderSections", "BuilderModules", "BuilderColumns", "BuilderActions", "BuilderFilters", "BuilderExport" })
                PermissionKeys.RegisterBuilt(key);

            Section(registry);
            Module(registry);
            Column(registry);
            Action(registry);
            Filter(registry);
            Export(registry);
        }

        private static void Section(IModuleRegistry registry) => registry.Register(new ModuleDefinition
        {
            Key = "BuilderSections", Reorderable = true, TitleKey = "Str.Builder.Sections", PermissionPrefix = "BuilderSections",
            ViewModelFactory = s => Vm<BuilderSectionsService>(s, "BuilderSections"),
            Columns = new()
            {
                new() { Header = LocalizationService.Get("Str.Builder.Key"), Binding = nameof(BuilderSection.Key), Width = 140 },
                new() { Header = LocalizationService.Get("Str.Builder.Title"), Binding = nameof(BuilderSection.Title), Width = 240, IsStarWidth = true },
                new() { Header = LocalizationService.Get("Str.Builder.Order"), Binding = nameof(BuilderSection.SortOrder), Width = 90, Align = ColumnAlign.Center },
            },
            Dialog = Dialog<BuilderSectionsService>("Str.Builder.Section", new()
            {
                new() { Key = nameof(BuilderSection.Key), LabelKey = "Str.Builder.Key", Kind = FieldKind.Text, IsRequired = true, MaxLength = 60 },
                new() { Key = nameof(BuilderSection.Title), LabelKey = "Str.Builder.Title", Kind = FieldKind.Text, IsRequired = true, MaxLength = 120 },
                new() { Key = nameof(BuilderSection.IconKey), LabelKey = "Str.Builder.Icon", Kind = FieldKind.Text, MaxLength = 60 },
            })
        });

        private static void Module(IModuleRegistry registry) => registry.Register(new ModuleDefinition
        {
            Key = "BuilderModules", Reorderable = true, TitleKey = "Str.Builder.Modules", PermissionPrefix = "BuilderModules",
            ViewModelFactory = s => Vm<BuilderModulesService>(s, "BuilderModules"),
            // القسم يُختار أولاً: لا صفحة بلا قسم، ولا زرّ ولا فلتر ولا عمود بلا صفحة.
            Filters = new()
            {
                new() { Key = nameof(DynamicFilter.SectionId), LabelKey = "Str.Builder.Sections", PickerType = "BuilderSection" },
            },
            Columns = new()
            {
                new() { Header = LocalizationService.Get("Str.Builder.Key"), Binding = nameof(BuilderModule.Key), Width = 140 },
                new() { Header = LocalizationService.Get("Str.Builder.Title"), Binding = nameof(BuilderModule.Title), Width = 220, IsStarWidth = true },
                new() { Header = LocalizationService.Get("Str.Builder.Sections"), Binding = "SectionName", Width = 140 },
                new() { Header = LocalizationService.Get("Str.Builder.Kind"), Binding = "KindName", Width = 100, Align = ColumnAlign.Center },
                new() { Header = LocalizationService.Get("Str.Builder.Order"), Binding = nameof(BuilderModule.SortOrder), Width = 80, Align = ColumnAlign.Center },
                new() { Header = LocalizationService.Get("Str.Builder.Table"), Binding = nameof(BuilderModule.TableName), Width = 160 },
            },
            Dialog = Dialog<BuilderModulesService>("Str.Builder.Module", new()
            {
                // النوع أول سؤال: هو ما يحكم القطع التالية والترحيل والصلاحيات.
                new() { Key = nameof(BuilderModule.Kind), LabelKey = "Str.Builder.Kind", Kind = FieldKind.Picker, IsRequired = true, PickerType = "BuilderKind" },
                new() { Key = nameof(BuilderModule.SectionId), LabelKey = "Str.Builder.Sections", Kind = FieldKind.Picker, IsRequired = true, PickerType = "BuilderSection" },
                new() { Key = nameof(BuilderModule.Key), LabelKey = "Str.Builder.Key", Kind = FieldKind.Text, IsRequired = true, MaxLength = 60, IsReadOnlyOnEdit = true },
                new() { Key = nameof(BuilderModule.Title), LabelKey = "Str.Builder.Title", Kind = FieldKind.Text, IsRequired = true, MaxLength = 160 },
                // الجدول للسجل والحركة، والمصدر للتقرير — كلٌّ يظهر بنوعه.
                new() { Key = nameof(BuilderModule.TableName), LabelKey = "Str.Builder.Table", Kind = FieldKind.Text, MaxLength = 60,
                        VisibleWhenField = nameof(BuilderModule.Kind), VisibleWhenValue = (int)Domain.Enums.BuilderKind.Record },
                new() { Key = nameof(BuilderModule.LineTable), LabelKey = "Str.Builder.LineTable", Kind = FieldKind.Text, MaxLength = 60,
                        VisibleWhenField = nameof(BuilderModule.Kind), VisibleWhenValue = (int)Domain.Enums.BuilderKind.Movement },
                new() { Key = nameof(BuilderModule.SourceKey), LabelKey = "Str.Builder.ReadsFrom", Kind = FieldKind.Picker, PickerType = "BuilderModule",
                        VisibleWhenField = nameof(BuilderModule.Kind), VisibleWhenValue = (int)Domain.Enums.BuilderKind.Report },
                new() { Key = nameof(BuilderModule.CopiedFrom), LabelKey = "Str.Builder.CopyOf", Kind = FieldKind.Picker, PickerType = "AnyModule" },
                new() { Key = nameof(BuilderModule.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
            })
        });

        private static void Column(IModuleRegistry registry) => registry.Register(new ModuleDefinition
        {
            Key = "BuilderColumns", Reorderable = true, TitleKey = "Str.Builder.Columns", PermissionPrefix = "BuilderColumns",
            ViewModelFactory = s => Vm<BuilderColumnsService>(s, "BuilderColumns"),
            Filters = new()
            {
                new() { Key = nameof(DynamicFilter.SectionId), LabelKey = "Str.Builder.Sections", PickerType = "BuilderSection" },
                new() { Key = nameof(DynamicFilter.ModuleId), LabelKey = "Str.Builder.Modules", PickerType = "BuilderModule",
                        PickerFilterField = nameof(DynamicFilter.SectionId) },
            },
            Columns = new()
            {
                new() { Header = LocalizationService.Get("Str.Builder.ColumnName"), Binding = nameof(BuilderColumn.Name), Width = 140 },
                new() { Header = LocalizationService.Get("Str.Builder.Header"), Binding = nameof(BuilderColumn.Header), Width = 200, IsStarWidth = true },
                new() { Header = LocalizationService.Get("Str.Builder.Modules"), Binding = "ModuleName", Width = 160 },
                new() { Header = LocalizationService.Get("Str.Builder.Sections"), Binding = "SectionName", Width = 140 },
                new() { Header = LocalizationService.Get("Str.Builder.Kind"), Binding = nameof(BuilderColumn.DataType), Width = 110, Align = ColumnAlign.Center },
                new() { Header = LocalizationService.Get("Str.Builder.WidthPercent"), Binding = nameof(BuilderColumn.WidthPercent), Width = 90, Align = ColumnAlign.Center, Format = "N1" },
                new() { Header = LocalizationService.Get("Str.Builder.Footer"), Binding = nameof(BuilderColumn.Footer), Width = 90, Align = ColumnAlign.Center },
            },
            Dialog = Dialog<BuilderColumnsService>("Str.Builder.Column", new()
            {
                new() { Key = nameof(BuilderColumn.ModuleId), LabelKey = "Str.Builder.Modules", Kind = FieldKind.Picker, IsRequired = true, PickerType = "BuilderModule" },
                new() { Key = nameof(BuilderColumn.Header), LabelKey = "Str.Builder.Header", Kind = FieldKind.Text, IsRequired = true, MaxLength = 120 },
                new() { Key = nameof(BuilderColumn.Name), LabelKey = "Str.Builder.ColumnName", Kind = FieldKind.Text, IsRequired = true, MaxLength = 60 },
                new() { Key = nameof(BuilderColumn.DataType), LabelKey = "Str.Builder.DataType", Kind = FieldKind.Picker, IsRequired = true, PickerType = "BuilderDataType" },
                // «من جدول»: القائمة تتبع النوع، فيظهر الجدول المرجعي عند اختيار مرجع.
                new() { Key = nameof(BuilderColumn.RefModule), LabelKey = "Str.Builder.FromTable", Kind = FieldKind.Picker, PickerType = "BuilderModule",
                        VisibleWhenField = nameof(BuilderColumn.DataType), VisibleWhenValue = (int)Domain.Enums.BuilderDataType.Reference },
                new() { Key = nameof(BuilderColumn.RefDisplay), LabelKey = "Str.Builder.DisplayColumn", Kind = FieldKind.Text, MaxLength = 60,
                        VisibleWhenField = nameof(BuilderColumn.DataType), VisibleWhenValue = (int)Domain.Enums.BuilderDataType.Reference },
                // العمود المحسوب: تجميعٌ من جدول مرتبط — يُقرأ ولا يُخزَّن.
                new() { Key = nameof(BuilderColumn.Aggregate), LabelKey = "Str.Builder.Computed", Kind = FieldKind.Picker, PickerType = "BuilderAggregate" },
                new() { Key = nameof(BuilderColumn.AggFrom), LabelKey = "Str.Builder.AggFrom", Kind = FieldKind.Picker, PickerType = "BuilderModule" },
                new() { Key = nameof(BuilderColumn.AggColumn), LabelKey = "Str.Builder.AggColumn", Kind = FieldKind.Text, MaxLength = 60 },
                new() { Key = nameof(BuilderColumn.AggMatch), LabelKey = "Str.Builder.AggMatch", Kind = FieldKind.Text, MaxLength = 60 },
                new() { Key = nameof(BuilderColumn.IsRequired), LabelKey = "Str.Builder.Required", Kind = FieldKind.Check },
                new() { Key = nameof(BuilderColumn.IsUnique), LabelKey = "Str.Builder.Unique", Kind = FieldKind.Check },
                new() { Key = nameof(BuilderColumn.MaxLength), LabelKey = "Str.Builder.Length", Kind = FieldKind.Number },
                new() { Key = nameof(BuilderColumn.Footer), LabelKey = "Str.Builder.Footer", Kind = FieldKind.Picker, PickerType = "FooterAggregate" },
                new() { Key = nameof(BuilderColumn.ShowInGrid), LabelKey = "Str.Builder.InGrid", Kind = FieldKind.Check, DefaultValue = true },
                new() { Key = nameof(BuilderColumn.ShowInForm), LabelKey = "Str.Builder.InForm", Kind = FieldKind.Check, DefaultValue = true },
                new() { Key = nameof(BuilderColumn.IsLine), LabelKey = "Str.Builder.IsLine", Kind = FieldKind.Check },
                new() { Key = nameof(BuilderColumn.Width), LabelKey = "Str.Builder.Width", Kind = FieldKind.Number, DefaultValue = 140m },
                new() { Key = nameof(BuilderColumn.WidthPercent), LabelKey = "Str.Builder.WidthPercent", Kind = FieldKind.Number },
            })
        });

        private static void Action(IModuleRegistry registry) => registry.Register(new ModuleDefinition
        {
            Key = "BuilderActions", Reorderable = true, TitleKey = "Str.Builder.Actions", PermissionPrefix = "BuilderActions",
            ViewModelFactory = s => Vm<BuilderActionsService>(s, "BuilderActions"),
            Filters = new()
            {
                new() { Key = nameof(DynamicFilter.SectionId), LabelKey = "Str.Builder.Sections", PickerType = "BuilderSection" },
                new() { Key = nameof(DynamicFilter.ModuleId), LabelKey = "Str.Builder.Modules", PickerType = "BuilderModule",
                        PickerFilterField = nameof(DynamicFilter.SectionId) },
            },
            Columns = new()
            {
                new() { Header = LocalizationService.Get("Str.Builder.Button"), Binding = nameof(BuilderAction.ActionKey), Width = 200, IsStarWidth = true },
                new() { Header = LocalizationService.Get("Str.Builder.Modules"), Binding = "ModuleName", Width = 160 },
                new() { Header = LocalizationService.Get("Str.Builder.Sections"), Binding = "SectionName", Width = 140 },
                new() { Header = LocalizationService.Get("Str.Builder.OnRow"), Binding = nameof(BuilderAction.OnTable), Width = 100, Align = ColumnAlign.Center },
            },
            Dialog = Dialog<BuilderActionsService>("Str.Builder.Action", new()
            {
                new() { Key = nameof(BuilderAction.ModuleId), LabelKey = "Str.Builder.Modules", Kind = FieldKind.Picker, IsRequired = true, PickerType = "BuilderModule" },
                // الزرّ يُختار من كتالوج النظام لا يُكتب.
                new() { Key = nameof(BuilderAction.ActionKey), LabelKey = "Str.Builder.Button", Kind = FieldKind.Picker, IsRequired = true, PickerType = "ToolbarAction" },
                new() { Key = nameof(BuilderAction.OnTable), LabelKey = "Str.Builder.OnRow", Kind = FieldKind.Check },
            })
        });

        private static void Filter(IModuleRegistry registry) => registry.Register(new ModuleDefinition
        {
            Key = "BuilderFilters", Reorderable = true, TitleKey = "Str.Builder.Filters", PermissionPrefix = "BuilderFilters",
            ViewModelFactory = s => Vm<BuilderFiltersService>(s, "BuilderFilters"),
            Filters = new()
            {
                new() { Key = nameof(DynamicFilter.SectionId), LabelKey = "Str.Builder.Sections", PickerType = "BuilderSection" },
                new() { Key = nameof(DynamicFilter.ModuleId), LabelKey = "Str.Builder.Modules", PickerType = "BuilderModule",
                        PickerFilterField = nameof(DynamicFilter.SectionId) },
            },
            Columns = new()
            {
                new() { Header = LocalizationService.Get("Str.Builder.Key"), Binding = nameof(BuilderFilter.Key), Width = 140 },
                new() { Header = LocalizationService.Get("Str.Builder.Filter"), Binding = nameof(BuilderFilter.Label), Width = 200, IsStarWidth = true },
                new() { Header = LocalizationService.Get("Str.Builder.Modules"), Binding = "ModuleName", Width = 160 },
                new() { Header = LocalizationService.Get("Str.Builder.Sections"), Binding = "SectionName", Width = 140 },
                new() { Header = LocalizationService.Get("Str.Builder.Kind"), Binding = nameof(BuilderFilter.Kind), Width = 110, Align = ColumnAlign.Center },
            },
            Dialog = Dialog<BuilderFiltersService>("Str.Builder.Filter", new()
            {
                new() { Key = nameof(BuilderFilter.ModuleId), LabelKey = "Str.Builder.Modules", Kind = FieldKind.Picker, IsRequired = true, PickerType = "BuilderModule" },
                new() { Key = nameof(BuilderFilter.Key), LabelKey = "Str.Builder.Key", Kind = FieldKind.Text, IsRequired = true, MaxLength = 60 },
                new() { Key = nameof(BuilderFilter.Label), LabelKey = "Str.Builder.Header", Kind = FieldKind.Text, MaxLength = 120 },
                new() { Key = nameof(BuilderFilter.Kind), LabelKey = "Str.Builder.Kind", Kind = FieldKind.Picker, PickerType = "BuilderFilterKind" },
                new() { Key = nameof(BuilderFilter.RefModule), LabelKey = "Str.Builder.FromTable", Kind = FieldKind.Picker, PickerType = "BuilderModule" },
            })
        });

        /// <summary>
        /// إنشاء برنامج: شجرة الأقسام وصفحاتها مؤشَّرة، والوضع مصدرها أعلاها، ومسارها من نافذة النظام.
        /// شجرةُ التأشير نفسها التي تُصيَّر بها صلاحيات الأدوار — إعلانٌ لا شاشة.
        /// </summary>
        private static void Export(IModuleRegistry registry) => registry.Register(new ModuleDefinition
        {
            Key = "BuilderExport", TitleKey = "Str.Builder.Export", PermissionPrefix = "BuilderExport",
            LayoutKind = LayoutKind.TreeCheckList,
            TreeCheckList = new TreeCheckListDefinition
            {
                TitleKey = "Str.Builder.Export",
                SourceLabelKey = "Str.Builder.Customer",
                Mode = TreeCheckMode.TwoState,
                SaveTextKey = "Str.Builder.CreateProgram",
                // المصدر عميلٌ لا وضعٌ: الوضع صار صفةً على ترخيصه، وشجرته تُبنى بما اختير له.
                SourceItems = services => services.GetRequiredService<ILicenseService>().GetAll().Value
                    .Select(l => new SourceOption
                    {
                        Id = l.Id,
                        Display = string.IsNullOrWhiteSpace(l.Location) ? l.CustomerName : $"{l.CustomerName} — {l.Location}"
                    }).ToList(),
                SourceNote = (services, id) => Licensed(services, id)?.Serial ?? "",
                BuildTree = (services, id) => LicenseTree(services, id),
                ApplyRules = Inherit,
                Save = (services, id, nodes) => CreateEdition(services, id, nodes),
                Actions = new List<TreeCheckListAction>
                {
                    new() { TextKey = "Str.Builder.NewSerial", Variant = "primary", RequiresSource = false, Run = (services, _, __) => NewSerial(services) },
                    new() { TextKey = "Str.Builder.Installer", RunAsync = (services, id, _) => CustomerInstaller(services, id) },
                    new() { TextKey = "Str.SelectAll", RequiresSource = false, Run = (_, __, nodes) => SetAll(nodes, NodeCheckState.Checked) },
                    new() { TextKey = "Str.ClearAll",  RequiresSource = false, Run = (_, __, nodes) => SetAll(nodes, NodeCheckState.Unchecked) },
                }
            }
        });

        private static LicenseDto Licensed(IServiceProvider services, int id) =>
            id <= 0 ? null : services.GetRequiredService<ILicenseService>().GetById(id).Value;

        /// <summary>شجرة العميل: صفحات وضعه، مؤشَّرٌ منها ما سبق أن اختير له — والترخيص الجديد بالكل.</summary>
        private static List<TreeNodeViewModel> LicenseTree(IServiceProvider services, int licenseId)
        {
            var license = Licensed(services, licenseId);

            // بلا عميلٍ مختار تُعرَض الشجرة كاملةً كما كانت: الصفحة تقول ما يمكن منحه، والبناء وحده يطلب عميلاً.
            if (license == null) return EditionTree(services, simplified: false);

            var roots = EditionTree(services, license.Simplified);
            if (license.ModuleKeys.Count == 0) return roots;

            foreach (var page in roots.SelectMany(section => section.Children).Where(p => p.IsCheckEnabled))
                page.CheckState = license.ModuleKeys.Contains(page.Id) ? NodeCheckState.Checked : NodeCheckState.Unchecked;

            Inherit(roots, null);
            return roots;
        }

        /// <summary>
        /// منصِّب العميل: الملف الصغير نفسه يُنزَّل من الخادم، ويُكتب سريال العميل بجواره فلا يُمليه أحد.
        /// نفس بوّابة الشبكة ونفس حوار التقدّم — بلا آلية ثالثة.
        /// </summary>
        private static async Task<Result> CustomerInstaller(IServiceProvider services, int licenseId)
        {
            var license = Licensed(services, licenseId);
            if (license == null) return Result.Fail("اختر العميل أولاً", ErrorCode.ValidationFailed);

            var folder = FolderOutput.Pick(LocalizationService.Get("Str.Builder.ChooseFolder"));
            if (string.IsNullOrWhiteSpace(folder)) return Result.Fail("لم يُختَر مجلد", ErrorCode.ValidationFailed);

            var server = services.GetRequiredService<Platform.Settings.ISettingsProvider>()
                .Get(Platform.Settings.SettingKeys.Developer.ServerUrl, "").TrimEnd('/');

            if (string.IsNullOrWhiteSpace(server))
                return Result.Fail("اضبط عنوان خادم التراخيص من الإعدادات", ErrorCode.ValidationFailed);

            var target = System.IO.Path.Combine(folder, "PrimeERP.Setup.exe");
            var http = services.GetRequiredService<Platform.Net.IHttpGateway>();

            using var handle = services.GetRequiredService<UI.Services.IDialogService>()
                .ShowProgress(LocalizationService.Get("Str.Builder.Installer"), license.CustomerName);

            var progress = new Progress<double>(percent => handle.Report(percent, LocalizationService.Get("Str.Builder.Installer")));

            var (ok, error) = await http.DownloadAsync($"{server}/package/PrimeERP.Setup.exe", target, progress);
            if (!ok) return Result.Fail($"تعذّر تنزيل المنصِّب: {error}", ErrorCode.Unexpected);

            // السريال بجوار المنصِّب: يقرؤه عند فتحه فلا يكتبه العميل ولا يخطئ فيه.
            await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(folder, "serial.txt"), license.Serial);

            return Result.Ok();
        }

        /// <summary>سريالٌ جديد بنموذجٍ صغير — نفس آلية أي حوار إضافة، بلا نافذة مكتوبة.</summary>
        private static void NewSerial(IServiceProvider services) =>
            DialogRenderer.ShowAndSave(new DialogDefinition
            {
                TitleKey = "Str.Builder.NewSerial", TitleEditKey = "Str.Builder.NewSerial", GridColumns = 1,
                ServiceType = typeof(ILicenseService),
                CreateDtoType = typeof(CreateLicenseDto), UpdateDtoType = typeof(CreateLicenseDto),
                Fields = new List<FieldDefinition>
                {
                    new() { Key = nameof(CreateLicenseDto.CustomerName), LabelKey = "Str.Builder.Customer", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                    new() { Key = nameof(CreateLicenseDto.Location), LabelKey = "Str.Builder.Location", Kind = FieldKind.Text, MaxLength = 200 },
                    new() { Key = nameof(CreateLicenseDto.Simplified), LabelKey = "Str.Builder.Simplified", Kind = FieldKind.Check },
                }
            }, services, services.GetRequiredService<UI.Services.IToastService>());

        /// <summary>نفس أقسام الشريط الجانبي وصفحاته الظاهرة في الوضع المختار — لا قائمة ثانية تُكتب.</summary>
        private static List<TreeNodeViewModel> EditionTree(IServiceProvider services, bool simplified)
        {
            var modules = services.GetRequiredService<IModuleRegistry>()
                .VisibleFor(simplified).ToDictionary(m => m.Key);

            var roots = new List<TreeNodeViewModel>();

            foreach (var group in NavigationSource.Groups(services))
            {
                // وحدة البناء لا تُصدَّر: النسخة برنامجٌ يعمل، لا أداة بناء.
                if (group.Key == "Builder") continue;

                // المحاسبة والإعدادات في كل نسخة — تُعرَض مؤشَّرة ولا تُنزع.
                var always = NavigationMap.Protected.Contains(group.Key);

                var section = new TreeNodeViewModel
                {
                    Id = group.Key, DisplayText = group.Text, IsExpanded = true,
                    CheckState = NodeCheckState.Checked, IsCheckEnabled = !always
                };

                foreach (var key in group.Keys.Where(modules.ContainsKey))
                    section.AddChild(new TreeNodeViewModel
                    {
                        Id = key, DisplayText = LocalizationService.Get(modules[key].TitleKey), IsLeaf = true,
                        CheckState = NodeCheckState.Checked, IsCheckEnabled = !always
                    });

                if (section.Children.Count > 0) roots.Add(section);
            }

            return roots;
        }

        /// <summary>
        /// القسم يورّث صفحاته والصفحات تُحدّد قسمها: الشجرة نفسها لا تورّث التأشير، والحفظ يقرأ الصفحات
        /// وحدها — فبلا هذه القاعدة إلغاءُ قسمٍ لا يستثني منه شيئاً. والمحميّ يبقى مؤشَّراً دائماً.
        /// </summary>
        private static void Inherit(List<TreeNodeViewModel> roots, TreeNodeViewModel changed)
        {
            if (changed != null && changed.Children.Count > 0)
                foreach (var page in changed.Children) page.CheckState = changed.CheckState;

            foreach (var section in roots)
            {
                if (NavigationMap.Protected.Contains(section.Id))
                {
                    section.CheckState = NodeCheckState.Checked;
                    foreach (var page in section.Children) page.CheckState = NodeCheckState.Checked;
                    continue;
                }

                section.CheckState = section.Children.All(page => page.CheckState == NodeCheckState.Checked)
                    ? NodeCheckState.Checked
                    : NodeCheckState.Unchecked;
            }
        }

        private static void SetAll(List<TreeNodeViewModel> roots, NodeCheckState state)
        {
            foreach (var section in roots.Where(r => !NavigationMap.Protected.Contains(r.Id)))
            {
                section.CheckState = state;
                foreach (var page in section.Children) page.CheckState = state;
            }
        }

        /// <summary>
        /// المسار أولاً، ثم حوار التقدّم القائم بينما العمل يجري خارج خيط الواجهة — النسخ آلاف الملفات،
        /// وتشغيله على الخيط يُجمّد النافذة. Progress يعيد كل تقرير إلى خيط الواجهة بنفسه.
        /// </summary>
        private static async Task<Result> CreateEdition(IServiceProvider services, int licenseId,
            List<TreeNodeViewModel> roots)
        {
            var license = Licensed(services, licenseId);
            if (license == null) return Result.Fail("اختر العميل أولاً", ErrorCode.ValidationFailed);

            var keys = roots.SelectMany(section => section.Children)
                .Where(page => page.CheckState == NodeCheckState.Checked)
                .Select(page => page.Id).ToList();

            var folder = FolderOutput.Pick(LocalizationService.Get("Str.Builder.ChooseFolder"));

            var edition = new CreateEditionDto
            {
                ModuleKeys = keys,
                Simplified = license.Simplified,
                TargetFolder = folder
            };

            var service = services.GetRequiredService<IProgramEditionService>();
            using var handle = services.GetRequiredService<UI.Services.IDialogService>()
                .ShowProgress(LocalizationService.Get("Str.Builder.Export"),
                              LocalizationService.Get("Str.Builder.Creating"));

            var progress = new Progress<EditionProgress>(step => handle.Report(step.Percent, step.Stage));
            var licenses = services.GetRequiredService<ILicenseService>();

            // الحفظ على الترخيص يمرّ بالشبكة، فمكانه خارج خيط الواجهة داخل نفس المهمة — وإلا جمّد
            // النافذة قبل أن يظهر مربّع التقدّم أصلاً.
            return await Task.Run(() =>
            {
                var saved = licenses.SaveManifest(licenseId, keys);
                if (saved.IsFailure) return saved;

                return service.Create(edition, progress);
            });
        }

        // ===================== المشترك =====================

        private static object Vm<TService>(IServiceProvider services, string permissionPrefix)
            where TService : class, IRowService =>
            new DynamicViewModel(services.GetRequiredService<TService>(), permissionPrefix,
                services.GetRequiredService<Platform.Permissions.IPermissionService>(),
                services.GetRequiredService<UI.Services.IToastService>(),
                services.GetRequiredService<UI.Services.IDialogService>());

        private static DialogDefinition Dialog<TService>(string title, List<FieldDefinition> fields)
            where TService : class => new()
        {
            TitleKey = title, TitleEditKey = title,
            ServiceType = typeof(TService),
            CreateDtoType = typeof(ExpandoObject),
            UpdateDtoType = typeof(ExpandoObject),
            Fields = fields
        };
    }
}
