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
        protected abstract Result Erase(int id);

        // ===== ما يطلبه الأساس للقراءة =====

        protected override TEntity FindById(int id) => All().FirstOrDefault(e => e.Id == id);

        protected override (List<TEntity> Items, int Total) FindPaged(int page, int pageSize, DynamicFilter filter)
        {
            var items = Match(Narrow(Sort(All()), filter), filter?.SearchText);
            if (pageSize <= 0) return (items, items.Count);

            return (items.Skip((page < 1 ? 0 : page - 1) * pageSize).Take(pageSize).ToList(), items.Count);
        }

        /// <summary>ترشيح بالقسم أو الصفحة — كلٌّ يقرّر ما ينطبق عليه، والافتراضي بلا ترشيح.</summary>
        protected virtual List<TEntity> Narrow(List<TEntity> items, DynamicFilter filter) => items;

        protected virtual List<TEntity> Sort(List<TEntity> items) => items;

        protected Dictionary<int, int> SectionOrder() =>
            Repo.Sections().ToDictionary(section => section.Id, section => section.SortOrder);

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

            var erased = Erase(id);
            if (!erased.IsSuccess) return erased;

            Audit.Log(EntityName, id, AuditAction.Delete);

            return Result.Ok();
        }

        /// <summary>ترتيبٌ مذكور يُحترَم، وغيابه يعني آخر القائمة — فالسهمان وحدهما ما يُعيد الترتيب.</summary>
        protected static int Order(IDictionary<string, object> v, IEnumerable<int> siblings)
        {
            var written = Int(v, "SortOrder");
            return written > 0 ? written : siblings.DefaultIfEmpty(0).Max() + 10;
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

        /// <summary>قسمٌ به صفحات لا يُحذف — الصفحة تطلب قسماً، فلا يُترك يتيم.</summary>
        protected override Result Erase(int id)
        {
            if (Repo.Modules().Any(m => m.SectionId == id))
                return Fail(Msg("SectionHasModules"), ErrorCode.ValidationFailed);

            Repo.DeleteSection(id);
            return Result.Ok();
        }

        protected override int Write(IDictionary<string, object> v) => Repo.SaveSection(new BuilderSection
        {
            SortOrder = Order(v, Repo.Sections().Select(s => s.SortOrder)),
            Id = Int(v, "Id"), Key = Text(v, "Key"), Title = Text(v, "Title"),
            IconKey = Text(v, "IconKey"), CreatedBy = CurrentUser,
            // عمود الصفحات احتياطُ قاعدةٍ لم تُبذَر — يُقرأ من الصفّ المحفوظ فلا يمحوه تعديلٌ للاسم.
            Modules = Repo.Sections().FirstOrDefault(s => s.Id == Int(v, "Id"))?.Modules
        });
    }

    public class BuilderModulesService : BuilderCrudServiceBase<BuilderModule>
    {
        public BuilderModulesService(IBuilderRepository repo, IPermissionService p, ISettingsProvider s, ILocalizationService l, IAuditLogger a)
            : base(repo, p, s, l, a) { }

        private Dictionary<int, string> _sections;

        protected override string EntityName => "BuilderModules";
        protected override List<BuilderModule> All() => Repo.Modules();

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

        /// <summary>الشبكة تعرض الاسم لا المعرِّف — القسم من صفّه، والنوع من نصوصه.</summary>
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
            var id = Repo.SaveModule(new BuilderModule
            {
                SortOrder = Order(v, Repo.Modules().Where(m => m.SectionId == Int(v, "SectionId")).Select(m => m.SortOrder)),
                Id = Int(v, "Id"), Key = Text(v, "Key"), Title = Text(v, "Title"),
                Kind = (BuilderKind)Int(v, "Kind"), SectionId = Int(v, "SectionId"),
                TableName = Text(v, "TableName"), LineTable = Text(v, "LineTable"),
                SourceKey = Text(v, "SourceKey"), CopiedFrom = Text(v, "CopiedFrom"),
                IsActive = Bool(v, "IsActive"), CreatedBy = CurrentUser,
                // «مبذورة من الكود» صفةُ منشأٍ لا حقلَ حوار — تُقرأ من الصفّ المحفوظ فلا يمحوها تعديلٌ للاسم.
                IsCoded = Repo.Modules().FirstOrDefault(m => m.Id == Int(v, "Id"))?.IsCoded ?? false
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
        protected abstract TEntity From(IDictionary<string, object> values);
        protected abstract int ModuleOf(TEntity item);
        protected abstract int Save(int moduleId, TEntity item);
        protected abstract void Remove(int id);

        private Dictionary<int, string> _modules;
        private Dictionary<int, string> _sections;

        protected override List<TEntity> All() => Of(0);

        /// <summary>الشبكة تعرض اسم الصفحة وقسمها لا معرِّفاتهما — كشاشة الصفحات.</summary>
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
            v["SortOrder"] = Order(v, Of(Int(v, "ModuleId")).Select(OrderOf));

            var incoming = From(v);
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

        protected override BuilderColumn From(IDictionary<string, object> v) => new()
        {
            Id = Int(v, "Id"), ModuleId = Int(v, "ModuleId"), Name = Text(v, "Name"), Header = Text(v, "Header"),
            DataType = (BuilderDataType)Int(v, "DataType"), IsRequired = Bool(v, "IsRequired"), IsUnique = Bool(v, "IsUnique"),
            MaxLength = Int(v, "MaxLength") == 0 ? null : Int(v, "MaxLength"),
            RefModule = Text(v, "RefModule"), RefDisplay = Text(v, "RefDisplay"),
            Aggregate = (BuilderAggregate)Int(v, "Aggregate"),
            AggFrom = Text(v, "AggFrom"), AggColumn = Text(v, "AggColumn"), AggMatch = Text(v, "AggMatch"),
            ShowInGrid = Bool(v, "ShowInGrid"), ShowInForm = Bool(v, "ShowInForm"), IsLine = Bool(v, "IsLine"),
            Width = Num(v, "Width"), WidthPercent = Num(v, "WidthPercent"),
            Footer = Text(v, "Footer"), SortOrder = Int(v, "SortOrder")
        };

        private Dictionary<int, double> _shares;

        /// <summary>النسبة الظاهرة هي التي يُطبّقها المُحمِّل — من ColumnWidths، فلا يختلف المعروض عن المطبَّق.</summary>
        protected override IDictionary<string, object> ToDto(BuilderColumn entity)
        {
            var row = base.ToDto(entity);

            _shares ??= Repo.Columns(0).Where(c => c.ShowInGrid && !c.IsLine)
                .GroupBy(c => c.ModuleId)
                .SelectMany(module => ColumnWidths.Shares(module.ToList()))
                .ToDictionary(share => share.Key, share => System.Math.Round(share.Value, 1));

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
        protected override int Save(int moduleId, BuilderFilter item) => Repo.SaveFilter(moduleId, item);
        protected override void Remove(int id) => Repo.DeleteFilter(id);
        protected override int ModuleOf(BuilderFilter item) => item.ModuleId;

        protected override BuilderFilter From(IDictionary<string, object> v) => new()
        {
            Id = Int(v, "Id"), ModuleId = Int(v, "ModuleId"), Key = Text(v, "Key"), Label = Text(v, "Label"),
            Kind = Text(v, "Kind"), RefModule = Text(v, "RefModule"), SortOrder = Int(v, "SortOrder")
        };
    }
}
