using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.HR
{
    public class JobTitleService : ServiceBase, IJobTitleService
    {
        protected override string PermissionPrefix => "JobTitles";
        protected override string StringPrefix => "Str.JobTitle";
        protected override string EntityName => "JobTitles";

        private readonly IJobTitleRepository _repo;

        public JobTitleService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IJobTitleRepository repo) : base(permissions, settings, localization, audit) => _repo = repo;

        public Result<List<JobTitleDto>> GetAll(bool includeInactive = false) =>
            Result.Ok(_repo.GetAll(includeInactive).Select(ToDto).ToList());

        public Result<JobTitleDto> Create(CreateJobTitleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return Result.Fail<JobTitleDto>("اسم الوظيفة مطلوب", ErrorCode.ValidationFailed);

            var jobTitle = new JobTitle { Name = dto.Name, NameEn = dto.NameEn, IsActive = dto.IsActive };
            var id = _repo.Insert(jobTitle);
            jobTitle.Id = id;

            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { jobTitle.Name });
            return Result.Ok(ToDto(jobTitle));
        }

        public Result Update(UpdateJobTitleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return Result.Fail("اسم الوظيفة مطلوب", ErrorCode.ValidationFailed);

            var jobTitle = _repo.GetById(dto.Id);
            if (jobTitle == null) return Result.Fail("الوظيفة غير موجودة", ErrorCode.NotFound);

            jobTitle.Name = dto.Name; jobTitle.NameEn = dto.NameEn; jobTitle.IsActive = dto.IsActive;
            _repo.Update(jobTitle);
            Audit.Log(EntityName, jobTitle.Id, AuditAction.Update, newValue: new { jobTitle.Name });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            var jobTitle = _repo.GetById(id);
            if (jobTitle == null) return Result.Fail("الوظيفة غير موجودة", ErrorCode.NotFound);

            _repo.Delete(id);
            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private static JobTitleDto ToDto(JobTitle j) => new() { Id = j.Id, Name = j.Name, NameEn = j.NameEn, IsActive = j.IsActive };
    }
}
