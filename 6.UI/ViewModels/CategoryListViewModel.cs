using System;
using System.Linq;
using PrimeERP.Application.Legacy.Common;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>نماذج عرض CategoryFilter</summary>
    public class CategoryFilter { }

    public class CategoryListViewModel : CrudViewModelBase<Category, CategoryFilter>
    {
        private readonly ICategoryService _categories;
        private readonly string _moduleKey;

        protected override string PermissionPrefix { get; }

        protected CategoryListViewModel(string moduleKey, string permissionPrefix, ICategoryService categories,
                                         IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs)
        {
            _categories = categories;
            _moduleKey = moduleKey;
            PermissionPrefix = permissionPrefix;
        }

        protected override Result<PagedResult<Category>> FetchPage(int page, int pageSize, CategoryFilter filter) =>
            AllRows(_categories.GetAll(_moduleKey), c => c.Name);

        protected override int IdOf(Category item) => item.Id;
        protected override Result DeleteItem(int id) => _categories.Delete(id);
    }
}
