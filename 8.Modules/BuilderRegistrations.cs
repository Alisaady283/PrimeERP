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
            Key = "BuilderSections", TitleKey = "Str.Builder.Sections", PermissionPrefix = "BuilderSections",
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
                new() { Key = nameof(BuilderSection.SortOrder), LabelKey = "Str.Builder.Order", Kind = FieldKind.Number },
            })
        });

        private static void Module(IModuleRegistry registry) => registry.Register(new ModuleDefinition
        {
            Key = "BuilderModules", TitleKey = "Str.Builder.Modules", PermissionPrefix = "BuilderModules",
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
                new() { Key = nameof(BuilderModule.SortOrder), LabelKey = "Str.Builder.Order", Kind = FieldKind.Number },
                new() { Key = nameof(BuilderModule.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
            })
        });

        private static void Column(IModuleRegistry registry) => registry.Register(new ModuleDefinition
        {
            Key = "BuilderColumns", TitleKey = "Str.Builder.Columns", PermissionPrefix = "BuilderColumns",
            ViewModelFactory = s => Vm<BuilderColumnsService>(s, "BuilderColumns"),
            Filters = new()
            {
                new() { Key = nameof(DynamicFilter.SectionId), LabelKey = "Str.Builder.Sections", PickerType = "BuilderSection" },
                new() { Key = nameof(DynamicFilter.ModuleId), LabelKey = "Str.Builder.Modules", PickerType = "BuilderModule",
                        PickerFilterField = nameof(DynamicFilter.SectionId) },
            },
            Columns = new()
            {
                new() { Header = LocalizationService.Get("Str.Builder.Modules"), Binding = "ModuleName", Width = 160 },
                new() { Header = LocalizationService.Get("Str.Builder.Header"), Binding = nameof(BuilderColumn.Header), Width = 200, IsStarWidth = true },
                new() { Header = "العمود", Binding = nameof(BuilderColumn.Name), Width = 140 },
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
                new() { Key = nameof(BuilderColumn.SortOrder), LabelKey = "Str.Builder.Order", Kind = FieldKind.Number },
            })
        });

        private static void Action(IModuleRegistry registry) => registry.Register(new ModuleDefinition
        {
            Key = "BuilderActions", TitleKey = "Str.Builder.Actions", PermissionPrefix = "BuilderActions",
            ViewModelFactory = s => Vm<BuilderActionsService>(s, "BuilderActions"),
            Filters = new()
            {
                new() { Key = nameof(DynamicFilter.SectionId), LabelKey = "Str.Builder.Sections", PickerType = "BuilderSection" },
                new() { Key = nameof(DynamicFilter.ModuleId), LabelKey = "Str.Builder.Modules", PickerType = "BuilderModule",
                        PickerFilterField = nameof(DynamicFilter.SectionId) },
            },
            Columns = new()
            {
                new() { Header = LocalizationService.Get("Str.Builder.Modules"), Binding = "ModuleName", Width = 160 },
                new() { Header = LocalizationService.Get("Str.Builder.Button"), Binding = nameof(BuilderAction.ActionKey), Width = 200, IsStarWidth = true },
                new() { Header = LocalizationService.Get("Str.Builder.OnRow"), Binding = nameof(BuilderAction.OnTable), Width = 100, Align = ColumnAlign.Center },
            },
            Dialog = Dialog<BuilderActionsService>("Str.Builder.Action", new()
            {
                new() { Key = nameof(BuilderAction.ModuleId), LabelKey = "Str.Builder.Modules", Kind = FieldKind.Picker, IsRequired = true, PickerType = "BuilderModule" },
                // الزرّ يُختار من كتالوج النظام لا يُكتب.
                new() { Key = nameof(BuilderAction.ActionKey), LabelKey = "Str.Builder.Button", Kind = FieldKind.Picker, IsRequired = true, PickerType = "ToolbarAction" },
                new() { Key = nameof(BuilderAction.OnTable), LabelKey = "Str.Builder.OnRow", Kind = FieldKind.Check },
                new() { Key = nameof(BuilderAction.SortOrder), LabelKey = "Str.Builder.Order", Kind = FieldKind.Number },
            })
        });

        private static void Filter(IModuleRegistry registry) => registry.Register(new ModuleDefinition
        {
            Key = "BuilderFilters", TitleKey = "Str.Builder.Filters", PermissionPrefix = "BuilderFilters",
            ViewModelFactory = s => Vm<BuilderFiltersService>(s, "BuilderFilters"),
            Filters = new()
            {
                new() { Key = nameof(DynamicFilter.SectionId), LabelKey = "Str.Builder.Sections", PickerType = "BuilderSection" },
                new() { Key = nameof(DynamicFilter.ModuleId), LabelKey = "Str.Builder.Modules", PickerType = "BuilderModule",
                        PickerFilterField = nameof(DynamicFilter.SectionId) },
            },
            Columns = new()
            {
                new() { Header = LocalizationService.Get("Str.Builder.Modules"), Binding = "ModuleName", Width = 160 },
                new() { Header = LocalizationService.Get("Str.Builder.Filter"), Binding = nameof(BuilderFilter.Label), Width = 200, IsStarWidth = true },
                new() { Header = LocalizationService.Get("Str.Builder.Kind"), Binding = nameof(BuilderFilter.Kind), Width = 110, Align = ColumnAlign.Center },
            },
            Dialog = Dialog<BuilderFiltersService>("Str.Builder.Filter", new()
            {
                new() { Key = nameof(BuilderFilter.ModuleId), LabelKey = "Str.Builder.Modules", Kind = FieldKind.Picker, IsRequired = true, PickerType = "BuilderModule" },
                new() { Key = nameof(BuilderFilter.Key), LabelKey = "Str.Builder.Key", Kind = FieldKind.Text, IsRequired = true, MaxLength = 60 },
                new() { Key = nameof(BuilderFilter.Label), LabelKey = "Str.Builder.Header", Kind = FieldKind.Text, MaxLength = 120 },
                new() { Key = nameof(BuilderFilter.Kind), LabelKey = "Str.Builder.Kind", Kind = FieldKind.Picker, PickerType = "BuilderFilterKind" },
                new() { Key = nameof(BuilderFilter.RefModule), LabelKey = "Str.Builder.FromTable", Kind = FieldKind.Picker, PickerType = "BuilderModule" },
                new() { Key = nameof(BuilderFilter.SortOrder), LabelKey = "Str.Builder.Order", Kind = FieldKind.Number },
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
                SourceLabelKey = "Str.Builder.Mode",
                Mode = TreeCheckMode.TwoState,
                SaveTextKey = "Str.Builder.CreateProgram",
                SourceItems = _ => new List<SourceOption>
                {
                    new() { Id = 1, Display = LocalizationService.Get("Str.Builder.Simplified") },
                    new() { Id = 2, Display = LocalizationService.Get("Str.Builder.FullCycle") },
                },
                BuildTree = (services, mode) => EditionTree(services, mode == 1),
                ApplyRules = Inherit,
                Save = (services, mode, nodes) => CreateEdition(services, mode == 1, nodes),
                Actions = new List<TreeCheckListAction>
                {
                    new() { TextKey = "Str.SelectAll", Run = (_, __, nodes) => SetAll(nodes, NodeCheckState.Checked) },
                    new() { TextKey = "Str.ClearAll",  Run = (_, __, nodes) => SetAll(nodes, NodeCheckState.Unchecked) },
                }
            }
        });

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
        private static async Task<Result> CreateEdition(IServiceProvider services, bool simplified,
            List<TreeNodeViewModel> roots)
        {
            var folder = FolderOutput.Pick(LocalizationService.Get("Str.Builder.ChooseFolder"));

            var edition = new CreateEditionDto
            {
                ModuleKeys = roots.SelectMany(section => section.Children)
                    .Where(page => page.CheckState == NodeCheckState.Checked)
                    .Select(page => page.Id).ToList(),
                Simplified = simplified,
                TargetFolder = folder
            };

            var service = services.GetRequiredService<IProgramEditionService>();
            using var handle = services.GetRequiredService<UI.Services.IDialogService>()
                .ShowProgress(LocalizationService.Get("Str.Builder.Export"),
                              LocalizationService.Get("Str.Builder.Creating"));

            var progress = new Progress<EditionProgress>(step => handle.Report(step.Percent, step.Stage));

            return await Task.Run(() => service.Create(edition, progress));
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
