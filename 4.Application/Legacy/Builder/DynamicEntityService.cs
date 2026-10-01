using PrimeERP.Application.Services.Core;
using PrimeERP.Application.Services.Entities;
using System.Collections.Generic;
using System.Linq;
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

namespace PrimeERP.Application.Legacy.Builder
{
    /// <summary>مرشِّح أي وحدة مبنيّة</summary>
    public class DynamicFilter
    {
        public string SearchText { get; set; }
        public string SortBy { get; set; }
        public bool   SortDescending { get; set; }

        public int?   SectionId { get; set; }
        public int?   ModuleId  { get; set; }
    }

    /// <summary>خدمة أي جدول بناه المستخدم</summary>
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

            return Result.Ok(Paged(items, total, page, pageSize, rows => rows));
        }

        public Result<IDictionary<string, object>> GetById(int id)
        {
            if (!Can("View")) return FailDenied<IDictionary<string, object>>();

            var row = _repo.GetById(id);
            return row == null
                ? Result.Fail<IDictionary<string, object>>(Localization.Get("Str.Common.RecordNotFound"), ErrorCode.NotFound)
                : Result.Ok(row);
        }

        public Result<IDictionary<string, object>> Create(IDictionary<string, object> values)
        {
            if (!Can("Create")) return FailDenied<IDictionary<string, object>>();

            var valid = Validate(values, 0);
            if (valid.IsFailure) return Result.Fail<IDictionary<string, object>>(valid.ErrorMessage, valid.ErrorCode);

            var id = _repo.Insert(values);
            Audit.Log(EntityName, id, AuditAction.Insert, newValue: values);

            return Result.Ok(_repo.GetById(id));
        }

        public Result Update(IDictionary<string, object> values)
        {
            if (!Can("Edit")) return FailDenied();

            var id = Rows.Id(values);
            if (id == 0) return Result.Fail(Localization.Get("Str.Common.RecordNotFound"), ErrorCode.NotFound);

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

        private Result Validate(IDictionary<string, object> values, int exceptId) =>
            Check.Valid(values, _columns.Where(c => c.Aggregate == BuilderAggregate.None && c.ShowInForm)
                .SelectMany(c => RowFields(c, exceptId)).ToArray());

        /// <summary>شروط العمود من وصفه</summary>
        private IEnumerable<Field<IDictionary<string, object>>> RowFields(BuilderColumn c, int exceptId)
        {
            var name = c.Name;
            if (c.IsRequired)
                yield return new(r => r.ContainsKey(name) ? r[name] : null, "", Required: true, Name: name,
                    Message: "Str.Builder.FieldRequired", Args: _ => new object[] { c.Header });
            if (c.MaxLength is > 0)
                yield return new(r => r.ContainsKey(name) ? r[name] : null, "", Max: c.MaxLength.Value, Name: name,
                    Message: "Str.Builder.FieldTooLong", Args: _ => new object[] { c.Header, c.MaxLength });
            if (c.IsUnique)
                yield return new(r => r.ContainsKey(name) ? r[name] : null, "", Name: name, Message: "Str.Builder.FieldDuplicate",
                    Must: r => !r.TryGetValue(name, out var v) || string.IsNullOrWhiteSpace(v?.ToString()) || !_repo.Exists(name, v, exceptId),
                    Args: _ => new object[] { c.Header });
        }
    }
}
