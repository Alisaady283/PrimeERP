using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Layout;

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

            var header = new PageHeader { Title = LocalizationService.Get(definition.TitleKey) };
            var addButton = new Button { Content = LocalizationService.Get("Str.Add") };
            BindingOperations.SetBinding(addButton, ButtonBase.CommandProperty, new Binding("AddCommand"));
            header.ActionsContent = addButton;

            var filterBar = new FilterBar { SearchPlaceholder = LocalizationService.Get("Str.Search") };
            BindingOperations.SetBinding(filterBar, FilterBar.ResultCountProperty, new Binding("TotalCount"));
            filterBar.Search += (_, text) => { vm.SearchText = text; vm.SearchCommand.Execute(null); };

            var grid = new AppDataGrid { ColumnsSource = definition.Columns, ShowRowActions = true };
            BindingOperations.SetBinding(grid, AppDataGrid.ItemsSourceProperty, new Binding("Items"));
            BindingOperations.SetBinding(grid, AppDataGrid.SelectedItemProperty, new Binding("SelectedItem") { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, AppDataGrid.IsLoadingProperty, new Binding("IsLoading"));
            grid.RowEditRequested += (_, row) => { vm.SelectedItem = row; vm.EditCommand.Execute(null); };
            grid.RowDeleteRequested += (_, row) => { vm.SelectedItem = row; vm.DeleteCommand.Execute(null); };

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
    }
}
