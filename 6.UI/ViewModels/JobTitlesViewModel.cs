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

        protected override Result<PagedResult<JobTitleDto>> FetchPage(int page, int pageSize, JobTitleFilter filter) =>
            AllRows(_jobTitles.GetAll(), j => j.Name);

        protected override int IdOf(JobTitleDto item) => item.Id;
        protected override Result DeleteItem(int id) => _jobTitles.Delete(id);
    }
}
