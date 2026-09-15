using System.Collections.Generic;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using System.Linq;

namespace PrimeERP.Application.Services.Builder
{
    /// <summary>
    /// وصفُ ما بناه المستخدم كما تقرؤه الطبقات العليا. المُصيِّرات وتسجيل الوحدات تستهلك هذه لا المستودع
    /// مباشرةً — نفس القيد الذي يمنع أي شاشة من استدعاء مستودع، ويفحصه check.sh.
    /// </summary>
    /// <summary>صفحة مكتوبة كما تُقرأ من تسجيلها — تُبذَر صفّاً في الوصف فتظهر وتُرتَّب وتُحذَف كالمبنيّة.</summary>
    public record CodedPage(string SectionKey, string Key, string Title, BuilderKind Kind,
        List<BuilderColumn> Columns, List<BuilderAction> Actions, List<BuilderFilter> Filters);

    public interface IBuilderCatalog
    {
        List<BuilderSection> Sections();
        List<BuilderModule>  Modules();
        List<BuilderColumn>  Columns(int moduleId);
        List<BuilderAction>  Actions(int moduleId);
        List<BuilderFilter>  Filters(int moduleId);

        /// <summary>صفوف قائمة من جدول مبنيّ: المُعرِّف وعمود العرض.</summary>
        List<(int Id, string Display)> PickerRows(string table, string displayColumn);

        /// <summary>يُنشئ جدول الوحدة من أعمدتها — يُستدعى عند كل إقلاع، وآمن للتكرار.</summary>
        void EnsureBuiltTable(BuilderModule module, List<BuilderColumn> columns);

        /// <summary>يبذر أقسام الكود غير المحميّة مرّةً فتصير قابلةً للتعديل — آمن للتكرار.</summary>
        void SeedSections(IEnumerable<(string Key, string Title, string IconKey, string[] Modules)> coded,
                          IReadOnlyCollection<string> protectedKeys);

        /// <summary>يبذر صفحات الكود بأعمدتها وأزرارها وفلاترها مرّةً — ولا يُعيد ما حذفه المستخدم.</summary>
        void SeedModules(IEnumerable<CodedPage> pages);
    }

    public class BuilderCatalog : IBuilderCatalog
    {
        private readonly IBuilderRepository _repo;

        public BuilderCatalog(IBuilderRepository repo) => _repo = repo;

        public List<BuilderSection> Sections() => _repo.Sections();
        public List<BuilderModule>  Modules()  => _repo.Modules();
        public List<BuilderColumn>  Columns(int moduleId) => _repo.Columns(moduleId);
        public List<BuilderAction>  Actions(int moduleId) => _repo.Actions(moduleId);
        public List<BuilderFilter>  Filters(int moduleId) => _repo.Filters(moduleId);

        public List<(int Id, string Display)> PickerRows(string table, string displayColumn) =>
            _repo.PickerRows(table, displayColumn);

        public void EnsureBuiltTable(BuilderModule module, List<BuilderColumn> columns) =>
            _repo.EnsureBuiltTable(module, columns);

        public void SeedSections(IEnumerable<(string Key, string Title, string IconKey, string[] Modules)> coded,
                                 IReadOnlyCollection<string> protectedKeys)
        {
            // المفاتيح كلها بما فيها المحذوفة — كالصفحات تماماً، وإلا عاد القسم الذي حذفه المستخدم
            // عند كل إقلاع بينما صفحاته تحترم حذفها.
            var existing = _repo.SectionKeys().ToHashSet();

            var order = 0;
            foreach (var section in coded)
            {
                order += 10;
                if (protectedKeys.Contains(section.Key) || existing.Contains(section.Key)) continue;

                _repo.SaveSection(new BuilderSection
                {
                    Key = section.Key, Title = section.Title, IconKey = section.IconKey,
                    SortOrder = order, Modules = string.Join(",", section.Modules)
                });
            }
        }

        public void SeedModules(IEnumerable<CodedPage> pages)
        {
            var sections = _repo.Sections().ToDictionary(s => s.Key, s => s.Id);
            var existing = _repo.ModuleKeys().ToHashSet();
            var seeded = _repo.Modules().ToDictionary(m => m.Key, m => m.Id);

            var order = 0;
            foreach (var page in pages)
            {
                order += 10;
                if (existing.Contains(page.Key))
                {
                    if (seeded.TryGetValue(page.Key, out var seededId)) Backfill(seededId, page);
                    continue;
                }

                if (!sections.TryGetValue(page.SectionKey, out var sectionId)) continue;

                var id = _repo.SaveModule(new BuilderModule
                {
                    Key = page.Key, Title = page.Title, Kind = page.Kind,
                    SectionId = sectionId, SortOrder = order, IsActive = true, IsCoded = true
                });

                _repo.ReplaceColumns(id, page.Columns);
                _repo.ReplaceActions(id, page.Actions);
                _repo.ReplaceFilters(id, page.Filters);
            }
        }

        private void Backfill(int moduleId, CodedPage page)
        {
            if (page.Columns.Count > 0 && _repo.Columns(moduleId).Count == 0)
                _repo.ReplaceColumns(moduleId, page.Columns);

            if (page.Filters.Count > 0 && _repo.Filters(moduleId).Count == 0)
                _repo.ReplaceFilters(moduleId, page.Filters);
        }
    }
}
