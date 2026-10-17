using System.Windows;
using System.Windows.Data;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>شريط الترقيم مربوطاً بنموذج العرض</summary>
    public static class PaginationBar
    {
        public static AppPagination For(dynamic vm, Thickness? margin = null)
        {
            var pagination = margin.HasValue ? new AppPagination { Margin = margin.Value } : new AppPagination();

            BindingOperations.SetBinding(pagination, AppPagination.TotalItemsProperty, new Binding("TotalCount"));
            BindingOperations.SetBinding(pagination, AppPagination.PageSizeProperty, new Binding("PageSize"));
            BindingOperations.SetBinding(pagination, AppPagination.CurrentPageProperty, new Binding("CurrentPage") { Mode = BindingMode.OneWay });
            pagination.PageChanged += (_, page) => vm.GoToPageCommand.Execute(page);

            return pagination;
        }
    }
}
