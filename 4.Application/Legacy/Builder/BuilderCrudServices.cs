using PrimeERP.Application.Services.Entities;
using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Repositories;
using PrimeERP.Application.Validation;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using System.Dynamic;

namespace PrimeERP.Application.Legacy.Builder
{
    /// <summary>خدمة صفوف</summary>
    public interface IRowService
    {
        Result<PagedResult<IDictionary<string, object>>> GetPaged(int page, int pageSize, DynamicFilter filter);
        Result<IDictionary<string, object>> GetById(int id);
        Result<IDictionary<string, object>> Create(IDictionary<string, object> values);
        Result Update(IDictionary<string, object> values);
        Result Delete(int id);
    }

    /// <summary>شاشات الوصف الخمس</summary>
    public abstract class BuilderCrudServiceBase<TEntity>
        : CrudServiceBase<TEntity, IDictionary<string, object>, DynamicFilter>, IRowService
        where TEntity : BaseModel, new()
    {
        protected readonly IBuilderRepository Repo;

        protected BuilderCrudServiceBase(IBuilderRepository repo, IPermissionService permissions,
            ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit) => Repo = repo;

        protected override string PermissionPrefix => EntityName;
        protected override string StringPrefix => "Str.Builder";

        protected abstract List<TEntity> All();
        protected abstract int Write(IDictionary<string, object> values);
        protected abstract Result Erase(int id);


        protected override TEntity FindById(int id) => All().FirstOrDefault(e => e.Id == id);

        protected override (List<TEntity> Items, int Total) FindPaged(int page, int pageSize, DynamicFilter filter)
        {
            var items = Rows.Search(Narrow(Sort(All()), filter), filter?.SearchText);
            return (items, items.Count);
        }

        protected virtual List<TEntity> Narrow(List<TEntity> items, DynamicFilter filter) => items;

        protected virtual List<TEntity> Sort(List<TEntity> items) => items;

        protected Dictionary<int, int> SectionOrder() =>
            Repo.Sections().ToDictionary(section => section.Id, section => section.SortOrder);

        protected override List<TEntity> FindSearch(string term, int maxResults) => Rows.Search(All(), term, maxResults);

        protected override IDictionary<string, object> ToDto(TEntity entity) => Rows.Of(entity);


        public Result<PagedResult<IDictionary<string, object>>> GetPaged(int page, int pageSize, DynamicFilter filter) =>
            base.GetPaged(page, pageSize, filter);

        /// <summary>تحقّق الوصف قبل كتابته</summary>
        protected virtual Result Validate(IDictionary<string, object> values) => Result.Ok();

        public Result<IDictionary<string, object>> Create(IDictionary<string, object> values)
        {
            if (!Can("Create")) return FailDenied<IDictionary<string, object>>();

            var check = Validate(values);
            if (check.IsFailure) return check.As<IDictionary<string, object>>();

            var id = Write(values);
            Audit.Log(EntityName, id, AuditAction.Insert, newValue: values);

            return GetById(id);
        }

        public Result Update(IDictionary<string, object> values)
        {
            if (!Can("Edit")) return FailDenied();

            var check = Validate(values);
            if (check.IsFailure) return check;

            var id = Write(values);
            Audit.Log(EntityName, id, AuditAction.Update, newValue: values);

            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var erased = Erase(id);
            if (!erased.IsSuccess) return erased;

            Audit.Log(EntityName, id, AuditAction.Delete);

            return Result.Ok();
        }

        protected static int Order(int written, IEnumerable<int> siblings) =>
            written > 0 ? written : siblings.DefaultIfEmpty(0).Max() + 10;

        /// <summary>الكيان من قيم الحوار</summary>
        protected static TEntity From(IDictionary<string, object> values)
        {
            var entity = new TEntity { Id = Rows.Id(values) };
            Rows.Fill(entity, values);
            return entity;
        }
    }

    public class BuilderSectionsService : BuilderCrudServiceBase<BuilderSection>
    {
        public BuilderSectionsService(IBuilderRepository repo, IPermissionService p, ISettingsProvider s, ILocalizationService l, IAuditLogger a)
            : base(repo, p, s, l, a) { }

        protected override string EntityName => "BuilderSections";
        protected override List<BuilderSection> All() => Repo.Sections();

        protected override Result Erase(int id)
        {
            if (Repo.Sections().FirstOrDefault(s => s.Id == id)?.IsProtected == true)
                return Result.Fail(Msg("ProtectedSection"), ErrorCode.ValidationFailed);

            if (Repo.Modules().Any(m => m.SectionId == id))
                return Result.Fail(Msg("SectionHasModules"), ErrorCode.ValidationFailed);

            Repo.DeleteSection(id);
            return Result.Ok();
        }

        protected override int Write(IDictionary<string, object> v)
        {
            var section = From(v);
            section.SortOrder = Order(section.SortOrder, Repo.Sections().Select(s => s.SortOrder));
            section.Modules = Repo.Sections().FirstOrDefault(s => s.Id == section.Id)?.Modules;
            return Repo.SaveSection(section);
        }
    }

    public class BuilderModulesService : BuilderCrudServiceBase<BuilderModule>
    {
        public BuilderModulesService(IBuilderRepository repo, IPermissionService p, ISettingsProvider s, ILocalizationService l, IAuditLogger a)
            : base(repo, p, s, l, a) { }

        private Dictionary<int, string> _sections;

        protected override string EntityName => "BuilderModules";
        protected override List<BuilderModule> All() => Repo.Modules();

        protected override Result Validate(IDictionary<string, object> v) => Check.Valid(From(v), ModuleFields(Repo.Modules()));

        /// <summary>شروط الصفحة المبنيّة</summary>
        private static Field<BuilderModule>[] ModuleFields(List<BuilderModule> existing) => new Field<BuilderModule>[]
        {
            new(m => m.Key, "Str.Field.PageKey", Required: true),
            new(m => m.Title, "Str.Field.PageName", Required: true),
            new(m => m.SectionId, "", Required: true, Message: "Str.Builder.SectionRequired"),
            new(m => m.Key, "", Must: m => !existing.Any(e => e.Id != m.Id && e.Key == m.Key), Message: "Str.Builder.PageKeyDuplicate"),
            new(m => m.TableName, "", Must: m => m.Kind == BuilderKind.Report || !string.IsNullOrWhiteSpace(m.TableName), Message: "Str.Builder.TableRequired"),
            new(m => m.TableName, "", Must: m => m.Kind == BuilderKind.Report || string.IsNullOrWhiteSpace(m.TableName)
                || !existing.Any(e => e.Id != m.Id && e.TableName == m.TableName), Message: "Str.Builder.TableInUse"),
        };

        protected override List<BuilderModule> Sort(List<BuilderModule> items)
        {
            var sections = SectionOrder();

            return items
                .OrderBy(m => sections.TryGetValue(m.SectionId, out var order) ? order : int.MaxValue)
                .ThenBy(m => m.SortOrder)
                .ToList();
        }

        protected override List<BuilderModule> Narrow(List<BuilderModule> items, DynamicFilter filter)
        {
            _sections = null;
            return filter?.SectionId == null ? items : items.Where(m => m.SectionId == filter.SectionId).ToList();
        }

        protected override Result Erase(int id)
        {
            Repo.DeleteModule(id);
            return Result.Ok();
        }

        protected override IDictionary<string, object> ToDto(BuilderModule entity)
        {
            var row = base.ToDto(entity);

            _sections ??= Repo.Sections().ToDictionary(s => s.Id, s => s.Title);
            row["SectionName"] = _sections.TryGetValue(entity.SectionId, out var title) ? title : null;
            row["KindName"] = Msg($"Kind.{entity.Kind}");

            return row;
        }

        protected override int Write(IDictionary<string, object> v)
        {
            var module = From(v);
            module.SortOrder = Order(module.SortOrder, Repo.Modules().Where(m => m.SectionId == module.SectionId).Select(m => m.SortOrder));
            module.IsCoded = Repo.Modules().FirstOrDefault(m => m.Id == module.Id)?.IsCoded ?? false;
            var isNew = module.Id == 0;
            var id = Repo.SaveModule(module);

            if (isNew && !string.IsNullOrWhiteSpace(module.CopiedFrom))
            {
                var source = Repo.Modules().FirstOrDefault(m => m.Key == module.CopiedFrom);
                if (source != null)
                {
                    Repo.ReplaceColumns(id, Repo.Columns(source.Id).Select(c => { c.ModuleId = id; return c; }).ToList());
                    Repo.ReplaceActions(id, Repo.Actions(source.Id).Select(a => { a.ModuleId = id; return a; }).ToList());
                    Repo.ReplaceFilters(id, Repo.Filters(source.Id).Select(f => { f.ModuleId = id; return f; }).ToList());
                }
            }

            return id;
        }
    }

    /// <summary>أبناء الصفحة يُستبدلون بالجملة</summary>
    public abstract class BuilderChildService<TEntity> : BuilderCrudServiceBase<TEntity> where TEntity : BaseModel, new()
    {
        protected BuilderChildService(IBuilderRepository repo, IPermissionService p, ISettingsProvider s, ILocalizationService l, IAuditLogger a)
            : base(repo, p, s, l, a) { }

        protected abstract List<TEntity> Of(int moduleId);
        protected abstract int ModuleOf(TEntity item);
        protected abstract int Save(int moduleId, TEntity item);
        protected abstract void Remove(int id);

        private Dictionary<int, string> _modules;
        private Dictionary<int, string> _sections;

        protected override List<TEntity> All() => Of(0);

        protected override IDictionary<string, object> ToDto(TEntity entity)
        {
            var row = base.ToDto(entity);

            if (_modules == null)
            {
                var sections = Repo.Sections().ToDictionary(s => s.Id, s => s.Title);
                var modules = Repo.Modules();

                _modules = modules.ToDictionary(m => m.Id, m => m.Title);
                _sections = modules.ToDictionary(m => m.Id,
                    m => sections.TryGetValue(m.SectionId, out var section) ? section : null);
            }

            row["ModuleName"] = _modules.TryGetValue(ModuleOf(entity), out var title) ? title : null;
            row["SectionName"] = _sections.TryGetValue(ModuleOf(entity), out var owner) ? owner : null;

            return row;
        }

        protected override List<TEntity> Sort(List<TEntity> items)
        {
            var sections = SectionOrder();
            var modules = Repo.Modules().ToDictionary(m => m.Id,
                m => (Section: sections.TryGetValue(m.SectionId, out var order) ? order : int.MaxValue, Page: m.SortOrder));

            (int Section, int Page) Place(TEntity item) =>
                modules.TryGetValue(ModuleOf(item), out var place) ? place : (int.MaxValue, int.MaxValue);

            return items
                .OrderBy(item => Place(item).Section)
                .ThenBy(item => Place(item).Page)
                .ThenBy(OrderOf)
                .ToList();
        }

        protected override List<TEntity> Narrow(List<TEntity> items, DynamicFilter filter)
        {
            _modules = null;
            _sections = null;

            if (filter?.ModuleId != null) return items.Where(x => ModuleOf(x) == filter.ModuleId).ToList();

            if (filter?.SectionId == null) return items;

            var inSection = Repo.Modules().Where(m => m.SectionId == filter.SectionId).Select(m => m.Id).ToHashSet();
            return items.Where(x => inSection.Contains(ModuleOf(x))).ToList();
        }

        protected override Result Erase(int id)
        {
            Remove(id);
            return Result.Ok();
        }

        protected override int Write(IDictionary<string, object> v)
        {
            var incoming = From(v);
            v["SortOrder"] = Order(OrderOf(incoming), Of(ModuleOf(incoming)).Select(OrderOf));
            Rows.Fill(incoming, v);

            return Save(ModuleOf(incoming), incoming);
        }

        private int OrderOf(TEntity item) => Convert.ToInt32(base.ToDto(item)["SortOrder"] ?? 0);
    }

    public class BuilderColumnsService : BuilderChildService<BuilderColumn>
    {
        public BuilderColumnsService(IBuilderRepository repo, IPermissionService p, ISettingsProvider s, ILocalizationService l, IAuditLogger a)
            : base(repo, p, s, l, a) { }

        protected override string EntityName => "BuilderColumns";
        protected override List<BuilderColumn> Of(int moduleId) => Repo.Columns(moduleId);
        protected override int Save(int moduleId, BuilderColumn item) => Repo.SaveColumn(moduleId, item);
        protected override void Remove(int id) => Repo.DeleteColumn(id);
        protected override int ModuleOf(BuilderColumn item) => item.ModuleId;

        private Dictionary<int, double> _shares;

        protected override IDictionary<string, object> ToDto(BuilderColumn entity)
        {
            var row = base.ToDto(entity);

            _shares ??= Repo.Columns(0).Where(c => c.ShowInGrid && !c.IsLine)
                .GroupBy(c => c.ModuleId)
                .SelectMany(module => LayoutCalc.Shares(module.ToList(), 1))
                .ToDictionary(share => share.Key, share => share.Value);

            row["WidthPercent"] = _shares.TryGetValue(entity.Id, out var percent) ? percent : 0d;

            return row;
        }

        protected override List<BuilderColumn> Narrow(List<BuilderColumn> items, DynamicFilter filter)
        {
            _shares = null;
            return base.Narrow(items, filter);
        }
    }

    public class BuilderActionsService : BuilderChildService<BuilderAction>
    {
        public BuilderActionsService(IBuilderRepository repo, IPermissionService p, ISettingsProvider s, ILocalizationService l, IAuditLogger a)
            : base(repo, p, s, l, a) { }

        protected override string EntityName => "BuilderActions";
        protected override List<BuilderAction> Of(int moduleId) => Repo.Actions(moduleId);
        protected override int Save(int moduleId, BuilderAction item) => Repo.SaveAction(moduleId, item);
        protected override void Remove(int id) => Repo.DeleteAction(id);
        protected override int ModuleOf(BuilderAction item) => item.ModuleId;
    }

    public class BuilderFiltersService : BuilderChildService<BuilderFilter>
    {
        public BuilderFiltersService(IBuilderRepository repo, IPermissionService p, ISettingsProvider s, ILocalizationService l, IAuditLogger a)
            : base(repo, p, s, l, a) { }

        protected override string EntityName => "BuilderFilters";
        protected override List<BuilderFilter> Of(int moduleId) => Repo.Filters(moduleId);
        protected override int Save(int moduleId, BuilderFilter item) => Repo.SaveFilter(moduleId, item);
        protected override void Remove(int id) => Repo.DeleteFilter(id);
        protected override int ModuleOf(BuilderFilter item) => item.ModuleId;
    }
}
