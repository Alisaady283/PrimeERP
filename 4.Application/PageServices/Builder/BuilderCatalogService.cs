using PrimeERP.Platform.Localization;
using PrimeERP.Application.Services.Entities;
using System.Collections.Generic;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using System.Linq;

namespace PrimeERP.Application.PageServices.Builder
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
            var orders = _repo.Sections().ToDictionary(s => s.Key, s => s.SortOrder);

            string previous = null;
            foreach (var section in coded)
            {
                if (!existing.Contains(section.Key))
                {
                    var order = (previous != null && orders.TryGetValue(previous, out var after) ? after : 0) + 5;
                    orders[section.Key] = order;
                    _repo.SaveSection(new BuilderSection
                    {
                        Key = section.Key, Title = section.Title, IconKey = section.IconKey, SortOrder = order,
                        IsProtected = protectedKeys.Contains(section.Key), Modules = string.Join(",", section.Modules)
                    });
                }
                previous = section.Key;
            }

            var english = LocalizationService.CurrentLanguage == AppLanguage.En;
            var titles = coded.ToDictionary(c => c.Key, c => c.Title);
            foreach (var (section, i) in _repo.Sections().OrderBy(s => s.SortOrder).ThenBy(s => s.Id).Select((s, i) => (s, i)))
            {
                var translate = english && string.IsNullOrWhiteSpace(section.TitleEn) && titles.ContainsKey(section.Key);
                if (section.SortOrder == (i + 1) * 10 && !translate) continue;

                section.SortOrder = (i + 1) * 10;
                if (translate) section.TitleEn = titles[section.Key];
                _repo.SaveSection(section);
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
            var columned = _repo.Columns(0).Select(c => c.ModuleId).ToHashSet();
            var filtered = _repo.Filters(0).Select(f => f.ModuleId).ToHashSet();
            var orders = _repo.Modules().ToDictionary(m => m.Key, m => m.SortOrder);
            var rows = _repo.Modules().ToDictionary(m => m.Id);

            string previous = null, previousSection = null;
            foreach (var page in coded)
            {
                if (page.SectionKey != previousSection) (previous, previousSection) = (null, page.SectionKey);

                if (existing.Contains(page.Key))
                {
                    if (seeded.TryGetValue(page.Key, out var seededId))
                    {
                        Backfill(seededId, page, columned, filtered);
                        if (rows.TryGetValue(seededId, out var row)) Translate(row, page);
                    }
                    previous = page.Key;
                    continue;
                }

                if (!sections.TryGetValue(page.SectionKey, out var sectionId)) continue;

                var order = (previous != null && orders.TryGetValue(previous, out var after) ? after : 0) + 5;
                orders[page.Key] = order;
                previous = page.Key;

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

            foreach (var section in _repo.Modules().GroupBy(m => m.SectionId))
                foreach (var (module, i) in section.OrderBy(m => m.SortOrder).ThenBy(m => m.Id).Select((m, i) => (m, i)))
                    if (module.SortOrder != (i + 1) * 10)
                    {
                        module.SortOrder = (i + 1) * 10;
                        _repo.SaveModule(module);
                    }
        }

        private void Translate(BuilderModule module, CodedPage page)
        {
            if (LocalizationService.CurrentLanguage != AppLanguage.En) return;

            if (string.IsNullOrWhiteSpace(module.TitleEn))
            {
                module.TitleEn = page.Title;
                _repo.SaveModule(module);
            }

            var headers = page.Columns.Where(c => c.Name != null).GroupBy(c => c.Name).ToDictionary(g => g.Key, g => g.First().Header);
            foreach (var column in _repo.Columns(module.Id).Where(c => string.IsNullOrWhiteSpace(c.HeaderEn) && headers.ContainsKey(c.Name ?? "")))
            {
                column.HeaderEn = headers[column.Name];
                _repo.SaveColumn(module.Id, column);
            }

            var labels = page.Filters.Where(f => f.Key != null).GroupBy(f => f.Key).ToDictionary(g => g.Key, g => g.First().Label);
            foreach (var filter in _repo.Filters(module.Id).Where(f => string.IsNullOrWhiteSpace(f.LabelEn) && labels.ContainsKey(f.Key ?? "")))
            {
                filter.LabelEn = labels[filter.Key];
                _repo.SaveFilter(module.Id, filter);
            }
        }

        private void Backfill(int moduleId, CodedPage page, HashSet<int> columned, HashSet<int> filtered)
        {
            if (page.Columns.Count > 0 && !columned.Contains(moduleId))
                _repo.ReplaceColumns(moduleId, page.Columns);

            if (page.Filters.Count > 0 && !filtered.Contains(moduleId))
                _repo.ReplaceFilters(moduleId, page.Filters);
        }
    }
}
