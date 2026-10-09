using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>مفاتيح الشريط الجانبي</summary>
    public class NavigationGroupsTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public NavigationGroupsTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void EveryRegisteredModule_AppearsInExactlyOneNavigationGroup()
        {
            var registry = _db.Services.GetRequiredService<IModuleRegistry>();
            var grouped = PrimeERP.Composition.Registry.NavigationMap.Groups().SelectMany(g => g.Keys).ToList();

            var tabbed = registry.All().Where(m => m.TabModules != null).SelectMany(m => m.TabModules).ToHashSet();
            var missing = registry.All().Select(m => m.Key).Where(k => !grouped.Contains(k) && !tabbed.Contains(k)).ToList();
            Assert.True(missing.Count == 0, "وحدات مسجَّلة ولا تظهر في الشريط الجانبي: " + string.Join(", ", missing));

            var duplicated = grouped.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.True(duplicated.Count == 0, "وحدات مكرّرة في أكثر من مجموعة: " + string.Join(", ", duplicated));

            var unknown = grouped.Where(k => registry.Get(k) == null).ToList();
            Assert.True(unknown.Count == 0, "مفاتيح في الشريط الجانبي بلا وحدة مسجَّلة: " + string.Join(", ", unknown));
        }
    }
}
