using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using PrimeERP.Database;
using PrimeERP.Models;
using PrimeERP.Views.Dialogs;

namespace PrimeERP.ViewModels
{
    public class AccountsViewModel : BaseViewModel
    {
        private ObservableCollection<Account> _pagedAccounts;
        private Account _selectedAccount;
        private string _searchText = "";
        private int _levelFilter   = 0;
        private System.Collections.Generic.List<Account> _allData;
        private int _pageSize      = 15;

        public ObservableCollection<Account> PagedAccounts
        {
            get => _pagedAccounts;
            set => SetProperty(ref _pagedAccounts, value);
        }

        public Account SelectedAccount
        {
            get => _selectedAccount;
            set => SetProperty(ref _selectedAccount, value);
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

        public int TotalCount { get; private set; }

        public ICommand AddCommand    { get; }
        public ICommand EditCommand   { get; }
        public ICommand DeleteCommand { get; }
        public ICommand FilterCommand { get; }

        public Action<int> OnDataLoaded;

        public AccountsViewModel()
        {
            AccountDb.CreateTable();
            AccountDb.SeedDefaults();

            AddCommand    = new RelayCommand(OnAdd);
            EditCommand   = new RelayCommand(OnEdit);
            DeleteCommand = new RelayCommand(OnDelete);
            FilterCommand = new RelayCommand(param =>
            {
                if (param is string s && int.TryParse(s, out int lvl))
                {
                    _levelFilter = lvl;
                    LoadPage(1, _pageSize);
                }
            });
        }

        public void LoadPage(int page, int pageSize)
        {
            _pageSize = pageSize;
            _allData  = AccountDb.GetAll();

            var filtered = _allData.Where(a =>
            {
                if (!string.IsNullOrEmpty(SearchText))
                {
                    var s = SearchText.ToLower();
                    if (!a.Code.ToLower().Contains(s) &&
                        !a.Name.ToLower().Contains(s))
                        return false;
                }

                if (_levelFilter > 0 && a.Level != _levelFilter)
                    return false;

                return true;
            }).ToList();

            TotalCount = filtered.Count;
            OnPropertyChanged(nameof(TotalCount));
            OnDataLoaded?.Invoke(TotalCount);

            var paged = filtered.Skip((page - 1) * pageSize).Take(pageSize);
            PagedAccounts = new ObservableCollection<Account>(paged);
        }

        private void OnAdd(object _)
        {
            if (SelectedAccount == null)
            {
                System.Windows.MessageBox.Show(
                    "اختر الحساب الأب أولاً", "تنبيه",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (SelectedAccount.IsLeaf)
            {
                System.Windows.MessageBox.Show(
                    "هذا الحساب يقبل قيود — لا يمكن إضافة فرع تحته",
                    "تنبيه",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var dlg = new AccountDialog(SelectedAccount.Code);
            if (dlg.ShowDialog() == true)
                LoadPage(1, _pageSize);
        }

        private void OnEdit(object _)
        {
            if (SelectedAccount == null) return;
            var dlg = new AccountDialog(SelectedAccount.Code, editMode: true);
            if (dlg.ShowDialog() == true)
                LoadPage(1, _pageSize);
        }

        private void OnDelete(object _)
        {
            if (SelectedAccount == null) return;

            if (AccountDb.HasChildren(SelectedAccount.Code))
            {
                System.Windows.MessageBox.Show("لا يمكن حذف حساب له فروع", "تنبيه");
                return;
            }

            if (AccountDb.HasTransactions(SelectedAccount.Code))
            {
                System.Windows.MessageBox.Show("لا يمكن حذف حساب له قيود", "تنبيه");
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"حذف الحساب {SelectedAccount.Code}؟", "تأكيد",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                AccountDb.Delete(SelectedAccount.Code);
                LoadPage(1, _pageSize);
            }
        }
    }
}
