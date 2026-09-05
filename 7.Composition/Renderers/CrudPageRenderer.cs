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
    /// <summary>
    /// يبني صفحة قائمة+CRUD كاملة من ModuleDefinition واحدة عبر تجميع القطع الجاهزة (PageHeader/FilterBar/
    /// AppDataGrid/AppPagination) — صفر XAML جديد لكل كيان، صفر منطق داخل Window/Page (القاعدة المعمارية
    /// الثابتة منذ البداية). الربط بخصائص CrudViewModelBase&lt;TDto,TFilter&gt; عبر Binding بالاسم (WPF
    /// ينعكس على أي نوع مغلَق فعلياً وقت التشغيل، لا يحتاج معرفة TDto/TFilter هنا) — عدا استدعاءات مباشرة
    /// قليلة (LoadAsync، أوامر البحث/الصفحات من مستمعي أحداث C#) تحتاج dynamic لنفس السبب.
    /// </summary>
    public static class CrudPageRenderer
    {
        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services)
        {
            dynamic vm = services.GetRequiredService(definition.ViewModelType);

            // بلا Title — AppShell.TopBar يعرض عنوان الصفحة تلقائياً من NavItem المختار (breadcrumb)؛ تكراره
            // هنا ظهر فعلياً كنص مكرر حرفياً عند أول تشغيل حقيقي (راجع توقف 10). PageHeader هنا لاستضافة زر
            // الإضافة فقط.
            var header = new PageHeader();
            var actions = new List<ToolbarAction>
            {
                ToolbarAction.New((ICommand)vm.AddCommand, $"{definition.PermissionPrefix}.Create"),
                ToolbarAction.Edit((ICommand)vm.EditCommand, $"{definition.PermissionPrefix}.Edit"),
                ToolbarAction.Delete((ICommand)vm.DeleteCommand, $"{definition.PermissionPrefix}.Delete"),
                ToolbarAction.Refresh((ICommand)vm.RefreshCommand),
            };

            // زر الطباعة يظهر فقط لوحدات المستندات — القوائم المجرّدة (أصناف/عملاء) لا ورق لها.
            if (definition.DocumentDialog != null)
                actions.Insert(3, ToolbarAction.Print(new PrimeERP.UI.ViewModels.RelayCommand(_ =>
                    DocumentPrinter.PrintSelected(definition, services, vm.SelectedItem as object)), $"{definition.PermissionPrefix}.View"));
            // إجراءات الوحدة المُعلَنة (ترحيل قيد، تحريك شيك…) — تعمل على السجل المحدَّد، وتُحدِّث الشبكة بعدها.
            foreach (var rowAction in definition.RowActions ?? new List<RowAction>())
            {
                var captured = rowAction;
                var command = new PrimeERP.UI.ViewModels.RelayCommand(
                    _ =>
                    {
                        var item = vm.SelectedItem as object;
                        if (item == null) return;

                        var outcome = captured.Execute(services, item);
                        var toastService = services.GetRequiredService<IToastService>();

                        if (outcome.IsFailure) { toastService.Error(outcome.ErrorMessage); return; }

                        toastService.Success(LocalizationService.Get("Str.Success"));
                        vm.RefreshCommand.Execute(null);
                    },
                    _ => vm.SelectedItem != null && (captured.AppliesTo?.Invoke(vm.SelectedItem as object) ?? true));

                actions.Add(ToolbarAction.Build(captured.Label, captured.Label, null, captured.Variant, command, captured.PermissionKey, null, captured.Label));
            }

            header.ActionsContent = new ActionToolbar { ButtonsSource = actions };

            var filterBar = new FilterBar { SearchPlaceholder = LocalizationService.Get("Str.Search") };
            BindingOperations.SetBinding(filterBar, FilterBar.ResultCountProperty, new Binding("TotalCount"));
            filterBar.Search += (_, text) => { vm.SearchText = text; vm.SearchCommand.Execute(null); };

            if (definition.Filters is { Count: > 0 })
                filterBar.FiltersContent = BuildFilterControls(definition.Filters, vm, services);

            // ShowPagination=false — ترقيم AppDataGrid الداخلي جانب العميل (يُقسِّم القائمة الكاملة محلياً)
            // يتعارض مع الترقيم الحقيقي من طرف الخادم هنا (كل صفحة تُجلَب من GetPaged عند الطلب فقط، لا
            // القائمة كاملة أبداً في الذاكرة) — AppPagination أدناه هي المرجع الوحيد. اكتُشف التكرار البصري
            // فعلياً عند أول تشغيل حقيقي (تسجيل دخول + AppShell) — راجع توقف 10 في ARCHITECTURE.md.
            var grid = new AppDataGrid { ColumnsSource = definition.Columns, ShowRowActions = true, ShowPagination = false };
            BindingOperations.SetBinding(grid, AppDataGrid.ItemsSourceProperty, new Binding("Items"));
            BindingOperations.SetBinding(grid, AppDataGrid.SelectedItemProperty, new Binding("SelectedItem") { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, AppDataGrid.IsLoadingProperty, new Binding("IsLoading"));
            grid.RowEditRequested += (_, row) => { vm.SelectedItem = row; vm.EditCommand.Execute(null); };
            grid.RowDeleteRequested += (_, row) => { vm.SelectedItem = row; vm.DeleteCommand.Execute(null); };

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
                    if (DocumentRenderer.ShowAndSave(definition.DocumentDialog, services, toast, item))
                        vm.LoadCommand.Execute(null);
                });
            }

            var pagination = new AppPagination();
            BindingOperations.SetBinding(pagination, AppPagination.TotalItemsProperty, new Binding("TotalCount"));
            BindingOperations.SetBinding(pagination, AppPagination.PageSizeProperty, new Binding("PageSize"));
            // OneWay صراحة — CurrentPage على الـVM للقراءة فقط (private set)، والتنقل الفعلي عبر PageChanged→GoToPageCommand
            // لا كتابة عكسية على الخاصية؛ AppPagination.CurrentPageProperty ثنائية الاتجاه افتراضياً (BindsTwoWayByDefault).
            BindingOperations.SetBinding(pagination, AppPagination.CurrentPageProperty, new Binding("CurrentPage") { Mode = BindingMode.OneWay });
            pagination.PageChanged += (_, page) => vm.GoToPageCommand.Execute(page);

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
            root.Children.Add(pagination);

            root.Loaded += async (_, __) => await (Task)vm.LoadAsync();

            return root;
        }

        private static FrameworkElement BuildFilterControls(System.Collections.Generic.List<FilterDefinition> filters, dynamic vm, IServiceProvider services)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var filter in filters)
            {
                var combo = new PrimeERP.UI.Components.Inputs.AppComboBox
                {
                    Placeholder = LocalizationService.Get(filter.LabelKey), Width = filter.Width,
                    DisplayMemberPath = "Display", SelectedValuePath = "Id", AllowClear = true,
                    Margin = new Thickness(0, 0, 8, 0)
                };

                if (filter.PickerType == "Category")
                {
                    var categoryService = services.GetRequiredService<PrimeERP.Application.Services.Common.ICategoryService>();
                    var result = categoryService.GetAll(filter.PickerCategoryModuleKey);
                    if (result.IsSuccess)
                        combo.ItemsSource = result.Value.Select(c => new { c.Id, Display = c.Name }).ToList();
                }
                else if (filter.PickerType == "Department")
                {
                    var result = services.GetRequiredService<PrimeERP.Application.Services.HR.IDepartmentService>().GetAll();
                    if (result.IsSuccess)
                        combo.ItemsSource = result.Value.Select(d => new { d.Id, Display = d.Name }).ToList();
                }

                combo.SelectionChanged += (_, __) =>
                {
                    object filterObj = vm.Filter;
                    filterObj.GetType().GetProperty(filter.Key)?.SetValue(filterObj, combo.SelectedValue);
                    vm.SearchCommand.Execute(null);
                };

                panel.Children.Add(combo);
            }
            return panel;
        }
    }
}
