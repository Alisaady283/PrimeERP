using System.Linq;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Services.HR;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class JobTitlesViewModel : CrudViewModelBase<JobTitleDto, JobTitleFilter>
    {
        private readonly IJobTitleService _jobTitles;

        public JobTitlesViewModel(IJobTitleService jobTitles, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _jobTitles = jobTitles;

        protected override string PermissionPrefix => "JobTitles";

        protected override Result<PagedResult<JobTitleDto>> FetchPage(int page, int pageSize, JobTitleFilter filter)
        {
            var result = _jobTitles.GetAll();
            if (!result.IsSuccess) return Result.Fail<PagedResult<JobTitleDto>>(result.ErrorMessage);

            var items = result.Value;
            if (!string.IsNullOrWhiteSpace(SearchText))
                items = items.Where(j => j.Name.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase)).ToList();

            return Result.Ok(new PagedResult<JobTitleDto> { Items = items, Page = 1, PageSize = items.Count, TotalCount = items.Count });
        }

        protected override int IdOf(JobTitleDto item) => item.Id;
        protected override Result DeleteItem(int id) => _jobTitles.Delete(id);
    }
}
