using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;

namespace PrimeERP.UI.ViewModels.Base
{
    /// <summary>صفحات+بحث عامان لأي كيان</summary>
    public abstract class PagedViewModelBase<TDto, TFilter> : PermissionAwareViewModel where TFilter : new()
    {
        protected abstract string PermissionPrefix { get; }
        protected readonly IToastService Toast;

        public ObservableCollection<TDto> Items { get; } = new();

        private TDto _selectedItem;
        public TDto SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

        private string _searchText = "";
        public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }

        private int _currentPage = 1;
        public int CurrentPage { get => _currentPage; private set => SetProperty(ref _currentPage, value); }

        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set { if (SetProperty(ref _pageSize, value)) OnPropertyChanged(nameof(TotalPages)); }
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            private set { if (SetProperty(ref _totalCount, value)) OnPropertyChanged(nameof(TotalPages)); }
        }

        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        private TFilter _filter = new();
        public TFilter Filter { get => _filter; set => SetProperty(ref _filter, value); }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set { if (SetProperty(ref _isLoading, value)) OnPropertyChanged(nameof(IsEmpty)); }
        }

        public bool IsEmpty => !IsLoading && Items.Count == 0;

        public ICommand LoadCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand GoToPageCommand { get; }

        protected PagedViewModelBase(IPermissionService permissions, IToastService toast) : base(permissions)
        {
            Toast = toast;
            LoadCommand = new RelayCommand(async () => await GoToPageAsync(1));
            RefreshCommand = new RelayCommand(async () => await GoToPageAsync(CurrentPage));
            SearchCommand = new RelayCommand(async () => await GoToPageAsync(1));
            NextPageCommand = new RelayCommand(async () => await GoToPageAsync(CurrentPage + 1), () => CurrentPage < TotalPages);
            PreviousPageCommand = new RelayCommand(async () => await GoToPageAsync(CurrentPage - 1), () => CurrentPage > 1);
            GoToPageCommand = new RelayCommand(async param => await GoToPageAsync(Convert.ToInt32(param)));
        }

        protected abstract Result<PagedResult<TDto>> FetchPage(int page, int pageSize, TFilter filter);

        protected Result<PagedResult<TDto>> AllRows(Result<List<TDto>> source, params Func<TDto, string>[] searched)
        {
            if (!source.IsSuccess) return Result.Fail<PagedResult<TDto>>(source.ErrorMessage);

            var items = string.IsNullOrWhiteSpace(SearchText)
                ? source.Value
                : source.Value.Where(row => searched.Any(field =>
                    (field(row) ?? "").Contains(SearchText, StringComparison.OrdinalIgnoreCase))).ToList();

            return Result.Ok(new PagedResult<TDto> { Items = items, Page = 1, PageSize = items.Count, TotalCount = items.Count });
        }

        public Task LoadAsync() => GoToPageAsync(1);

        private static readonly System.Reflection.PropertyInfo FilterSearchTextProp =
            typeof(TFilter).GetProperty("SearchText");

        protected async Task GoToPageAsync(int page)
        {
            if (!Can($"{PermissionPrefix}.View")) return;

            if (FilterSearchTextProp != null && FilterSearchTextProp.CanWrite)
                FilterSearchTextProp.SetValue(Filter, SearchText);

            IsLoading = true;
            try
            {
                var result = await Task.Run(() => FetchPage(page, PageSize, Filter));
                if (!result.IsSuccess)
                {
                    Toast.Error(result.ErrorMessage);
                    return;
                }

                Items.Clear();
                foreach (var item in result.Value.Items)
                    Items.Add(item);

                CurrentPage = result.Value.Page;
                TotalCount = result.Value.TotalCount;
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception ex)
            {
                // الاستثناء يُعرَض لا يُسقط
                Toast.Error(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
