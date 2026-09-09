using System.Collections.Generic;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using System.Linq;

namespace PrimeERP.Application.Services.Builder
{
    /// <summary>
    /// وصفُ ما بناه المستخدم كما تقرؤه الطبقات العليا. المُصيِّرات وتسجيل الوحدات تستهلك هذه لا المستودع
    /// مباشرةً — نفس القيد الذي يمنع أي شاشة من استدعاء مستودع، ويفحصه check.sh.
    /// </summary>
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
            var existing = _repo.Sections().Select(s => s.Key).ToHashSet();

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
    }
}
