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
            dynamic vm = Resolve.ViewModel(definition, services);

            // حجم الصفحة من الإعدادات وحدها — لا رقم في نموذج العرض ولا في شريط الترقيم.
            var configured = services.GetRequiredService<PrimeERP.Platform.Settings.ISettingsProvider>()
                .Get(PrimeERP.Platform.Settings.SettingKeys.UI.PageSize, 25);
            if (configured > 0 && vm.PageSize < 1000) vm.PageSize = configured;

            // بلا Title — AppShell.TopBar يعرض عنوان الصفحة تلقائياً من NavItem المختار (breadcrumb)؛ تكراره
            // هنا ظهر فعلياً كنص مكرر حرفياً عند أول تشغيل حقيقي (راجع توقف 10). PageHeader هنا لاستضافة زر
            // الإضافة فقط.
            var header = new PageHeader();
            // سجلّ واحد: الإضافة تُعطَّل بعد أوّله — التعديل والحذف يبقيان بصلاحياتهما لمن يملكها.
            ICommand addCommand = definition.SingleRecord
                ? new PrimeERP.UI.ViewModels.RelayCommand(
                    _ => vm.AddCommand.Execute(null),
                    _ => ((System.Collections.IEnumerable)vm.Items).Cast<object>().Any() == false)
                : (ICommand)vm.AddCommand;

            var actions = new List<ToolbarAction>
            {
                ToolbarAction.New(addCommand, $"{definition.PermissionPrefix}.Create"),
                ToolbarAction.Edit((ICommand)vm.EditCommand, $"{definition.PermissionPrefix}.Edit"),
                ToolbarAction.Delete((ICommand)vm.DeleteCommand, $"{definition.PermissionPrefix}.Delete"),
                ToolbarAction.Refresh((ICommand)vm.RefreshCommand),
            };

            if (definition.Reorderable)
            {
                var edit = $"{definition.PermissionPrefix}.Edit";
                actions.Insert(3, ToolbarAction.MoveUp((ICommand)vm.MoveUpCommand, edit));
                actions.Insert(4, ToolbarAction.MoveDown((ICommand)vm.MoveDownCommand, edit));
            }

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
                        if (item == null && captured.RequiresSelection) return;

                        var outcome = captured.Execute(services, item);
                        var toastService = services.GetRequiredService<IToastService>();

                        if (outcome.IsFailure) { toastService.Error(outcome.ErrorMessage); return; }

                        toastService.Success(LocalizationService.Get("Str.Success"));
                        vm.RefreshCommand.Execute(null);
                    },
                    _ => (!captured.RequiresSelection || vm.SelectedItem != null)
                         && (captured.AppliesTo?.Invoke(vm.SelectedItem as object) ?? true));

                actions.Add(ToolbarAction.Build(captured.Label, captured.Label, null, captured.Variant, command, captured.PermissionKey, null, captured.Label));
            }

            header.ActionsContent = new ActionToolbar { ButtonsSource = ToolbarActions.Enabled(definition, actions) };

            var filterBar = new FilterBar { SearchPlaceholder = LocalizationService.Get("Str.Search") };
            BindingOperations.SetBinding(filterBar, FilterBar.ResultCountProperty, new Binding("TotalCount"));
            filterBar.Search += (_, text) => { vm.SearchText = text; vm.SearchCommand.Execute(null); };

            if (definition.Filters is { Count: > 0 })
                filterBar.FiltersContent = FilterControls.Build(definition.Filters, vm, services);

            if (documentActions != null)
                filterBar.ActionsContent = new ActionToolbar { ButtonsSource = ToolbarActions.Enabled(definition, documentActions) };

            // ShowPagination=false — ترقيم AppDataGrid الداخلي جانب العميل (يُقسِّم القائمة الكاملة محلياً)
            // يتعارض مع الترقيم الحقيقي من طرف الخادم هنا (كل صفحة تُجلَب من GetPaged عند الطلب فقط، لا
            // القائمة كاملة أبداً في الذاكرة) — AppPagination أدناه هي المرجع الوحيد. اكتُشف التكرار البصري
            // فعلياً عند أول تشغيل حقيقي (تسجيل دخول + AppShell) — راجع توقف 10 في ARCHITECTURE.md.
            // بطاقة الجدول تبدأ من رأسه لا من شريط الإجراءات فوقه — الشريط هيكل، والجدول بيانات.
            var grid = new AppDataGrid
            // بلا عمود إجراءات: تعديل وحذف في الشريط، فعمودٌ ثالث لهما تكرارٌ يأكل عرضاً ويزيد ضجيجاً.
            { ColumnsSource = definition.Columns, ShowRowActions = false, ShowPagination = false, Margin = new Thickness(0, 12, 0, 0) };
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
            var pagination = new AppPagination { Margin = new Thickness(0, 8, 0, 0) };
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

        /// <summary>الطباعة والتصدير من ListOutput — القائمة والتقرير يستوردان نفس القطعة.</summary>
        private static void PrintList(ModuleDefinition definition, IServiceProvider services, dynamic vm) =>
            ListOutput.Print(services, LocalizationService.Get(definition.TitleKey), definition.Columns, Rows(vm));

        private static void ExportGrid(ModuleDefinition definition, IServiceProvider services, dynamic vm) =>
            ListOutput.Export(services, LocalizationService.Get(definition.TitleKey), definition.Columns, Rows(vm));

        private static List<object> Rows(dynamic vm) =>
            ((System.Collections.IEnumerable)vm.Items).Cast<object>().ToList();
    }
}
