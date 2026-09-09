using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using System.Dynamic;

namespace PrimeERP.Application.Services.Builder
{
    /// <summary>
    /// خدمة صفوف: ما ينتظره CrudPageRenderer وDialogRenderer من أي شاشة. يُنفِّذه الجدولُ المبنيّ
    /// (DynamicEntityService) وشاشاتُ الوصف معاً، فنموذج عرض واحد يخدم الاثنين.
    /// </summary>
    public interface IRowService
    {
        Result<PagedResult<IDictionary<string, object>>> GetPaged(int page, int pageSize, DynamicFilter filter);
        Result<IDictionary<string, object>> GetById(int id);
        Result<IDictionary<string, object>> Create(IDictionary<string, object> values);
        Result Update(IDictionary<string, object> values);
        Result Delete(int id);
    }

    /// <summary>
    /// شاشات الوصف الخمس. ترث CrudServiceBase فتأخذ منه القراءة كاملةً (GetById/GetPaged/Search)
    /// بصلاحياتها ورسائلها — والإنشاء والتعديل والحذف تبقى هنا كما يقرّر الأساس نفسه، لأن منطق الكتابة
    /// يختلف بين كيان وآخر. والفرق بين الخمس ثلاث دوال: ماذا تقرأ، وكيف تكتب، وكيف تحذف.
    /// </summary>
    public abstract class BuilderCrudServiceBase<TEntity>
        : CrudServiceBase<TEntity, IDictionary<string, object>, DynamicFilter>, IRowService
        where TEntity : BaseModel
    {
        protected readonly IBuilderRepository Repo;

        protected BuilderCrudServiceBase(IBuilderRepository repo, IPermissionService permissions,
            ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit) => Repo = repo;

        protected override string PermissionPrefix => EntityName;
        protected override string StringPrefix => "Str.Builder";

        protected abstract List<TEntity> All();
        protected abstract int Write(IDictionary<string, object> values);
        protected abstract void Erase(int id);

        // ===== ما يطلبه الأساس للقراءة =====

        protected override TEntity FindById(int id) => All().FirstOrDefault(e => e.Id == id);

        protected override (List<TEntity> Items, int Total) FindPaged(int page, int pageSize, DynamicFilter filter)
        {
            var items = Match(Narrow(All(), filter), filter?.SearchText);
            return (items, items.Count);
        }

        /// <summary>ترشيح بالقسم أو الصفحة — كلٌّ يقرّر ما ينطبق عليه، والافتراضي بلا ترشيح.</summary>
        protected virtual List<TEntity> Narrow(List<TEntity> items, DynamicFilter filter) => items;

        protected override List<TEntity> FindSearch(string term, int maxResults) =>
            Match(All(), term).Take(maxResults).ToList();

        /// <summary>الصفّ ExpandoObject بخصائص الكيان — يربطه WPF بالاسم ويقرؤه الكود كقاموس.</summary>
        protected override IDictionary<string, object> ToDto(TEntity entity)
        {
            IDictionary<string, object> row = new ExpandoObject();

            foreach (var property in entity.GetType().GetProperties())
                row[property.Name] = property.GetValue(entity);

            return row;
        }

        private List<TEntity> Match(List<TEntity> items, string term) =>
            string.IsNullOrWhiteSpace(term)
                ? items
                : items.Where(e => ToDto(e).Values
                    .Any(v => v?.ToString()?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)).ToList();

        // ===== الكتابة: تبقى هنا كما يقرّر الأساس =====

        public Result<PagedResult<IDictionary<string, object>>> GetPaged(int page, int pageSize, DynamicFilter filter) =>
            base.GetPaged(page, pageSize, filter);

        public Result<IDictionary<string, object>> Create(IDictionary<string, object> values)
        {
            if (!Can("Create")) return FailDenied<IDictionary<string, object>>();

            var id = Write(values);
            Audit.Log(EntityName, id, AuditAction.Insert, newValue: values);

            return GetById(id);
        }

        public Result Update(IDictionary<string, object> values)
        {
            if (!Can("Edit")) return FailDenied();

            var id = Write(values);
            Audit.Log(EntityName, id, AuditAction.Update, newValue: values);

            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            Erase(id);
            Audit.Log(EntityName, id, AuditAction.Delete);

            return Result.Ok();
        }

        protected static int    Int(IDictionary<string, object> v, string key) => v.TryGetValue(key, out var r) && r != null ? Convert.ToInt32(r) : 0;
        protected static string Text(IDictionary<string, object> v, string key) => v.TryGetValue(key, out var r) ? r?.ToString() : null;
        protected static bool   Bool(IDictionary<string, object> v, string key) => v.TryGetValue(key, out var r) && r != null && Convert.ToBoolean(r);
        protected static double Num(IDictionary<string, object> v, string key) => v.TryGetValue(key, out var r) && r != null ? Convert.ToDouble(r) : 0d;
    }

    public class BuilderSectionsService : BuilderCrudServiceBase<BuilderSection>
    {
        public BuilderSectionsService(IBuilderRepository repo, IPermissionService p, ISettingsProvider s, ILocalizationService l, IAuditLogger a)
            : base(repo, p, s, l, a) { }

        protected override string EntityName => "BuilderSections";
        protected override List<BuilderSection> All() => Repo.Sections();
        protected override void Erase(int id) { }

        protected override int Write(IDictionary<string, object> v) => Repo.SaveSection(new BuilderSection
        {
            Id = Int(v, "Id"), Key = Text(v, "Key"), Title = Text(v, "Title"),
            IconKey = Text(v, "IconKey"), SortOrder = Int(v, "SortOrder"), CreatedBy = CurrentUser
        });
    }

    public class BuilderModulesService : BuilderCrudServiceBase<BuilderModule>
    {
        public BuilderModulesService(IBuilderRepository repo, IPermissionService p, ISettingsProvider s, ILocalizationService l, IAuditLogger a)
            : base(repo, p, s, l, a) { }

        protected override string EntityName => "BuilderModules";
        protected override List<BuilderModule> All() => Repo.Modules();

        protected override List<BuilderModule> Narrow(List<BuilderModule> items, DynamicFilter filter) =>
            filter?.SectionId == null ? items : items.Where(m => m.SectionId == filter.SectionId).ToList();
        protected override void Erase(int id) => Repo.DeleteModule(id);

        protected override int Write(IDictionary<string, object> v)
        {
            var id = Repo.SaveModule(new BuilderModule
            {
                Id = Int(v, "Id"), Key = Text(v, "Key"), Title = Text(v, "Title"),
                Kind = (BuilderKind)Int(v, "Kind"), SectionId = Int(v, "SectionId"),
                TableName = Text(v, "TableName"), LineTable = Text(v, "LineTable"),
                SourceKey = Text(v, "SourceKey"), CopiedFrom = Text(v, "CopiedFrom"),
                SortOrder = Int(v, "SortOrder"), IsActive = Bool(v, "IsActive"), CreatedBy = CurrentUser
            });

            // نسخة من صفحة: أعمدتها وأزرارها وفلاترها تُنسخ كما هي ثم تُعدَّل — نسخٌ لا بناء.
            var copiedFrom = Text(v, "CopiedFrom");
            if (Int(v, "Id") == 0 && !string.IsNullOrWhiteSpace(copiedFrom))
            {
                var source = Repo.Modules().FirstOrDefault(m => m.Key == copiedFrom);
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

    /// <summary>الأعمدة والأزرار والفلاتر أبناءٌ يُستبدلون بالجملة لصفحتهم — مزامنة واحدة للثلاثة.</summary>
    public abstract class BuilderChildService<TEntity> : BuilderCrudServiceBase<TEntity> where TEntity : BaseModel
    {
        protected BuilderChildService(IBuilderRepository repo, IPermissionService p, ISettingsProvider s, ILocalizationService l, IAuditLogger a)
            : base(repo, p, s, l, a) { }

        protected abstract List<TEntity> Of(int moduleId);
        protected abstract void Replace(int moduleId, List<TEntity> items);
        protected abstract TEntity From(IDictionary<string, object> values);
        protected abstract int ModuleOf(TEntity item);

        protected override List<TEntity> All() => Of(0);

        protected override List<TEntity> Narrow(List<TEntity> items, DynamicFilter filter)
        {
            if (filter?.ModuleId != null) return items.Where(x => ModuleOf(x) == filter.ModuleId).ToList();

            if (filter?.SectionId == null) return items;

            var inSection = Repo.Modules().Where(m => m.SectionId == filter.SectionId).Select(m => m.Id).ToHashSet();
            return items.Where(x => inSection.Contains(ModuleOf(x))).ToList();
        }

        protected override void Erase(int id)
        {
            var item = All().FirstOrDefault(x => x.Id == id);
            if (item == null) return;

            var module = ModuleOf(item);
            Replace(module, Of(module).Where(x => x.Id != id).ToList());
        }

        protected override int Write(IDictionary<string, object> v)
        {
            var incoming = From(v);
            var module = ModuleOf(incoming);

            var current = Of(module).Where(x => x.Id != incoming.Id).ToList();
            current.Add(incoming);
            Replace(module, current);

            return Of(module).LastOrDefault()?.Id ?? 0;
        }
    }

    public class BuilderColumnsService : BuilderChildService<BuilderColumn>
    {
        public BuilderColumnsService(IBuilderRepository repo, IPermissionService p, ISettingsProvider s, ILocalizationService l, IAuditLogger a)
            : base(repo, p, s, l, a) { }

        protected override string EntityName => "BuilderColumns";
        protected override List<BuilderColumn> Of(int moduleId) => Repo.Columns(moduleId);
        protected override void Replace(int moduleId, List<BuilderColumn> items) => Repo.ReplaceColumns(moduleId, items);
        protected override int ModuleOf(BuilderColumn item) => item.ModuleId;

        protected override BuilderColumn From(IDictionary<string, object> v) => new()
        {
            Id = Int(v, "Id"), ModuleId = Int(v, "ModuleId"), Name = Text(v, "Name"), Header = Text(v, "Header"),
            DataType = (BuilderDataType)Int(v, "DataType"), IsRequired = Bool(v, "IsRequired"), IsUnique = Bool(v, "IsUnique"),
            MaxLength = Int(v, "MaxLength") == 0 ? null : Int(v, "MaxLength"),
            RefModule = Text(v, "RefModule"), RefDisplay = Text(v, "RefDisplay"),
            Aggregate = (BuilderAggregate)Int(v, "Aggregate"),
            AggFrom = Text(v, "AggFrom"), AggColumn = Text(v, "AggColumn"), AggMatch = Text(v, "AggMatch"),
            ShowInGrid = Bool(v, "ShowInGrid"), ShowInForm = Bool(v, "ShowInForm"), IsLine = Bool(v, "IsLine"),
            Width = Num(v, "Width"), Footer = Text(v, "Footer"), SortOrder = Int(v, "SortOrder")
        };
    }

    public class BuilderActionsService : BuilderChildService<BuilderAction>
    {
        public BuilderActionsService(IBuilderRepository repo, IPermissionService p, ISettingsProvider s, ILocalizationService l, IAuditLogger a)
            : base(repo, p, s, l, a) { }

        protected override string EntityName => "BuilderActions";
        protected override List<BuilderAction> Of(int moduleId) => Repo.Actions(moduleId);
        protected override void Replace(int moduleId, List<BuilderAction> items) => Repo.ReplaceActions(moduleId, items);
        protected override int ModuleOf(BuilderAction item) => item.ModuleId;

        protected override BuilderAction From(IDictionary<string, object> v) => new()
        {
            Id = Int(v, "Id"), ModuleId = Int(v, "ModuleId"), ActionKey = Text(v, "ActionKey"),
            OnTable = Bool(v, "OnTable"), SortOrder = Int(v, "SortOrder")
        };
    }

    public class BuilderFiltersService : BuilderChildService<BuilderFilter>
    {
        public BuilderFiltersService(IBuilderRepository repo, IPermissionService p, ISettingsProvider s, ILocalizationService l, IAuditLogger a)
            : base(repo, p, s, l, a) { }

        protected override string EntityName => "BuilderFilters";
        protected override List<BuilderFilter> Of(int moduleId) => Repo.Filters(moduleId);
        protected override void Replace(int moduleId, List<BuilderFilter> items) => Repo.ReplaceFilters(moduleId, items);
        protected override int ModuleOf(BuilderFilter item) => item.ModuleId;

        protected override BuilderFilter From(IDictionary<string, object> v) => new()
        {
            Id = Int(v, "Id"), ModuleId = Int(v, "ModuleId"), Key = Text(v, "Key"), Label = Text(v, "Label"),
            Kind = Text(v, "Kind"), RefModule = Text(v, "RefModule"), SortOrder = Int(v, "SortOrder")
        };
    }
}
