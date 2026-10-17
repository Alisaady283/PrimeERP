using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Layout;
using PrimeERP.UI.Services;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>تصيير صفحة القائمة وCRUD</summary>
    public static class CrudPageRenderer
    {
        /// <summary>search: نصّ بحثٍ تبدأ به القائمة (التفصيل من لوحة التحليل)</summary>
        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services, string search = null)
        {
            dynamic vm = Resolve.ViewModel(definition, services);

            var configured = services.GetRequiredService<PrimeERP.Platform.Settings.ISettingsProvider>()
                .Get(PrimeERP.Platform.Settings.SettingKeys.UI.PageSize, 25);
            if (configured > 0 && vm.PageSize < 1000) vm.PageSize = configured;

            var header = new PageHeader();
            ICommand addCommand = definition.SingleRecord
                ? new PrimeERP.UI.ViewModels.RelayCommand(
                    _ => vm.AddCommand.Execute(null),
                    _ => !((System.Collections.IEnumerable)vm.Items).Cast<object>().Any(row => DialogFor(definition, row) == definition.DocumentDialog))
                : (ICommand)vm.AddCommand;

            var actions = new List<ToolbarAction>
            {
                ToolbarAction.New(addCommand, $"{definition.PagePermission}.Create"),
                ToolbarAction.Edit((ICommand)vm.EditCommand, $"{definition.PagePermission}.Edit"),
                ToolbarAction.Delete((ICommand)vm.DeleteCommand, $"{definition.PagePermission}.Delete"),
                ToolbarAction.Refresh((ICommand)vm.RefreshCommand),
            };

            if (definition.Reorderable)
            {
                var edit = $"{definition.PagePermission}.Edit";
                actions.Insert(3, ToolbarAction.MoveUp((ICommand)vm.MoveUpCommand, edit));
                actions.Insert(4, ToolbarAction.MoveDown((ICommand)vm.MoveDownCommand, edit));
            }

            var view = $"{definition.PagePermission}.View";
            actions.Add(ToolbarAction.Print(new PrimeERP.UI.ViewModels.RelayCommand(
                _ => PrintList(definition, services, vm)), view, LocalizationService.Get("Str.Output.PrintReport")));
            actions.Add(ToolbarAction.Export(new PrimeERP.UI.ViewModels.RelayCommand(
                _ => ExportGrid(definition, services, vm)), view, LocalizationService.Get("Str.Output.ExportReport")));

            foreach (var rowAction in definition.RowActions ?? new List<RowAction>())
            {
                var captured = rowAction;
                var command = new PrimeERP.UI.ViewModels.RelayCommand(
                    _ =>
                    {
                        var item = vm.SelectedItem as object;
                        if (item == null && captured.RequiresSelection) return;

                        var outcome = captured.Execute(services, item);
                        var toastService = services.GetRequiredService<IToastService>();

                        if (outcome.IsFailure) { toastService.Error(outcome.ErrorMessage); return; }

                        toastService.Success(LocalizationService.Get("Str.Success"));
                        vm.RefreshCommand.Execute(null);
                    },
                    _ => (!captured.RequiresSelection || vm.SelectedItem != null)
                         && (captured.AppliesTo?.Invoke(vm.SelectedItem as object) ?? true));

                actions.Add(ToolbarAction.Build(captured.Label, captured.Label, null, captured.Variant, command, definition.PageKeyOf(captured.PermissionKey), null, captured.Label));
            }

            header.ActionsContent = new ActionToolbar { ButtonsSource = ToolbarActions.Enabled(definition, actions) };

            var filterBar = new FilterBar { SearchPlaceholder = LocalizationService.Get("Str.Search") };
            BindingOperations.SetBinding(filterBar, FilterBar.ResultCountProperty, new Binding("TotalCount"));
            filterBar.Search += (_, text) => { vm.SearchText = text; vm.SearchCommand.Execute(null); };

            if (!string.IsNullOrEmpty(search))
            {
                vm.SearchText = search;
                filterBar.SearchText = search;
            }

            if (definition.Filters is { Count: > 0 })
                filterBar.FiltersContent = FilterControls.Build(definition.Filters, vm, services);

            var grid = new AppDataGrid
            { ColumnsSource = definition.Columns, ShowRowActions = false, ShowPagination = false, Margin = new Thickness(0, 12, 0, 0) };
            BindingOperations.SetBinding(grid, AppDataGrid.ItemsSourceProperty, new Binding("Items"));
            BindingOperations.SetBinding(grid, AppDataGrid.SelectedItemProperty, new Binding("SelectedItem") { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, AppDataGrid.IsLoadingProperty, new Binding("IsLoading"));
            grid.RowEditRequested += (_, row) => { vm.SelectedItem = (dynamic)row; vm.EditCommand.Execute(null); };
            grid.RowDeleteRequested += (_, row) => { vm.SelectedItem = (dynamic)row; vm.DeleteCommand.Execute(null); };

            if (definition.Dialog != null)
            {
                var toast = services.GetRequiredService<IToastService>();
                vm.AddRequested += (Action)(() =>
                {
                    if (DialogRenderer.ShowAndSave(definition.Dialog, services, toast))
                        vm.LoadCommand.Execute(null);
                });
                vm.EditRequested += (Action<object>)(item =>
                {
                    if (DialogRenderer.ShowAndSave(definition.Dialog, services, toast, item))
                        vm.LoadCommand.Execute(null);
                });
            }
            else if (definition.DocumentDialog != null)
            {
                var toast = services.GetRequiredService<IToastService>();
                vm.AddRequested += (Action)(() =>
                {
                    if (DocumentRenderer.ShowAndSave(definition.DocumentDialog, services, toast))
                        vm.LoadCommand.Execute(null);
                });
                vm.EditRequested += (Action<object>)(item =>
                {
                    // طباعة المستند وتصديره من داخله بعد حفظه — كالويب، فلا يأخذان مساحةً في شريط الصفحة
                    if (DocumentRenderer.ShowAndSave(DialogFor(definition, item), services, toast, item,
                            print: () => DocumentPrinter.PrintSelected(definition, services, item),
                            export: () => DocumentPrinter.ExportSelected(definition, services, item)))
                        vm.LoadCommand.Execute(null);
                });
            }

            var pagination = PaginationBar.For(vm, new Thickness(0, 8, 0, 0));
            AppGallery gallery = definition.Gallery == null ? null : Gallery(definition, (object)vm, grid, filterBar);

            var root = new Grid { DataContext = vm };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetRow(header, 0);
            Grid.SetRow(filterBar, 1);
            Grid.SetRow(grid, 2);
            Grid.SetRow(pagination, 3);
            root.Children.Add(header);
            root.Children.Add(filterBar);
            root.Children.Add(grid);
            if (gallery != null)
            {
                Grid.SetRow(gallery, 2);
                root.Children.Add(gallery);
            }
            root.Children.Add(pagination);

            root.Loaded += async (_, __) => await (Task)vm.LoadAsync();

            return root;
        }

        /// <summary>طريقة العرض المختارة لكل صفحة — تبقى طوال الجلسة</summary>
        private static readonly Dictionary<string, GalleryView> Views = new();

        /// <summary>
        /// المربّعات بجانب الجدول لصفحةٍ أعلنت Gallery: زرّا العرض في شريط التصفية، والمربّعات من صفوف الصفحة نفسها،
        /// والنقر على المربّع يفتح تعديله كالنقر المزدوج على الصفّ.
        /// </summary>
        private static AppGallery Gallery(ModuleDefinition definition, object model, FrameworkElement grid, FilterBar filterBar)
        {
            dynamic vm = model;
            var spec = definition.Gallery;
            var gallery = new AppGallery { Margin = new Thickness(0, 12, 0, 0) };
            var tiles = new AppIconButton { Icon = (System.Windows.Media.Geometry)System.Windows.Application.Current.FindResource("IconGrid"), TooltipText = LocalizationService.Get("Str.View.Tiles") };
            var details = new AppIconButton { Icon = (System.Windows.Media.Geometry)System.Windows.Application.Current.FindResource("IconList"), TooltipText = LocalizationService.Get("Str.View.Details") };

            object Read(object row, string property) => property == null ? null : row.GetType().GetProperty(property)?.GetValue(row);
            string Text(object value, string format) => value is IFormattable number && format != null ? number.ToString(format, null) : value?.ToString();

            void Fill() => gallery.Items = Rows(model).Select(row => new GalleryItem(row,
                Text(Read(row, spec.TitleProperty), null), Text(Read(row, spec.HintProperty), null),
                Text(Read(row, spec.ValueProperty), spec.ValueFormat), Read(row, spec.ImageProperty) as string)).ToList();

            void Show(GalleryView view)
            {
                Views[definition.Key] = view;
                gallery.View = view;
                gallery.Visibility = view == GalleryView.Tiles ? Visibility.Visible : Visibility.Collapsed;
                grid.Visibility = view == GalleryView.Tiles ? Visibility.Collapsed : Visibility.Visible;
                tiles.Variant = view == GalleryView.Tiles ? "brand" : "ghost";
                details.Variant = view == GalleryView.Details ? "brand" : "ghost";
                if (view == GalleryView.Tiles) Fill();
            }

            // الصفوف تتبدّل بالبحث والتصفية والصفحات: المربّعات تتبعها
            System.Collections.Specialized.INotifyCollectionChanged watched = null;
            void Watch()
            {
                if (watched != null) watched.CollectionChanged -= Changed;
                watched = (object)vm.Items as System.Collections.Specialized.INotifyCollectionChanged;
                if (watched != null) watched.CollectionChanged += Changed;
            }
            void Changed(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { if (gallery.Visibility == Visibility.Visible) Fill(); }
            ((System.ComponentModel.INotifyPropertyChanged)model).PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != "Items") return;
                Watch();
                if (gallery.Visibility == Visibility.Visible) Fill();
            };
            Watch();

            // نقرةٌ تحدّد (فيعمل التعديل والحذف من الشريط على المحدَّد)، ونقرتان تفتحان التعديل — كصفّ الجدول
            gallery.ItemClick += (_, item) => vm.SelectedItem = (dynamic)item.Key;
            gallery.ItemOpen += (_, item) => { vm.SelectedItem = (dynamic)item.Key; vm.EditCommand.Execute(null); };
            tiles.Click += (_, __) => Show(GalleryView.Tiles);
            details.Click += (_, __) => Show(GalleryView.Details);
            filterBar.ActionsContent = new StackPanel { Orientation = Orientation.Horizontal, Children = { tiles, details } };
            Show(Views.GetValueOrDefault(definition.Key, GalleryView.Tiles));
            return gallery;
        }

        private static void PrintList(ModuleDefinition definition, IServiceProvider services, dynamic vm) =>
            ListOutput.Print(services, LocalizationService.Get(definition.TitleKey), definition.Columns, Rows(vm));

        private static void ExportGrid(ModuleDefinition definition, IServiceProvider services, dynamic vm) =>
            ListOutput.Export(services, LocalizationService.Get(definition.TitleKey), definition.Columns, Rows(vm));

        private static List<object> Rows(dynamic vm) =>
            ((System.Collections.IEnumerable)vm.Items).Cast<object>().ToList();

        /// <summary>محرّر الصفّ بنوعه</summary>
        private static DocumentDialogDefinition DialogFor(ModuleDefinition definition, object row)
        {
            var kind = definition.RowKindProperty == null ? null
                : row?.GetType().GetProperty(definition.RowKindProperty)?.GetValue(row) as string;
            return definition.KindDialogs?.FirstOrDefault(d => d.DocumentKind == kind) ?? definition.DocumentDialog;
        }
    }
}
