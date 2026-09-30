using PrimeERP.Application.Legacy.Builder;
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

        public Lookup(EntitySpec spec, ILookupRepository<TEntity> rows, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, INumberSequenceService numbers)
            : base(permissions, settings, localization, audit, numbers)
        {
            _spec = spec;
            _rows = rows;
            _fields = spec.NameLabel == null ? Array.Empty<Field<TEntity>>() : new[] { new Field<TEntity>(NameOf(), spec.NameLabel, Required: true) };
        }

        protected override string PermissionPrefix => _spec.Key;
        protected override string StringPrefix => _spec.Strings;
        protected override string EntityName => _spec.Key;
        protected override string SequenceKey => _spec.Sequence;
        protected override Field<TEntity>[] Fields => _fields;

        public Result<List<IDictionary<string, object>>> GetAll(bool includeInactive = false) =>
            Result.Ok(_rows.GetAll(includeInactive).Select(ToDto).ToList());

        public Dictionary<int, string> NamesOf(IEnumerable<int> ids) => _rows.NamesOf(ids);

        protected override TEntity FindById(int id) => _rows.GetById(id);

        protected override (List<TEntity> Items, int Total) FindPaged(int page, int pageSize, DynamicFilter filter)
        {
            var rows = FindSearch(filter?.SearchText, int.MaxValue);
            return (rows.Skip(Math.Max(0, page - 1) * pageSize).Take(pageSize).ToList(), rows.Count);
        }

        protected override List<TEntity> FindSearch(string term, int maxResults) =>
            _rows.GetAll()
                 .Where(e => string.IsNullOrWhiteSpace(term) || ToDto(e).Values.Any(v => v?.ToString()?.Contains(term, StringComparison.OrdinalIgnoreCase) == true))
                 .Take(maxResults).ToList();

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
        protected override void Erase(PrimeDbContext db, TEntity entity) => _rows.Delete(entity.Id, db);

        protected override object AuditValue(TEntity entity) => ToDto(entity);
        protected override IDictionary<string, object> ToDto(TEntity entity) => Rows.Of(entity);

        private static Expression<Func<TEntity, object>> NameOf()
        {
            var entity = Expression.Parameter(typeof(TEntity), "e");
            return Expression.Lambda<Func<TEntity, object>>(Expression.Convert(Expression.Property(entity, "Name"), typeof(object)), entity);
        }
    }
}
