using System.Collections.Generic;
using PrimeERP.Application.Services.Builder;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;
using System.Dynamic;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>
    /// نموذج عرض أي شاشة صفوفها قواميس — الجدول المبنيّ وشاشات الوصف معاً. يرث CrudViewModelBase كأي
    /// نموذج في النظام، والفرق أن خدمته تُمرَّر جاهزةً بوصفها وبادئةِ صلاحيتها بدل حقنها بالنوع.
    /// </summary>
    public class DynamicViewModel : CrudViewModelBase<IDictionary<string, object>, DynamicFilter>
    {
        private readonly IRowService _service;
        private readonly string _permissionPrefix;

        public DynamicViewModel(IRowService service, string permissionPrefix,
            IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs)
        {
            _service = service;
            _permissionPrefix = permissionPrefix;
        }

        protected override string PermissionPrefix => _permissionPrefix;

        protected override Result<PagedResult<IDictionary<string, object>>> FetchPage(int page, int pageSize, DynamicFilter filter)
        {
            filter ??= new DynamicFilter();
            filter.SearchText = SearchText;

            return _service.GetPaged(page, pageSize, filter);
        }

        protected override int IdOf(IDictionary<string, object> item) =>
            item.TryGetValue("Id", out var raw) && raw != null ? System.Convert.ToInt32(raw) : 0;

        protected override Result DeleteItem(int id) => _service.Delete(id);
    }
}
