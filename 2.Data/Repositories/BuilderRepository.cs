using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع ما بناه المستخدم</summary>
    public interface IBuilderRepository
    {

        List<BuilderSection> Sections();
        List<BuilderModule>  Modules();
        List<string> SectionKeys();
        List<string> ModuleKeys();

        List<BuilderColumn>  Columns(int moduleId = 0);
        List<BuilderAction>  Actions(int moduleId = 0);
        List<BuilderFilter>  Filters(int moduleId = 0);

        int  SaveSection(BuilderSection section);
        int  SaveModule(BuilderModule module);
        int  SaveColumn(int moduleId, BuilderColumn column);
        int  SaveAction(int moduleId, BuilderAction action);
        int  SaveFilter(int moduleId, BuilderFilter filter);
        void DeleteColumn(int id);
        void DeleteAction(int id);
        void DeleteFilter(int id);
        void ReplaceColumns(int moduleId, List<BuilderColumn> columns);
        void ReplaceActions(int moduleId, List<BuilderAction> actions);
        void ReplaceFilters(int moduleId, List<BuilderFilter> filters);
        void DeleteModule(int moduleId);
        void DeleteSection(int sectionId);

        void EnsureBuiltTable(BuilderModule module, List<BuilderColumn> columns);

        List<(int Id, string Display)> PickerRows(string table, string displayColumn);
    }

    public class BuilderRepository : RepositoryBase<BuilderModule>, IBuilderRepository
    {
        protected override string TableName => "BuilderModules";



        public List<BuilderSection> Sections() =>
            FetchOf<BuilderSection>("BuilderSections",
                q => q.Where(s => !s.IsDeleted).OrderBy(s => s.SortOrder).ThenBy(s => s.Title));

        public List<BuilderModule> Modules() =>
            Fetch(q => q.Where(m => !m.IsDeleted).OrderBy(m => m.SortOrder).ThenBy(m => m.Title));

        public List<string> SectionKeys() =>
            FetchOf<BuilderSection>("BuilderSections", q => q).Select(s => s.Key).ToList();

        public List<string> ModuleKeys() => Fetch(q => q).Select(m => m.Key).ToList();

        public List<BuilderColumn> Columns(int moduleId = 0) =>
            FetchOf<BuilderColumn>("BuilderColumns", q => Children(q, moduleId).OrderBy(c => c.SortOrder));

        public List<BuilderAction> Actions(int moduleId = 0) =>
            FetchOf<BuilderAction>("BuilderActions", q => Children(q, moduleId).OrderBy(a => a.SortOrder));

        public List<BuilderFilter> Filters(int moduleId = 0) =>
            FetchOf<BuilderFilter>("BuilderFilters", q => Children(q, moduleId).OrderBy(f => f.SortOrder));

        /// <summary>أبناء وحدةٍ بعينها</summary>
        private static IQueryable<T> Children<T>(IQueryable<T> rows, int moduleId) where T : BuilderChild =>
            rows.Where(x => !x.IsDeleted && (moduleId == 0 || x.ModuleId == moduleId));

        public List<(int Id, string Display)> PickerRows(string table, string displayColumn)
        {
            using var db = DbContextFactory.Open();
            return db.Rows(table)
                .Where(r => !(bool)r["IsDeleted"])
                .OrderBy(r => r[displayColumn])
                .AsEnumerable()
                .Select(r => ((int)r["Id"], r[displayColumn]?.ToString()))
                .ToList();
        }

        public int SaveSection(BuilderSection s) =>
            Write(db =>
            {
                var set = SetOf<BuilderSection>(db, "BuilderSections");
                if (s.Id == 0)
                {
                    s.CreatedAt = DateTime.Now;
                    set.Add(s);
                    return 0;
                }

                var row = set.FirstOrDefault(x => x.Id == s.Id);
                if (row == null) return 0;
                row.Key = s.Key;
                row.Title = s.Title;
                row.IconKey = s.IconKey ?? "";
                row.SortOrder = s.SortOrder;
                row.Modules = s.Modules ?? "";
                row.UpdatedAt = DateTime.Now;
                return 0;
            }) is var _ ? s.Id : s.Id;

        public int SaveModule(BuilderModule m) =>
            Write(db =>
            {
                if (m.Id == 0)
                {
                    m.CreatedAt = DateTime.Now;
                    SetOf(db).Add(m);
                    return 0;
                }

                var row = Rows(db).AsTracking().FirstOrDefault(x => x.Id == m.Id);
                if (row == null) return 0;
                row.Key = m.Key;
                row.Title = m.Title;
                row.Kind = m.Kind;
                row.SectionId = m.SectionId;
                row.TableName = m.TableName ?? "";
                row.LineTable = m.LineTable ?? "";
                row.SourceKey = m.SourceKey ?? "";
                row.CopiedFrom = m.CopiedFrom ?? "";
                row.SortOrder = m.SortOrder;
                row.IsActive = m.IsActive;
                row.IsCoded = m.IsCoded;
                row.UpdatedAt = DateTime.Now;
                return 0;
            }) is var _ ? m.Id : m.Id;

        public int SaveColumn(int moduleId, BuilderColumn c) => SaveChild("BuilderColumns", moduleId, c);
        public int SaveAction(int moduleId, BuilderAction a) => SaveChild("BuilderActions", moduleId, a);
        public int SaveFilter(int moduleId, BuilderFilter f) => SaveChild("BuilderFilters", moduleId, f);

        /// <summary>الأبناء الثلاثة يُحفظون بنفس الآلية</summary>
        private int SaveChild<T>(string table, int moduleId, T child) where T : BuilderChild
        {
            child.ModuleId = moduleId;
            Write(db =>
            {
                var set = SetOf<T>(db, table);
                if (child.Id == 0)
                {
                    child.CreatedAt = DateTime.Now;
                    set.Add(child);
                    return 0;
                }

                var row = set.FirstOrDefault(x => x.Id == child.Id);
                if (row != null) db.Entry(row).CurrentValues.SetValues(child);
                return 0;
            });
            return child.Id;
        }

        public void ReplaceColumns(int moduleId, List<BuilderColumn> columns) =>
            Replace("BuilderColumns", moduleId, columns, (id, c) => SaveColumn(id, c));

        public void ReplaceActions(int moduleId, List<BuilderAction> actions) =>
            Replace("BuilderActions", moduleId, actions, (id, a) => SaveAction(id, a));

        public void ReplaceFilters(int moduleId, List<BuilderFilter> filters) =>
            Replace("BuilderFilters", moduleId, filters, (id, f) => SaveFilter(id, f));

        private void Replace<T>(string table, int moduleId, List<T> rows, Action<int, T> save) where T : BuilderChild
        {
            Clear<T>(table, moduleId);
            foreach (var row in rows) { row.Id = 0; save(moduleId, row); }
        }

        private void Clear<T>(string table, int moduleId) where T : BuilderChild =>
            Write(db =>
            {
                SetOf<T>(db, table).RemoveRange(RowsOf<T>(db, table).Where(x => x.ModuleId == moduleId));
                return 0;
            });

        public void DeleteColumn(int id) => Remove<BuilderColumn>("BuilderColumns", id);
        public void DeleteAction(int id) => Remove<BuilderAction>("BuilderActions", id);
        public void DeleteFilter(int id) => Remove<BuilderFilter>("BuilderFilters", id);

        private void Remove<T>(string table, int id) where T : class =>
            Write(db =>
            {
                var row = RowsOf<T>(db, table).AsTracking().FirstOrDefault(x => EF.Property<int>(x, "Id") == id);
                if (row != null) SetOf<T>(db, table).Remove(row);
                return 0;
            });

        public void DeleteSection(int sectionId) =>
            Write(db =>
            {
                var row = RowsOf<BuilderSection>(db, "BuilderSections").AsTracking().FirstOrDefault(s => s.Id == sectionId);
                if (row == null) return 0;
                row.IsDeleted = true;
                row.DeletedAt = DateTime.Now;
                return 0;
            });

        public void DeleteModule(int moduleId)
        {
            Clear<BuilderColumn>("BuilderColumns", moduleId);
            Clear<BuilderAction>("BuilderActions", moduleId);
            Clear<BuilderFilter>("BuilderFilters", moduleId);

            Write(db =>
            {
                var row = Rows(db).AsTracking().FirstOrDefault(m => m.Id == moduleId);
                if (row == null) return 0;
                row.IsDeleted = true;
                row.DeletedAt = DateTime.Now;
                return 0;
            });
        }

        public void EnsureBuiltTable(BuilderModule module, List<BuilderColumn> columns)
        {
            if (module.Kind == BuilderKind.Report || string.IsNullOrWhiteSpace(module.TableName)) return;

            using var db = DbContextFactory.Open();
            db.CreateBuiltTable(module.TableName, columns.Where(c => !c.IsLine).ToList());

            if (module.Kind == BuilderKind.Movement && !string.IsNullOrWhiteSpace(module.LineTable))
                db.CreateBuiltTable(module.LineTable, columns.Where(c => c.IsLine).ToList(), module.TableName);
        }
    }
}
