using System.Linq;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Services.HR;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    // بنفس فكرة CategoryListViewModel — GetAll بلا صفحات حقيقية، تُغلَّف بصفحة واحدة لتطابق FetchPage.
    public class DepartmentsViewModel : CrudViewModelBase<DepartmentDto, DepartmentFilter>
    {
        private readonly IDepartmentService _departments;

        public DepartmentsViewModel(IDepartmentService departments, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _departments = departments;

        protected override string PermissionPrefix => "Departments";

        protected override Result<PagedResult<DepartmentDto>> FetchPage(int page, int pageSize, DepartmentFilter filter)
        {
            var result = _departments.GetAll();
            if (!result.IsSuccess) return Result.Fail<PagedResult<DepartmentDto>>(result.ErrorMessage);

            var items = result.Value;
            if (!string.IsNullOrWhiteSpace(SearchText))
                items = items.Where(d => d.Name.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)).ToList();

            return Result.Ok(new PagedResult<DepartmentDto> { Items = items, Page = 1, PageSize = items.Count, TotalCount = items.Count });
        }

        protected override int IdOf(DepartmentDto item) => item.Id;
        protected override Result DeleteItem(int id) => _departments.Delete(id);
    }
}
