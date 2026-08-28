using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;

namespace PrimeERP.UI.ViewModels.Base
{
    /// <summary>
    /// صفحات+بحث عامان لأي كيان — يعكس فقط ما هو موحّد فعلياً عبر كل الخدمات الثمانية
    /// (`CrudServiceBase.GetPaged(page,pageSize,filter)→Result&lt;PagedResult&lt;TDto&gt;&gt;`). لا يعمّم
    /// Create/Update عمداً — راجع مبدأ استخراج القطعة في ARCHITECTURE.md.
    ///
    /// SearchText مقصود بلا ربط تلقائي بـ TFilter (العام بلا شكل مضمون) — الوارث يقرأها داخل FetchPage
    /// ويدمجها مع Filter بنفسه (كيف يُدمَج نص البحث يختلف بين كيان وآخر — نفس اختبار "لا if لكل كيان").
    /// </summary>
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

        // مُهيَّأة دائماً — MasterListRenderer يكتب في خصائصها عبر Reflection (فلاتر إعلانية) بلا حاجة للتحقق من null.
        private TFilter _filter = new();
        public TFilter Filter { get => _filter; set => SetProperty(ref _filter, value); }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set { if (SetProperty(ref _isLoading, value)) OnPropertyChanged(nameof(IsEmpty)); }
        }

        /// <summary>صحيح فقط بعد اكتمال أول تحميل — يمنع وميض "لا نتائج" أثناء التحميل الأول.</summary>
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

        /// <summary>الخطاف الوحيد المطلوب من الوارث — استدعاء IXService.GetPaged الفعلي.</summary>
        protected abstract Result<PagedResult<TDto>> FetchPage(int page, int pageSize, TFilter filter);

        public Task LoadAsync() => GoToPageAsync(1);

        protected async Task GoToPageAsync(int page)
        {
            if (!Can($"{PermissionPrefix}.View")) return;

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
            finally
            {
                IsLoading = false;
            }
        }
    }
}
