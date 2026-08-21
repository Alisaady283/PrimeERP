using System.Windows.Controls;
using System.Windows.Input;
using PrimeERP.ViewModels;
using PrimeERP.Views.Dialogs;

namespace PrimeERP.Views.Pages
{
    public partial class AccountsPage : UserControl
    {
        public AccountsPage()
        {
            InitializeComponent();

            pagination.PageChanged     += Pagination_PageChanged;
            pagination.PageSizeChanged += Pagination_PageSizeChanged;

            this.Loaded += (s, e) =>
            {
                if (DataContext is AccountsViewModel vm)
                {
                    vm.OnDataLoaded += count =>
                        pagination.TotalItems = count;
                    vm.LoadPage(1, pagination.PageSize);
                }
            };
        }

        private void Pagination_PageChanged(object sender, int page)
        {
            if (DataContext is AccountsViewModel vm)
                vm.LoadPage(page, pagination.PageSize);
        }

        private void Pagination_PageSizeChanged(object sender, int size)
        {
            if (DataContext is AccountsViewModel vm)
                vm.LoadPage(1, size);
        }

        private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is AccountsViewModel vm &&
                vm.SelectedAccount != null &&
                !vm.SelectedAccount.IsLeaf)
            {
                var dlg = new AccountDialog(vm.SelectedAccount.Code,
                                            editMode: true);
                if (dlg.ShowDialog() == true)
                    vm.LoadPage(1, pagination.PageSize);
            }
        }
    }
}