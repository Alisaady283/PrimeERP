using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Services;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Application.Validation;
using System.Dynamic;

namespace PrimeERP.Application.Services.Builder
{
    /// <summary>مرشِّح أي وحدة مبنيّة — نفس شكل مرشِّحات النظام: بحث وفرز وترقيم.</summary>
    public class DynamicFilter
    {
        public string SearchText { get; set; }
        public string SortBy { get; set; }
        public bool   SortDescending { get; set; }

        /// <summary>ترشيح شاشات الوصف: القسم ثم صفحته — فارغ = الكل.</summary>
        public int?   SectionId { get; set; }
        public int?   ModuleId  { get; set; }
    }

    /// <summary>
    /// خدمة أي جدول بناه المستخدم. ترث ServiceBase كأي خدمة، فتأخذ منها الصلاحية ورسالتها والتدقيق
    /// والتحقق — والفرق أن بادئتها واسم كيانها من الوصف، وأن صفّها قاموسٌ لا كيان.
    /// تواقيعها هي التي ينتظرها CrudPageRenderer وDialogRenderer: GetPaged / GetById / Create / Update / Delete.
    /// </summary>
    public class DynamicEntityService : ServiceBase, IRowService
    {
        private readonly BuilderModule _module;
        private readonly List<BuilderColumn> _columns;
        private readonly DynamicRepository _repo;

        public DynamicEntityService(BuilderModule module, List<BuilderColumn> columns,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _module = module;
            _columns = columns;
            _repo = new DynamicRepository(module.TableName, columns);
        }

        protected override string PermissionPrefix => _module.Key;
        protected override string StringPrefix => "Str.Builder";
        protected override string EntityName => _module.TableName;

        public Result<PagedResult<IDictionary<string, object>>> GetPaged(int page, int pageSize, DynamicFilter filter)
        {
            if (!Can("View")) return FailDenied<PagedResult<IDictionary<string, object>>>();

            filter ??= new DynamicFilter();
            var (items, total) = _repo.GetPaged(page, pageSize, filter.SearchText, filter.SortBy, filter.SortDescending);

            return Result.Ok(new PagedResult<IDictionary<string, object>>
            { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<IDictionary<string, object>> GetById(int id)
        {
            if (!Can("View")) return FailDenied<IDictionary<string, object>>();

            var row = _repo.GetById(id);
            return row == null
                ? Result.Fail<IDictionary<string, object>>("السجل غير موجود", ErrorCode.NotFound)
                : Result.Ok(row);
        }

        public Result<IDictionary<string, object>> Create(IDictionary<string, object> values)
        {
            if (!Can("Create")) return FailDenied<IDictionary<string, object>>();

            var valid = Validate(values, 0);
            if (valid.IsFailure) return Result.Fail<IDictionary<string, object>>(valid.ErrorMessage, valid.ErrorCode);

            var id = _repo.Insert(values, CurrentUser);
            Audit.Log(EntityName, id, AuditAction.Insert, newValue: values);

            return Result.Ok(_repo.GetById(id));
        }

        public Result Update(IDictionary<string, object> values)
        {
            if (!Can("Edit")) return FailDenied();

            var id = values.TryGetValue("Id", out var raw) && raw != null ? System.Convert.ToInt32(raw) : 0;
            if (id == 0) return Result.Fail("السجل غير موجود", ErrorCode.NotFound);

            var valid = Validate(values, id);
            if (valid.IsFailure) return valid;

            _repo.Update(id, values);
            Audit.Log(EntityName, id, AuditAction.Update, newValue: values);

            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            _repo.Delete(id);
            Audit.Log(EntityName, id, AuditAction.Delete);

            return Result.Ok();
        }

        /// <summary>القواعد مُعلَنة على الأعمدة، والمتحقّق يقرؤها — وCheck يترجم نتيجته كأي خدمة.</summary>
        private Result Validate(IDictionary<string, object> values, int exceptId) =>
            Check(new BuilderRowValidator(_columns, (column, value) => _repo.Exists(column, value, exceptId)), values);
    }
}
