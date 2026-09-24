using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Builder;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>وصف ما بناه المستخدم</summary>
    public class BuilderCatalogTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public BuilderCatalogTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private T Service<T>() => _db.Services.GetRequiredService<T>();

        private static IDictionary<string, object> Row(params (string Key, object Value)[] fields)
        {
            IDictionary<string, object> row = new ExpandoObject();
            foreach (var (key, value) in fields) row[key] = value;
            return row;
        }

        private int NewSection(string key)
        {
            var created = Service<BuilderSectionsService>().Create(
                Row(("Id", 0), ("Key", key), ("Title", key), ("IconKey", "IconSettings"), ("SortOrder", 900)));

            Assert.True(created.IsSuccess, created.ErrorMessage);
            return Convert.ToInt32(created.Value["Id"]);
        }

        private int NewModule(int sectionId, string key) =>
            Convert.ToInt32(Service<BuilderModulesService>().Create(
                Row(("Id", 0), ("Key", key), ("Title", key), ("Kind", 1), ("SectionId", sectionId),
                    ("TableName", $"Built_{key}"), ("SortOrder", 10), ("IsActive", true))).Value["Id"]);

        private bool SectionExists(int id) =>
            Service<IBuilderCatalog>().Sections().Any(s => s.Id == id);

        [Fact]
        public void AnEmptySection_IsActuallyDeleted()
        {
            var id = NewSection("TestEmptySection");

            var deleted = Service<BuilderSectionsService>().Delete(id);

            Assert.True(deleted.IsSuccess, deleted.ErrorMessage);
            Assert.False(SectionExists(id), "القسم بقي بعد حذفٍ أُعلن نجاحه");
        }

        [Fact]
        public void ASectionWithPages_RefusesDeletion()
        {
            var id = NewSection("TestFullSection");
            NewModule(id, "TestPageInSection");

            var deleted = Service<BuilderSectionsService>().Delete(id);

            Assert.False(deleted.IsSuccess);
            Assert.True(SectionExists(id));
        }

        [Fact]
        public void AddingAColumn_ReturnsTheRowThatWasWritten()
        {
            var sectionId = NewSection("TestColumnsSection");
            var moduleId = NewModule(sectionId, "TestColumnsPage");
            var columns = Service<BuilderColumnsService>();

            columns.Create(Row(("Id", 0), ("ModuleId", moduleId), ("Name", "Last"), ("Header", "الأخير"),
                ("DataType", 0), ("SortOrder", 900), ("ShowInGrid", true)));

            var created = columns.Create(Row(("Id", 0), ("ModuleId", moduleId), ("Name", "First"), ("Header", "الأول"),
                ("DataType", 0), ("SortOrder", 10), ("ShowInGrid", true)));

            Assert.True(created.IsSuccess, created.ErrorMessage);
            Assert.Equal("First", created.Value["Name"]);
        }

        [Fact]
        public void EditingOneColumn_KeepsTheOthersIdentities()
        {
            var sectionId = NewSection("TestStableSection");
            var moduleId = NewModule(sectionId, "TestStablePage");
            var columns = Service<BuilderColumnsService>();

            var kept = Convert.ToInt32(columns.Create(Row(("Id", 0), ("ModuleId", moduleId), ("Name", "Kept"),
                ("Header", "يبقى"), ("DataType", 0), ("SortOrder", 10), ("ShowInGrid", true))).Value["Id"]);

            var edited = Convert.ToInt32(columns.Create(Row(("Id", 0), ("ModuleId", moduleId), ("Name", "Edited"),
                ("Header", "يُعدَّل"), ("DataType", 0), ("SortOrder", 20), ("ShowInGrid", true))).Value["Id"]);

            columns.Update(Row(("Id", edited), ("ModuleId", moduleId), ("Name", "Edited"), ("Header", "عُدِّل"),
                ("DataType", 0), ("SortOrder", 20), ("ShowInGrid", true)));

            Assert.Contains(Service<IBuilderCatalog>().Columns(moduleId), c => c.Id == kept);
        }

        [Fact]
        public void ANewRow_LandsAtTheEnd()
        {
            var moduleId = NewModule(NewSection("TestOrderSection"), "TestOrderPage");
            var columns = Service<BuilderColumnsService>();

            var first = columns.Create(Row(("Id", 0), ("ModuleId", moduleId), ("Name", "A"), ("Header", "أ"),
                ("DataType", 0), ("ShowInGrid", true)));
            var second = columns.Create(Row(("Id", 0), ("ModuleId", moduleId), ("Name", "B"), ("Header", "ب"),
                ("DataType", 0), ("ShowInGrid", true)));

            Assert.True(Convert.ToInt32(second.Value["SortOrder"]) > Convert.ToInt32(first.Value["SortOrder"]),
                "الصفّ الجديد لم يقع بعد سابقه");
        }

        [Fact]
        public void SwappingTwoOrders_FlipsTheirPlaces()
        {
            var moduleId = NewModule(NewSection("TestSwapSection"), "TestSwapPage");
            var columns = Service<BuilderColumnsService>();

            columns.Create(Row(("Id", 0), ("ModuleId", moduleId), ("Name", "Upper"), ("Header", "فوق"),
                ("DataType", 0), ("SortOrder", 30), ("ShowInGrid", true)));
            columns.Create(Row(("Id", 0), ("ModuleId", moduleId), ("Name", "Lower"), ("Header", "تحت"),
                ("DataType", 0), ("SortOrder", 40), ("ShowInGrid", true)));

            var before = Service<IBuilderCatalog>().Columns(moduleId);
            Assert.Equal("Upper", before[0].Name);

            var upper = Row(("Id", before[0].Id), ("ModuleId", moduleId), ("Name", "Upper"), ("Header", "فوق"),
                ("DataType", 0), ("SortOrder", 40), ("ShowInGrid", true));
            var lower = Row(("Id", before[1].Id), ("ModuleId", moduleId), ("Name", "Lower"), ("Header", "تحت"),
                ("DataType", 0), ("SortOrder", 30), ("ShowInGrid", true));

            Assert.True(columns.Update(upper).IsSuccess);
            Assert.True(columns.Update(lower).IsSuccess);

            var after = Service<IBuilderCatalog>().Columns(moduleId);
            Assert.Equal("Lower", after[0].Name);
            Assert.Equal(before[0].Id, after[1].Id);
        }

        [Theory]
        [InlineData("TrialBalance")]
        [InlineData("ItemCard")]
        public void ACodedReport_IsSeededWithItsColumnsAndParameters(string key)
        {
            var catalog = Service<IBuilderCatalog>();
            var report = catalog.Modules().FirstOrDefault(m => m.Key == key);

            Assert.NotNull(report);
            Assert.NotEmpty(catalog.Columns(report.Id));
            Assert.NotEmpty(catalog.Filters(report.Id));
        }

        [Fact]
        public void APageSeededEmpty_IsBackfilledOnTheNextStart()
        {
            var catalog = Service<IBuilderCatalog>();
            var repo = _db.Services.GetRequiredService<PrimeERP.Data.Repositories.IBuilderRepository>();
            var report = catalog.Modules().First(m => m.Key == "TrialBalance");

            repo.ReplaceColumns(report.Id, new List<PrimeERP.Domain.Entities.BuilderColumn>());
            Assert.Empty(catalog.Columns(report.Id));

            PrimeERP.Modules.BuilderModuleLoader.RegisterAll(
                _db.Services.GetRequiredService<PrimeERP.Composition.Registry.IModuleRegistry>(), _db.Services);

            Assert.NotEmpty(catalog.Columns(report.Id));
        }

        [Fact]
        public void TheSecondPage_DiffersFromTheFirst()
        {
            var sectionId = NewSection("TestPagingSection");
            var modules = Service<BuilderModulesService>();

            foreach (var index in Enumerable.Range(1, 5))
                NewModule(sectionId, $"TestPagingPage{index}");

            var first = modules.GetPaged(1, 2, new DynamicFilter { SectionId = sectionId });
            var second = modules.GetPaged(2, 2, new DynamicFilter { SectionId = sectionId });

            Assert.Equal(2, first.Value.Items.Count);
            Assert.Equal(2, second.Value.Items.Count);
            Assert.NotEqual(first.Value.Items[0]["Key"], second.Value.Items[0]["Key"]);
            Assert.Equal(5, first.Value.TotalCount);
        }
        [Fact]
        public void TheProtectedSections_AreSeededWithTheirPages_AndRefuseDeletion()
        {
            var catalog = Service<IBuilderCatalog>();

            var accounting = catalog.Sections().FirstOrDefault(s => s.Key == "Accounting");
            Assert.NotNull(accounting);
            Assert.True(accounting.IsProtected);

            var pages = catalog.Modules().Where(m => m.SectionId == accounting.Id).Select(m => m.Key).ToList();
            Assert.Contains("Accounts", pages);
            Assert.All(pages, key => Assert.NotEmpty(
                catalog.Columns(catalog.Modules().First(m => m.Key == key).Id)));

            var deleted = Service<BuilderSectionsService>().Delete(accounting.Id);
            Assert.False(deleted.IsSuccess);
        }

        [Fact]
        public void APageThatLeftTheCode_LeavesTheCatalogToo()
        {
            var catalog = Service<IBuilderCatalog>();
            var registry = _db.Services.GetRequiredService<PrimeERP.Composition.Registry.IModuleRegistry>();

            var section = catalog.Sections().First(s => s.Key == "Accounting");
            catalog.SeedModules(new[]
            {
                new CodedPage(section.Key, "GhostPage", "صفحة زائلة", PrimeERP.Domain.Enums.BuilderKind.Record,
                    new(), new(), new())
            });
            Assert.Contains(catalog.Modules(), m => m.Key == "GhostPage");

            PrimeERP.Modules.BuilderModuleLoader.RegisterAll(registry, _db.Services);

            Assert.DoesNotContain(catalog.Modules(), m => m.Key == "GhostPage");
        }
        [Fact]
        public void APageThatSavesRecords_IsRefusedWithoutATable()
        {
            var sectionId = NewSection("TestNoTableSection");

            var created = Service<BuilderModulesService>().Create(
                Row(("Id", 0), ("Key", "TestNoTablePage"), ("Title", "بلا جدول"), ("Kind", 1),
                    ("SectionId", sectionId), ("SortOrder", 10), ("IsActive", true)));

            Assert.False(created.IsSuccess);
            Assert.Contains("الجدول", created.ErrorMessage);
        }

        [Fact]
        public void APageBuiltByHand_GetsItsTable_AndKeepsTheRowSavedInIt()
        {
            var sectionId = NewSection("TestLiveSection");
            var moduleId = NewModule(sectionId, "TestLivePage");
            var columns = Service<BuilderColumnsService>();

            columns.Create(Row(("Id", 0), ("ModuleId", moduleId), ("Name", "Title"), ("Header", "العنوان"),
                ("DataType", 0), ("SortOrder", 10), ("ShowInGrid", true), ("ShowInForm", true)));
            columns.Create(Row(("Id", 0), ("ModuleId", moduleId), ("Name", "Amount"), ("Header", "المبلغ"),
                ("DataType", 2), ("SortOrder", 20), ("ShowInGrid", true), ("ShowInForm", true)));

            var catalog = Service<IBuilderCatalog>();
            var module = catalog.Modules().First(m => m.Id == moduleId);
            var built = catalog.Columns(moduleId);
            catalog.EnsureBuiltTable(module, built);

            var rows = new PrimeERP.Application.Services.Builder.DynamicEntityService(module, built,
                _db.Services.GetRequiredService<IPermissionService>(),
                _db.Services.GetRequiredService<PrimeERP.Platform.Settings.ISettingsProvider>(),
                _db.Services.GetRequiredService<PrimeERP.Platform.Localization.ILocalizationService>(),
                _db.Services.GetRequiredService<PrimeERP.Platform.Audit.IAuditLogger>());

            var created = rows.Create(Row(("Title", "سجل حقيقي"), ("Amount", 125.5m)));
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var page = rows.GetPaged(1, 20, new DynamicFilter());
            Assert.True(page.IsSuccess, page.ErrorMessage);
            Assert.Single(page.Value.Items);
            Assert.Equal("سجل حقيقي", page.Value.Items[0]["Title"]);
            Assert.Equal(125.5m, Convert.ToDecimal(page.Value.Items[0]["Amount"]));
        }
    }
}
