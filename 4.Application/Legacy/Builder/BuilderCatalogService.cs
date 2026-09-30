using PrimeERP.Application.Services.Entities;
using System.Collections.Generic;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using System.Linq;

namespace PrimeERP.Application.Legacy.Builder
{
    /// <summary>وصفُ ما بناه المستخدم كما</summary>
    public record CodedPage(string SectionKey, string Key, string Title, BuilderKind Kind,
        List<BuilderColumn> Columns, List<BuilderAction> Actions, List<BuilderFilter> Filters);

    public interface IBuilderCatalog
    {
        List<BuilderSection> Sections();
        List<BuilderModule>  Modules();
        List<BuilderColumn>  Columns(int moduleId);
        List<BuilderAction>  Actions(int moduleId);
        List<BuilderFilter>  Filters(int moduleId);

        List<(int Id, string Display)> PickerRows(string table, string displayColumn);

        void EnsureBuiltTable(BuilderModule module, List<BuilderColumn> columns);

        void SeedSections(IEnumerable<(string Key, string Title, string IconKey, string[] Modules)> coded,
                          IReadOnlyCollection<string> protectedKeys);

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
            var existing = _repo.SectionKeys().ToHashSet();

            var order = 0;
            foreach (var section in coded)
            {
                order += 10;
                if (existing.Contains(section.Key)) continue;

                _repo.SaveSection(new BuilderSection
                {
                    Key = section.Key, Title = section.Title, IconKey = section.IconKey, SortOrder = order,
                    IsProtected = protectedKeys.Contains(section.Key), Modules = string.Join(",", section.Modules)
                });
            }
        }

        public void SeedModules(IEnumerable<CodedPage> pages)
        {
            var coded = pages.ToList();
            var live = coded.Select(p => p.Key).ToHashSet();

            foreach (var stale in _repo.Modules().Where(m => m.IsCoded && !live.Contains(m.Key)))
                _repo.DeleteModule(stale.Id);

            var sections = _repo.Sections().ToDictionary(s => s.Key, s => s.Id);
            var existing = _repo.ModuleKeys().ToHashSet();
            var seeded = _repo.Modules().ToDictionary(m => m.Key, m => m.Id);

            var order = 0;
            foreach (var page in coded)
            {
                order += 10;
                if (existing.Contains(page.Key))
                {
                    if (seeded.TryGetValue(page.Key, out var seededId)) Backfill(seededId, page);
                    continue;
                }

                if (!sections.TryGetValue(page.SectionKey, out var sectionId)) continue;

                var id = _repo.SaveModule(Rows.Copy(page, new BuilderModule(), to =>
                {
                    to.SectionId = sectionId;
                    to.SortOrder = order;
                    to.IsActive = true;
                    to.IsCoded = true;
                }));

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
