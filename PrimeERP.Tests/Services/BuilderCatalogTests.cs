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
    /// <summary>وصف ما بناه المستخدم: الحذف يحذف فعلاً، والكتابة تُرجع صفّها، والصفحة تُقطَع.</summary>
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
                Row(("Id", 0), ("Key", key), ("Title", key), ("Kind", 0), ("SectionId", sectionId),
                    ("SortOrder", 10), ("IsActive", true))).Value["Id"]);

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

        /// <summary>الرسالة تقول الحقيقة: قسمٌ به صفحات يُرفض حذفه ولا يُعلَن نجاحاً كاذباً.</summary>
        [Fact]
        public void ASectionWithPages_RefusesDeletion()
        {
            var id = NewSection("TestFullSection");
            NewModule(id, "TestPageInSection");

            var deleted = Service<BuilderSectionsService>().Delete(id);

            Assert.False(deleted.IsSuccess);
            Assert.True(SectionExists(id));
        }

        /// <summary>الكتابة تُرجع الصفّ المكتوب — كانت تُرجع صاحب أكبر ترتيب.</summary>
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

        /// <summary>تعديل عمودٍ لا يُعيد توليد معرّفات إخوته — كان كل حفظٍ يمسح الإخوة ويُدخلهم من جديد.</summary>
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

        /// <summary>صفٌّ جديد بلا رقم ترتيب يقع آخر القائمة — فالسهمان وحدهما ما يُعيد الترتيب.</summary>
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

        /// <summary>ما يفعله السهم: تبادل رقمين فينقلب موضع الصفّين، بلا إعادة ترقيم القائمة.</summary>
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

        /// <summary>التقرير صفحةٌ كغيرها: أعمدته وفلاتره تُبذَر من إعلانه — كانت تُقرأ من حقلٍ فارغ فتُبذَر صفراً.</summary>
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

        /// <summary>صفحةٌ مبذورة بلا أعمدة تُملأ في الإقلاع التالي — البذر كان يتخطّى كل مفتاحٍ موجود.</summary>
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

        /// <summary>الصفحة الثانية غير الأولى — كانت كل الصفحات تُرجع القائمة كاملةً نفسها.</summary>
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
    }
}
