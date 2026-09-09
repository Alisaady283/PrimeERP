using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using PrimeERP.Data.Query;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    public interface IBuilderRepository
    {
        void CreateTables();

        List<BuilderSection> Sections();
        List<BuilderModule>  Modules();
        /// <summary>كل مفاتيح الصفحات بما فيها المحذوفة — البذر لا يُعيد ما حذفه المستخدم.</summary>
        List<string> ModuleKeys();

        List<BuilderColumn>  Columns(int moduleId = 0);
        List<BuilderAction>  Actions(int moduleId = 0);
        List<BuilderFilter>  Filters(int moduleId = 0);

        int  SaveSection(BuilderSection section);
        int  SaveModule(BuilderModule module);
        void ReplaceColumns(int moduleId, List<BuilderColumn> columns);
        void ReplaceActions(int moduleId, List<BuilderAction> actions);
        void ReplaceFilters(int moduleId, List<BuilderFilter> filters);
        void DeleteModule(int moduleId);

        /// <summary>يُنشئ جدول الوحدة (ورأس/سطور للحركة) من أعمدتها — يُستدعى عند الحفظ وعند كل إقلاع.</summary>
        void EnsureBuiltTable(BuilderModule module, List<BuilderColumn> columns);

        /// <summary>صفوف قائمة من جدول مبنيّ: المُعرِّف وعمود العرض — تستهلكها القائمة العامّة "Table:".</summary>
        List<(int Id, string Display)> PickerRows(string table, string displayColumn);
    }

    /// <summary>
    /// وصف ما بناه المستخدم. الجداول المبنيّة تُنشأ بـ SchemaBuilder نفسه الذي تُنشأ به جداول النظام،
    /// فلا مسار ثانٍ للإنشاء ولا فرق بين جدولٍ كتبناه وجدولٍ بناه المستخدم.
    /// </summary>
    public class BuilderRepository : RepositoryBase<BuilderModule>, IBuilderRepository
    {
        protected override string TableName => "BuilderModules";

        protected override BuilderModule Map(System.Data.DataRow row) => MapModule(row);

        public void CreateTables()
        {
            SchemaBuilder.Table("BuilderSections").Id()
                .Text("Key", 60, required: true, unique: true).Text("Title", 120, required: true)
                .Text("IconKey", 60).Int("SortOrder", nullable: false, defaultValue: 0).Text("Modules", 2000)
                .Audit().SoftDelete().Create();

            SchemaBuilder.Table("BuilderModules").Id()
                .Text("Key", 60, required: true, unique: true).Text("Title", 160, required: true)
                .Int("Kind", nullable: false, defaultValue: 0).Int("SectionId", nullable: false, defaultValue: 0)
                .Text("TableName", 60).Text("LineTable", 60).Text("SourceKey", 60).Text("CopiedFrom", 60)
                .Int("SortOrder", nullable: false, defaultValue: 0).Bool("IsActive", true).Bool("IsCoded")
                .Audit().SoftDelete().Index("SectionId").Create();

            SchemaBuilder.Table("BuilderColumns").Id()
                .Int("ModuleId", nullable: false).Text("Name", 60, required: true).Text("Header", 120, required: true)
                .Int("DataType", nullable: false, defaultValue: 0)
                .Bool("IsRequired").Bool("IsUnique").Int("MaxLength")
                .Text("RefModule", 60).Text("RefDisplay", 60)
                .Int("Aggregate", nullable: false, defaultValue: 0)
                .Text("AggFrom", 60).Text("AggColumn", 60).Text("AggMatch", 60)
                .Bool("ShowInGrid", true).Bool("ShowInForm", true).Bool("IsLine")
                .Decimal("Width", 8, 2, 140).Text("Footer", 20)
                .Int("SortOrder", nullable: false, defaultValue: 0)
                .Audit().SoftDelete().Index("ModuleId").Create();

            SchemaBuilder.Table("BuilderActions").Id()
                .Int("ModuleId", nullable: false).Text("ActionKey", 40, required: true).Bool("OnTable")
                .Int("SortOrder", nullable: false, defaultValue: 0)
                .Audit().SoftDelete().Index("ModuleId").Create();

            SchemaBuilder.Table("BuilderFilters").Id()
                .Int("ModuleId", nullable: false).Text("Key", 60, required: true).Text("Label", 120)
                .Text("Kind", 20).Text("RefModule", 60)
                .Int("SortOrder", nullable: false, defaultValue: 0)
                .Audit().SoftDelete().Index("ModuleId").Create();
        }

        // ===================== القراءة =====================

        public List<BuilderSection> Sections() => QueryAs(MapSection,
            $"SELECT * FROM BuilderSections WHERE IsDeleted = @d {OrderBuilder.By("SortOrder", false, "Title")}", null, null, ("@d", false));

        public List<BuilderModule> Modules() => QueryAs(MapModule,
            $"SELECT * FROM BuilderModules WHERE IsDeleted = @d {OrderBuilder.By("SortOrder", false, "Title")}", null, null, ("@d", false));

        public List<string> ModuleKeys() => QueryAs(r => Str(r, "Key"), "SELECT Key FROM BuilderModules");

        public List<BuilderColumn> Columns(int moduleId = 0) => Children(MapColumn, "BuilderColumns", moduleId);
        public List<BuilderAction> Actions(int moduleId = 0) => Children(MapAction, "BuilderActions", moduleId);
        public List<BuilderFilter> Filters(int moduleId = 0) => Children(MapFilter, "BuilderFilters", moduleId);

        private List<T> Children<T>(System.Func<System.Data.DataRow, T> map, string table, int moduleId)
        {
            var where = new WhereBuilder().Eq("IsDeleted", false).Eq("ModuleId", moduleId == 0 ? null : moduleId);
            return QueryAs(map, $"SELECT * FROM {table} {where.Sql} {OrderBuilder.By("SortOrder", false)}",
                null, null, where.Parameters);
        }

        public List<(int Id, string Display)> PickerRows(string table, string displayColumn) =>
            QueryAs(r => (System.Convert.ToInt32(r["Id"]), r[displayColumn]?.ToString()),
                $"SELECT Id, {displayColumn} FROM {table} WHERE IsDeleted = @d {OrderBuilder.By(displayColumn, false)}",
                null, null, ("@d", false));

        // ===================== الكتابة =====================

        public int SaveSection(BuilderSection s)
        {
            var p = new (string, object)[]
            {
                ("@k", s.Key), ("@t", s.Title), ("@i", s.IconKey ?? ""), ("@o", s.SortOrder),
                ("@m", s.Modules ?? ""), ("@now", System.DateTime.Now)
            };

            if (s.Id == 0)
                return InsertGetId("INSERT INTO BuilderSections (Key, Title, IconKey, SortOrder, Modules, CreatedAt, CreatedBy) VALUES (@k,@t,@i,@o,@m,@now,@by)",
                    null, null, p.Append(("@by", (object)(s.CreatedBy ?? ""))).ToArray());

            Exec("UPDATE BuilderSections SET Key=@k, Title=@t, IconKey=@i, SortOrder=@o, Modules=@m, UpdatedAt=@now WHERE Id=@id",
                null, null, p.Append(("@id", (object)s.Id)).ToArray());
            return s.Id;
        }

        public int SaveModule(BuilderModule m)
        {
            if (m.Id == 0)
                return InsertGetId(@"INSERT INTO BuilderModules (Key, Title, Kind, SectionId, TableName, LineTable, SourceKey,
                                            CopiedFrom, SortOrder, IsActive, IsCoded, CreatedAt, CreatedBy)
                                     VALUES (@k,@t,@kind,@sec,@tbl,@line,@src,@copy,@o,@a,@coded,@now,@by)",
                    null, null, ModuleParams(m).Append(("@by", (object)(m.CreatedBy ?? ""))).ToArray());

            Exec(@"UPDATE BuilderModules SET Key=@k, Title=@t, Kind=@kind, SectionId=@sec, TableName=@tbl,
                          LineTable=@line, SourceKey=@src, CopiedFrom=@copy, SortOrder=@o, IsActive=@a, IsCoded=@coded, UpdatedAt=@now
                   WHERE Id=@id", null, null, ModuleParams(m).Append(("@id", (object)m.Id)).ToArray());
            return m.Id;
        }

        private static IEnumerable<(string, object)> ModuleParams(BuilderModule m) => new (string, object)[]
        {
            ("@k", m.Key), ("@t", m.Title), ("@kind", (int)m.Kind), ("@sec", m.SectionId),
            ("@tbl", m.TableName ?? ""), ("@line", m.LineTable ?? ""), ("@src", m.SourceKey ?? ""),
            ("@copy", m.CopiedFrom ?? ""), ("@o", m.SortOrder), ("@a", m.IsActive), ("@coded", m.IsCoded),
        ("@now", System.DateTime.Now)
        };

        public void ReplaceColumns(int moduleId, List<BuilderColumn> columns)
        {
            Exec("DELETE FROM BuilderColumns WHERE ModuleId = @m", null, null, ("@m", moduleId));
            foreach (var c in columns)
                Exec(@"INSERT INTO BuilderColumns (ModuleId, Name, Header, DataType, IsRequired, IsUnique, MaxLength,
                              RefModule, RefDisplay, Aggregate, AggFrom, AggColumn, AggMatch,
                              ShowInGrid, ShowInForm, IsLine, Width, Footer, SortOrder, CreatedAt)
                       VALUES (@m,@n,@h,@dt,@req,@uni,@len,@rm,@rd,@agg,@af,@ac,@am,@sg,@sf,@il,@w,@f,@o,@now)",
                    null, null,
                    ("@m", moduleId), ("@n", c.Name), ("@h", c.Header), ("@dt", (int)c.DataType),
                    ("@req", c.IsRequired), ("@uni", c.IsUnique), ("@len", (object)c.MaxLength ?? System.DBNull.Value),
                    ("@rm", c.RefModule ?? ""), ("@rd", c.RefDisplay ?? ""), ("@agg", (int)c.Aggregate),
                    ("@af", c.AggFrom ?? ""), ("@ac", c.AggColumn ?? ""), ("@am", c.AggMatch ?? ""),
                    ("@sg", c.ShowInGrid), ("@sf", c.ShowInForm), ("@il", c.IsLine),
                    ("@w", c.Width), ("@f", c.Footer ?? ""), ("@o", c.SortOrder), ("@now", System.DateTime.Now));
        }

        public void ReplaceActions(int moduleId, List<BuilderAction> actions)
        {
            Exec("DELETE FROM BuilderActions WHERE ModuleId = @m", null, null, ("@m", moduleId));
            foreach (var a in actions)
                Exec("INSERT INTO BuilderActions (ModuleId, ActionKey, OnTable, SortOrder, CreatedAt) VALUES (@m,@k,@t,@o,@now)",
                    null, null, ("@m", moduleId), ("@k", a.ActionKey), ("@t", a.OnTable), ("@o", a.SortOrder), ("@now", System.DateTime.Now));
        }

        public void ReplaceFilters(int moduleId, List<BuilderFilter> filters)
        {
            Exec("DELETE FROM BuilderFilters WHERE ModuleId = @m", null, null, ("@m", moduleId));
            foreach (var f in filters)
                Exec("INSERT INTO BuilderFilters (ModuleId, Key, Label, Kind, RefModule, SortOrder, CreatedAt) VALUES (@m,@k,@l,@kind,@r,@o,@now)",
                    null, null, ("@m", moduleId), ("@k", f.Key), ("@l", f.Label ?? ""), ("@kind", f.Kind ?? "Combo"),
                    ("@r", f.RefModule ?? ""), ("@o", f.SortOrder), ("@now", System.DateTime.Now));
        }

        public void DeleteModule(int moduleId)
        {
            // الوصف يُحذف، والجدول المبنيّ يبقى — حذف بياناتٍ أدخلها المستخدم لا يكون أثراً جانبياً لحذف شاشة.
            foreach (var table in new[] { "BuilderColumns", "BuilderActions", "BuilderFilters" })
                Exec($"DELETE FROM {table} WHERE ModuleId = @m", null, null, ("@m", moduleId));

            SoftDelete(moduleId);
        }

        // ===================== الجدول المبنيّ =====================

        public void EnsureBuiltTable(BuilderModule module, List<BuilderColumn> columns)
        {
            if (module.Kind == BuilderKind.Report || string.IsNullOrWhiteSpace(module.TableName)) return;

            Build(module.TableName, columns.Where(c => !c.IsLine));

            if (module.Kind == BuilderKind.Movement && !string.IsNullOrWhiteSpace(module.LineTable))
                Build(module.LineTable, columns.Where(c => c.IsLine), header: module.TableName);
        }

        private static void Build(string table, IEnumerable<BuilderColumn> columns, string header = null)
        {
            var schema = SchemaBuilder.Table(table).Id();

            if (header != null)
            {
                schema.Int("HeaderId", nullable: false).Int("LineNo", nullable: false, defaultValue: 1);
                schema.ForeignKey("HeaderId", header);
                schema.Index("HeaderId");
            }

            foreach (var c in columns.Where(c => c.Aggregate == BuilderAggregate.None))
                switch (c.DataType)
                {
                    case BuilderDataType.Text:      schema.Text(c.Name, c.MaxLength ?? 200, c.IsRequired, c.IsUnique); break;
                    case BuilderDataType.LongText:  schema.Text(c.Name, c.MaxLength ?? 2000, c.IsRequired); break;
                    case BuilderDataType.Number:    schema.Decimal(c.Name); break;
                    case BuilderDataType.Money:     schema.Decimal(c.Name); break;
                    case BuilderDataType.Date:      schema.DateCol(c.Name, !c.IsRequired); break;
                    case BuilderDataType.Bool:      schema.Bool(c.Name); break;
                    case BuilderDataType.Reference: schema.Int(c.Name, !c.IsRequired); schema.Index(c.Name); break;
                }

            schema.Audit().SoftDelete().Create();
        }

        // ===================== التحويل =====================

        private static BuilderSection MapSection(System.Data.DataRow r) => new()
        {
            Id = Int(r, "Id"), Key = Str(r, "Key"), Title = Str(r, "Title"),
            IconKey = Str(r, "IconKey"), SortOrder = Int(r, "SortOrder"), Modules = Str(r, "Modules")
        };

        private static BuilderModule MapModule(System.Data.DataRow r) => new()
        {
            Id = Int(r, "Id"), Key = Str(r, "Key"), Title = Str(r, "Title"),
            Kind = (BuilderKind)Int(r, "Kind"), SectionId = Int(r, "SectionId"),
            TableName = Str(r, "TableName"), LineTable = Str(r, "LineTable"),
            SourceKey = Str(r, "SourceKey"), CopiedFrom = Str(r, "CopiedFrom"),
            SortOrder = Int(r, "SortOrder"), IsActive = Bool(r, "IsActive"), IsCoded = Bool(r, "IsCoded")
        };

        private static BuilderColumn MapColumn(System.Data.DataRow r) => new()
        {
            Id = Int(r, "Id"), ModuleId = Int(r, "ModuleId"), Name = Str(r, "Name"), Header = Str(r, "Header"),
            DataType = (BuilderDataType)Int(r, "DataType"), IsRequired = Bool(r, "IsRequired"), IsUnique = Bool(r, "IsUnique"),
            MaxLength = r["MaxLength"] == System.DBNull.Value ? null : Int(r, "MaxLength"),
            RefModule = Str(r, "RefModule"), RefDisplay = Str(r, "RefDisplay"),
            Aggregate = (BuilderAggregate)Int(r, "Aggregate"),
            AggFrom = Str(r, "AggFrom"), AggColumn = Str(r, "AggColumn"), AggMatch = Str(r, "AggMatch"),
            ShowInGrid = Bool(r, "ShowInGrid"), ShowInForm = Bool(r, "ShowInForm"), IsLine = Bool(r, "IsLine"),
            Width = System.Convert.ToDouble(r["Width"]), Footer = Str(r, "Footer"), SortOrder = Int(r, "SortOrder")
        };

        private static BuilderAction MapAction(System.Data.DataRow r) => new()
        {
            Id = Int(r, "Id"), ModuleId = Int(r, "ModuleId"), ActionKey = Str(r, "ActionKey"),
            OnTable = Bool(r, "OnTable"), SortOrder = Int(r, "SortOrder")
        };

        private static BuilderFilter MapFilter(System.Data.DataRow r) => new()
        {
            Id = Int(r, "Id"), ModuleId = Int(r, "ModuleId"), Key = Str(r, "Key"), Label = Str(r, "Label"),
            Kind = Str(r, "Kind"), RefModule = Str(r, "RefModule"), SortOrder = Int(r, "SortOrder")
        };

        private static int    Int(System.Data.DataRow r, string c)  => System.Convert.ToInt32(r[c]);
        private static string Str(System.Data.DataRow r, string c)  => r[c]?.ToString();
        private static bool   Bool(System.Data.DataRow r, string c) => System.Convert.ToBoolean(r[c]);
    }
}
