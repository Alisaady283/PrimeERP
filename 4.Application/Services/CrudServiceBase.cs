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
    /// <summary>القراءة العامة (GetById/GetPaged/Search) لكيان له شكل صفحات + بحث. الإنشاء/التعديل/الحذف
    /// تبقى في الخدمة الفعلية أو في قاعدة أضيق (PartyServiceBase) — منطق العمل فيها مختلف بين كل كيان
    /// لدرجة أن قالباً عاماً واحداً يخفي المنطق بدل أن يلخّصه.</summary>
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
            if (!Can("View")) return Fail<TDto>("PermissionDenied", ErrorCode.Unauthorized);

            var entity = FindById(id);
            if (entity == null) return Fail<TDto>("NotFound", ErrorCode.NotFound);

            return Ok(ToDto(entity));
        }

        public virtual Result<PagedResult<TDto>> GetPaged(int page, int pageSize, TFilter filter)
        {
            if (!Can("View")) return Fail<PagedResult<TDto>>("PermissionDenied", ErrorCode.Unauthorized);

            var (items, total) = FindPaged(page, pageSize, filter);
            return Ok(new PagedResult<TDto> { Items = items.Select(ToDto).ToList(), TotalCount = total, Page = page, PageSize = pageSize });
        }

        public virtual Result<List<TDto>> Search(string term, int maxResults = 50)
        {
            if (!Can("View")) return Fail<List<TDto>>("PermissionDenied", ErrorCode.Unauthorized);

            return Ok(FindSearch(term ?? "", maxResults).Select(ToDto).ToList());
        }
    }
}
