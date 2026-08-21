using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using PrimeERP.Database;
using PrimeERP.Models;
using PrimeERP.Views.Dialogs;

namespace PrimeERP.ViewModels
{
    public class JournalViewModel : BaseViewModel
    {
        private ObservableCollection<JournalEntry> _pagedEntries;
        private JournalEntry _selectedEntry;
        private string _searchText = "";
        private List<JournalEntry> _allData;
        private int _pageSize = 15;

        public ObservableCollection<JournalEntry> PagedEntries
        {
            get => _pagedEntries;
            set => SetProperty(ref _pagedEntries, value);
        }

        public JournalEntry SelectedEntry
        {
            get => _selectedEntry;
            set => SetProperty(ref _selectedEntry, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                LoadPage(1, _pageSize);
            }
        }

        public int    TotalCount  { get; private set; }
        public string TotalDebit  { get; private set; }
        public string TotalCredit { get; private set; }

        public ICommand AddCommand    { get; }
        public ICommand EditCommand   { get; }
        public ICommand ViewCommand   { get; }
        public ICommand DeleteCommand { get; }

        public Action<int> OnDataLoaded;

        public JournalViewModel()
        {
            JournalDb.CreateTable();

            AddCommand    = new RelayCommand(OnAdd);
            EditCommand   = new RelayCommand(OnEdit);
            ViewCommand   = new RelayCommand(OnView);
            DeleteCommand = new RelayCommand(OnDelete);
        }

        public void LoadPage(int page, int pageSize)
        {
            _pageSize = pageSize;
            _allData  = string.IsNullOrEmpty(SearchText)
                ? JournalDb.GetAll()
                : JournalDb.Search(SearchText);

            TotalCount = _allData.Count;
            OnPropertyChanged(nameof(TotalCount));
            OnDataLoaded?.Invoke(TotalCount);

            TotalDebit  = _allData.Sum(e => e.TotalDebit).ToString("N2");
            TotalCredit = _allData.Sum(e => e.TotalCredit).ToString("N2");
            OnPropertyChanged(nameof(TotalDebit));
            OnPropertyChanged(nameof(TotalCredit));

            var paged = _allData.Skip((page - 1) * pageSize).Take(pageSize);
            PagedEntries = new ObservableCollection<JournalEntry>(paged);
        }

        private void OnAdd(object _)
        {
            var dlg = new JournalDialog();
            if (dlg.ShowDialog() == true)
                LoadPage(1, _pageSize);
        }

        private void OnEdit(object _)
        {
            if (SelectedEntry == null) return;

            if (SelectedEntry.IsPosted)
            {
                System.Windows.MessageBox.Show(
                    "لا يمكن تعديل قيد مرحّل", "تنبيه",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var dlg = new JournalDialog(SelectedEntry.Id);
            if (dlg.ShowDialog() == true)
                LoadPage(1, _pageSize);
        }

        private void OnView(object _)
        {
            if (SelectedEntry == null) return;
            var dlg = new JournalDialog(SelectedEntry.Id, viewOnly: true);
            dlg.ShowDialog();
        }

        private void OnDelete(object _)
        {
            if (SelectedEntry == null) return;

            if (SelectedEntry.IsPosted)
            {
                System.Windows.MessageBox.Show(
                    "لا يمكن حذف قيد مرحّل", "تنبيه",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"حذف القيد {SelectedEntry.EntryNo}؟\nسيتم عكس الأرصدة.",
                "تأكيد الحذف",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                JournalDb.DeleteEntry(SelectedEntry.Id);
                LoadPage(1, _pageSize);
            }
        }
    }
}
