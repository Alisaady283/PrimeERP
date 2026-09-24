using System.Collections.Generic;
using PrimeERP.Application.Services;
using System.Linq;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services
{
    /// <summary>القراءة العامة لكيان بصفحات وبحث</summary>
    public abstract class CrudServiceBase<TEntity, TDto, TFilter> : ServiceBase where TEntity : BaseModel
    {
        protected abstract TEntity FindById(int id);
        protected abstract (List<TEntity> Items, int Total) FindPaged(int page, int pageSize, TFilter filter);
        protected abstract List<TEntity> FindSearch(string term, int maxResults);
        protected abstract TDto ToDto(TEntity entity);

        protected CrudServiceBase(IPermissionService permissions, ISettingsProvider settings,
                                   ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit) { }

        public virtual Result<TDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<TDto>();

            var entity = FindById(id);
            if (entity == null) return Fail<TDto>("NotFound", ErrorCode.NotFound);

            return Ok(ToDto(entity));
        }

        public virtual Result<PagedResult<TDto>> GetPaged(int page, int pageSize, TFilter filter)
        {
            if (!Can("View")) return FailDenied<PagedResult<TDto>>();

            var (items, total) = FindPaged(page, pageSize, filter);

            var rows = pageSize > 0 && items.Count > pageSize
                ? items.Skip((page < 1 ? 0 : page - 1) * pageSize).Take(pageSize).ToList()
                : items;

            return Ok(new PagedResult<TDto> { Items = rows.Select(ToDto).ToList(), TotalCount = total, Page = page, PageSize = pageSize });
        }

        public virtual Result<List<TDto>> Search(string term, int maxResults = 50)
        {
            if (!Can("View")) return FailDenied<List<TDto>>();

            return Ok(FindSearch(term ?? "", maxResults).Select(ToDto).ToList());
        }
    }
}
