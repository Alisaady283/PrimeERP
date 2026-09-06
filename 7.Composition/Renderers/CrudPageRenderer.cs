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

            // المستوى الأول: القائمة كتقرير — طباعة وتصدير، بلا حاجة لتحديد سجل.
            var view = $"{definition.PermissionPrefix}.View";
            actions.Add(ToolbarAction.Print(new PrimeERP.UI.ViewModels.RelayCommand(
                _ => PrintList(definition, services, vm)), view, "طباعة التقرير"));
            actions.Add(ToolbarAction.Export(new PrimeERP.UI.ViewModels.RelayCommand(
                _ => ExportGrid(definition, services, vm)), view, "تصدير التقرير"));

            // المستوى الثاني يعيش في صفّ الفلترة أدناه، لا هنا — إجراءات الصفحة وإجراءات المستند صفّان لا صفّ.
            var documentActions = definition.DocumentDialog == null ? null : new List<ToolbarAction>
            {
                ToolbarAction.Print(new PrimeERP.UI.ViewModels.RelayCommand(
                    _ => DocumentPrinter.PrintSelected(definition, services, vm.SelectedItem as object),
                    _ => vm.SelectedItem != null), view, "طباعة المستند"),
                ToolbarAction.Export(new PrimeERP.UI.ViewModels.RelayCommand(
                    _ => DocumentPrinter.ExportSelected(definition, services, vm.SelectedItem as object),
                    _ => vm.SelectedItem != null), view, "تصدير المستند"),
            };

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

            if (documentActions != null)
                filterBar.ActionsContent = new ActionToolbar { ButtonsSource = documentActions };

            // ShowPagination=false — ترقيم AppDataGrid الداخلي جانب العميل (يُقسِّم القائمة الكاملة محلياً)
            // يتعارض مع الترقيم الحقيقي من طرف الخادم هنا (كل صفحة تُجلَب من GetPaged عند الطلب فقط، لا
            // القائمة كاملة أبداً في الذاكرة) — AppPagination أدناه هي المرجع الوحيد. اكتُشف التكرار البصري
            // فعلياً عند أول تشغيل حقيقي (تسجيل دخول + AppShell) — راجع توقف 10 في ARCHITECTURE.md.
            // بطاقة الجدول تبدأ من رأسه لا من شريط الإجراءات فوقه — الشريط هيكل، والجدول بيانات.
            var grid = new AppDataGrid
            { ColumnsSource = definition.Columns, ShowRowActions = true, ShowPagination = false, Margin = new Thickness(0, 12, 0, 0) };
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

            // حشو متساوٍ حول الصفحة يأتي من AppShell؛ هنا الفصل بين الجدول وشريط الترقيم وحده.
            var pagination = new AppPagination { Margin = new Thickness(0, 12, 0, 0) };
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

        /// <summary>القائمة المعروضة كتقرير مطبوع — نفس أعمدة الشبكة، بترويسة الشركة وتذييلها.</summary>
        private static void PrintList(ModuleDefinition definition, IServiceProvider services, dynamic vm)
        {
            var toast = services.GetRequiredService<IToastService>();
            var items = ((System.Collections.IEnumerable)vm.Items).Cast<object>().ToList();
            if (items.Count == 0) { toast.Info("لا بيانات للطباعة"); return; }

            var report = new ReportResult
            {
                Title = LocalizationService.Get(definition.TitleKey),
                Columns = definition.Columns,
                Rows = items
            };

            var orientation = definition.Columns.Count > 6
                ? PrimeERP.Domain.Contracts.PrintOrientation.Landscape
                : PrimeERP.Domain.Contracts.PrintOrientation.Portrait;

            var printed = services.GetRequiredService<PrimeERP.Application.Services.Print.IPrintService>()
                .PrintPreview(Print.PrintDocuments.Report(report, orientation));

            if (printed.IsFailure) toast.Error(printed.ErrorMessage);
        }

        // صيغة واحدة تُختار من امتداد الملف — نافذة الحفظ نفسها هي القائمة، بلا حوار صيغ إضافي.
        private static void ExportGrid(ModuleDefinition definition, IServiceProvider services, dynamic vm)
        {
            var toast = services.GetRequiredService<IToastService>();
            var items = ((System.Collections.IEnumerable)vm.Items).Cast<object>().ToList();
            if (items.Count == 0) { toast.Info("لا بيانات للتصدير"); return; }

            var title = LocalizationService.Get(definition.TitleKey);
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"{title}-{DateTime.Today:yyyy-MM-dd}",
                Filter = "Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv|PDF (*.pdf)|*.pdf"
            };
            if (dialog.ShowDialog() != true) return;

            var export = services.GetRequiredService<IExportService>();
            try
            {
                switch (System.IO.Path.GetExtension(dialog.FileName).ToLowerInvariant())
                {
                    case ".csv":  export.ExportToCsv(items, definition.Columns, dialog.FileName); break;
                    case ".pdf":  export.ExportToPdf(items, definition.Columns, dialog.FileName, title); break;
                    default:      export.ExportToExcel(items, definition.Columns, dialog.FileName); break;
                }

                toast.Success($"تم التصدير إلى {System.IO.Path.GetFileName(dialog.FileName)}");
            }
            catch (Exception ex) { toast.Error($"تعذّر التصدير: {ex.Message}"); }
        }
    }
}
