using System;
using System.Linq;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.Services.Common;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class CategoryFilter { }

    // يخدم كل الوحدات "المسطّحة القائمة على ModuleKey" (فروع، ماركات، وحدات، مخازن، أقسام، وظائف،
    // فئات أصول...) بلا تكرار خدمة/ViewModel — الفرق الوحيد بين وحدة وأخرى هو moduleKey/permissionPrefix،
    // يُثبَّتان في مُنشئ وحدة فرعية سطر واحد (راجع LookupViewModels.cs).
    public class CategoryListViewModel : CrudViewModelBase<CategoryDto, CategoryFilter>
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

        protected override Result<PagedResult<CategoryDto>> FetchPage(int page, int pageSize, CategoryFilter filter) =>
            AllRows(_categories.GetAll(_moduleKey), c => c.Name);

        protected override int IdOf(CategoryDto item) => item.Id;
        protected override Result DeleteItem(int id) => _categories.Delete(id);
    }
}
