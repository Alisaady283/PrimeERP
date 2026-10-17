using PrimeERP.Application.PageServices.Builder;
using PrimeERP.Application.Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Entities
{
    /// <summary>صفحة قائمةٍ من إعلانها</summary>
    public sealed class Lookup<TEntity>
        : EntityService<TEntity, IDictionary<string, object>, IDictionary<string, object>, IDictionary<string, object>, DynamicFilter>,
          IRowService
        where TEntity : BaseModel, new()
    {
        private readonly EntitySpec _spec;
        private readonly ILookupRepository<TEntity> _rows;
        private readonly Field<TEntity>[] _fields;
        private readonly IServiceProvider _services;
        private readonly IRowPosting<TEntity> _posting;

        public Lookup(EntitySpec spec, ILookupRepository<TEntity> rows, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, INumberSequenceService numbers,
            IServiceProvider services, IEnumerable<IRowPosting<TEntity>> postings)
            : base(permissions, settings, localization, audit, numbers)
        {
            _spec = spec;
            _rows = rows;
            _services = services;
            _posting = postings.FirstOrDefault();
            _fields = (spec.NameLabel == null ? Enumerable.Empty<Field<TEntity>>() : new[] { new Field<TEntity>(Property("Name"), spec.NameLabel, Required: true) })
                .Concat((spec.Required ?? new Dictionary<string, string>()).Select(r => new Field<TEntity>(Property(r.Key), r.Value, Required: true)))
                .ToArray();
        }

        protected override string PermissionPrefix => _spec.Key;
        protected override string StringPrefix => _spec.Strings;
        protected override string EntityName => _spec.Key;
        protected override string SequenceKey => _spec.Sequence;
        protected override Field<TEntity>[] Fields => _fields;

        public Result<List<IDictionary<string, object>>> GetAll(bool includeInactive = false) =>
            Result.Ok(ToDtos(Scoped(_rows.GetAll(includeInactive)).ToList()));

        private IEnumerable<TEntity> Scoped(IEnumerable<TEntity> rows) =>
            !_spec.BranchScoped || AppSession.BranchId == null ? rows
                : rows.Where(r => Equals(typeof(TEntity).GetProperty("BranchId")?.GetValue(r), AppSession.BranchId));

        public Dictionary<int, string> NamesOf(IEnumerable<int> ids) => _rows.NamesOf(ids);

        public Result SeedDefault()
        {
            if (_spec.Defaults == null || _rows.GetAll(includeInactive: true).Count > 0) return Result.Ok();

            var created = Add(_spec.Defaults.ToDictionary(field => field.Key, field => (object)LocalizationService.Get(field.Value)));
            return created.IsSuccess ? Result.Ok() : Result.Fail(created.ErrorMessage, created.ErrorCode);
        }

        protected override TEntity FindById(int id) => _rows.GetById(id);

        protected override (List<TEntity> Items, int Total) FindPaged(int page, int pageSize, DynamicFilter filter)
        {
            var rows = FindSearch(filter?.SearchText, int.MaxValue);
            return (rows, rows.Count);
        }

        protected override List<TEntity> FindSearch(string term, int maxResults) => Rows.Search(_rows.GetAll(), term, maxResults);

        protected override TEntity New(IDictionary<string, object> values)
        {
            var entity = new TEntity();
            Rows.Fill(entity, values);
            return entity;
        }

        protected override void Apply(TEntity entity, IDictionary<string, object> values) => Rows.Fill(entity, values);
        protected override int IdOf(IDictionary<string, object> values) => Rows.Id(values);
        protected override void Number(TEntity entity, string code) => typeof(TEntity).GetProperty("Code")?.SetValue(entity, code);

        protected override int Insert(PrimeDbContext db, TEntity entity) => _rows.Insert(entity, db);
        protected override void Save(PrimeDbContext db, TEntity entity) => _rows.Update(entity, db);

        protected override void Erase(PrimeDbContext db, TEntity entity)
        {
            _posting?.Reverse(db, entity);
            _rows.Delete(entity.Id, db);
        }

        // الصفّ الذي يحمل قيده: يُحسب ويُتحقّق قبل الحفظ، ويُقيَّد في معاملته، ويُعكس قبل الحذف
        protected override Result Prepare(TEntity entity, TEntity stored) =>
            _posting == null ? Result.Ok() : _posting.Prepare(entity).Then(() => stored == null ? Result.Ok() : _posting.CanReverse(stored));

        protected override Result OnSaved(PrimeDbContext db, TEntity entity, TEntity stored)
        {
            if (_posting == null) return Result.Ok();
            var posted = _posting.Post(db, entity, stored);
            if (posted.IsSuccess) _rows.Update(entity, db);
            return posted;
        }

        protected override Result CanErase(TEntity entity) => _posting?.CanReverse(entity) ?? Result.Ok();

        protected override object AuditValue(TEntity entity) => Rows.Of(entity);
        protected override IDictionary<string, object> ToDto(TEntity entity) => ToDtos(new List<TEntity> { entity })[0];

        /// <summary>الصفوف بأسماء معرّفاتها — كل مصدرٍ بدفعةٍ واحدة</summary>
        protected override List<IDictionary<string, object>> ToDtos(List<TEntity> entities)
        {
            var rows = entities.Select(Rows.Of).ToList();
            foreach (var join in _spec.Names ?? Array.Empty<NameJoin>())
            {
                var ids = rows.Select(r => r.TryGetValue(join.Id, out var v) ? v as int? : null).Where(v => v is > 0).Select(v => v.Value).Distinct().ToList();
                var names = ids.Count == 0 ? new Dictionary<int, string>() : join.Of(_services, ids);
                foreach (var row in rows)
                    row[join.Name] = row.TryGetValue(join.Id, out var v) && v is int id ? names.GetValueOrDefault(id) : null;
            }
            return rows;
        }

        private static Expression<Func<TEntity, object>> Property(string name)
        {
            var entity = Expression.Parameter(typeof(TEntity), "e");
            return Expression.Lambda<Func<TEntity, object>>(Expression.Convert(Expression.Property(entity, name), typeof(object)), entity);
        }
    }
}
