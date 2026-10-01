using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Core
{
    /// <summary>القراءة العامة لكيان بصفحات وبحث</summary>
    public abstract class CrudServiceBase<TEntity, TDto, TFilter> : ServiceBase where TEntity : class, IEntity
    {
        protected abstract TEntity FindById(int id);
        protected abstract (List<TEntity> Items, int Total) FindPaged(int page, int pageSize, TFilter filter);
        protected abstract List<TEntity> FindSearch(string term, int maxResults);
        protected abstract TDto ToDto(TEntity entity);

        /// <summary>صلاحية الفعل على السجل</summary>
        protected virtual bool CanOn(TEntity entity, string action) => Can(action);

        /// <summary>الصفحة كلها بضمّةٍ واحدة</summary>
        protected virtual List<TDto> ToDtos(List<TEntity> entities) => entities.Select(ToDto).ToList();

        protected CrudServiceBase(IPermissionService permissions, ISettingsProvider settings,
                                   ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit) { }

        public virtual Result<TDto> GetById(int id)
        {
            var entity = FindById(id);
            if (entity == null) return Can("View") ? Fail<TDto>("NotFound", ErrorCode.NotFound) : FailDenied<TDto>();

            return CanOn(entity, "View") ? Ok(ToDto(entity)) : FailDenied<TDto>();
        }

        public virtual Result<PagedResult<TDto>> GetPaged(int page, int pageSize, TFilter filter)
        {
            if (!Can("View")) return FailDenied<PagedResult<TDto>>();

            var (items, total) = FindPaged(page, pageSize, filter);

            return Ok(Paged(items, total, page, pageSize, ToDtos));
        }

        public virtual Result<List<TDto>> Search(string term, int maxResults = 50)
        {
            if (!Can("View")) return FailDenied<List<TDto>>();

            return Ok(ToDtos(FindSearch(term ?? "", maxResults)));
        }
    }
}
