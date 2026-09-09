using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Builder;
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
            foreach (var key in new[] { "BuilderSections", "BuilderModules", "BuilderColumns", "BuilderActions", "BuilderFilters" })
                PermissionKeys.RegisterBuilt(key);

            Section(registry);
            Module(registry);
            Column(registry);
            Action(registry);
            Filter(registry);
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
