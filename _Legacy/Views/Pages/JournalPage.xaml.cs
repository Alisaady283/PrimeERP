using System.Windows.Controls;
using System.Windows.Input;
using PrimeERP.ViewModels;
using PrimeERP.Views.Dialogs;

namespace PrimeERP.Views.Pages
{
    public partial class JournalPage : UserControl
    {
        public JournalPage()
        {
            InitializeComponent();

            pagination.PageChanged     += Pagination_PageChanged;
            pagination.PageSizeChanged += Pagination_PageSizeChanged;

            this.Loaded += (s, e) =>
            {
                if (DataContext is JournalViewModel vm)
                {
                    vm.OnDataLoaded += count =>
                        pagination.TotalItems = count;
                    vm.LoadPage(1, pagination.PageSize);
                }
            };
        }

        private void Pagination_PageChanged(object sender, int page)
        {
            if (DataContext is JournalViewModel vm)
                vm.LoadPage(page, pagination.PageSize);
        }

        private void Pagination_PageSizeChanged(object sender, int size)
        {
            if (DataContext is JournalViewModel vm)
                vm.LoadPage(1, size);
        }

        private void Grid_MouseDoubleClick(object sender,
                                            MouseButtonEventArgs e)
        {
            if (DataContext is JournalViewModel vm &&
                vm.SelectedEntry != null)
            {
                var dlg = new JournalDialog(vm.SelectedEntry.Id,
                                            viewOnly: true);
                dlg.ShowDialog();
            }
        }
    }
}