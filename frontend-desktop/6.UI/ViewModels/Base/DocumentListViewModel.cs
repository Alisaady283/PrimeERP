using System;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;

namespace PrimeERP.UI.ViewModels.Base
{
    public sealed class DocumentListViewModel<TDto, TFilter> : CrudViewModelBase<TDto, TFilter> where TFilter : new()
    {
        private readonly string _prefix;
        private readonly Func<int, int, TFilter, Result<PagedResult<TDto>>> _page;
        private readonly Func<int, Result> _delete;
        private readonly Func<TDto, int> _id;
        private readonly Action<TFilter, string> _search;

        public DocumentListViewModel(string prefix, Func<int, int, TFilter, Result<PagedResult<TDto>>> page, Func<int, Result> delete,
            Func<TDto, int> id, Action<TFilter, string> search, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs)
        {
            _prefix = prefix; _page = page; _delete = delete; _id = id; _search = search;
        }

        protected override string PermissionPrefix => _prefix;

        protected override Result<PagedResult<TDto>> FetchPage(int page, int pageSize, TFilter filter)
        {
            var current = filter ?? new TFilter();
            _search(current, SearchText);
            return _page(page, pageSize, current);
        }

        protected override int IdOf(TDto item) => _id(item);
        protected override Result DeleteItem(int id) => _delete(id);
    }
}
