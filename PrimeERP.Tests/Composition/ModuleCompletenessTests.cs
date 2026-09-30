using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>حارس اكتمال الشاشة</summary>
    public class ModuleCompletenessTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public ModuleCompletenessTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private IModuleRegistry Registry => _db.Services.GetRequiredService<IModuleRegistry>();

        private static HashSet<string> MappedKeys() =>
            NavigationMap.Coded.SelectMany(section => section.Modules).ToHashSet();

        [Fact]
        public void EveryRegisteredModule_HasAKeyInTheNavigationMap()
        {
            var mapped = MappedKeys();
            var orphans = Registry.All().Select(m => m.Key).Where(key => !mapped.Contains(key)).ToList();

            Assert.True(orphans.Count == 0,
                "شاشات مسجَّلة بلا مفتاح في NavigationMap.Coded — لن تظهر في الشريط الجانبي ولا وحدة " +
                "البناء ولا إنشاء برنامج: " + string.Join(", ", orphans));
        }

        [Fact]
        public void EveryMappedKey_IsActuallyRegistered()
        {
            var dangling = MappedKeys().Where(key => Registry.Get(key) == null).ToList();

            Assert.True(dangling.Count == 0,
                "مفاتيح في NavigationMap بلا وحدة مسجَّلة — قسمٌ يعرض فراغاً: " + string.Join(", ", dangling));
        }

        [Fact]
        public void EveryDeclaredType_ResolvesFromTheContainer()
        {
            var unresolved = new List<string>();

            foreach (var module in Registry.All())
            {
                Resolve(module.Key, "نموذج العرض", module.ViewModelType, unresolved);
                Resolve(module.Key, "خدمة الحوار", module.Dialog?.ServiceType, unresolved);
                Resolve(module.Key, "خدمة المستند", module.DocumentDialog?.ServiceType, unresolved);
                Resolve(module.Key, "خدمة التقرير", module.Report?.ServiceType, unresolved);
            }

            Assert.True(unresolved.Count == 0,
                "أنواع مُعلَنة لا يحلّها الحاوي — ينقصها سطر في DependencyInjection: " + string.Join(" · ", unresolved));
        }

        private void Resolve(string key, string role, Type type, List<string> unresolved)
        {
            if (type == null || type == typeof(System.Dynamic.ExpandoObject)) return;

            if (_db.Services.GetService(type) == null) unresolved.Add($"{key}: {role} ({type.Name})");
        }

        [Fact]
        public void EveryPostingScreen_OffersDeletion()
        {
            var posting = new[] { "Assets", "AssetRevaluations", "Journals", "SalesInvoices", "PurchaseInvoices" };

            var missing = posting
                .Select(key => Registry.Get(key))
                .Where(module => module != null)
                .Where(module => module.EnabledActions != null && !module.EnabledActions.Contains("delete"))
                .Select(module => module.Key)
                .ToList();

            Assert.True(missing.Count == 0,
                "شاشات تُرحّل قيوداً بلا زرّ حذف يعكسها: " + string.Join(", ", missing));
        }

        [Fact]
        public void EveryModulePrefix_HasPermissionKeys()
        {
            var prefixes = PermissionKeys.All().Select(key => key.Split('.')[0]).ToHashSet();

            var uncovered = Registry.All()
                .Select(module => module.PermissionPrefix)
                .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
                .Distinct()
                .Where(prefix => !prefixes.Contains(prefix))
                .ToList();

            Assert.True(uncovered.Count == 0,
                "بادئات شاشات بلا أي مفتاح في PermissionKeys — لا تظهر في شجرة الصلاحيات فلا تُمنح: "
                + string.Join(", ", uncovered));
        }

        [Fact]
        public void EveryServicePermissionKey_IsDefined()
        {
            var defined = PermissionKeys.All().ToHashSet();

            var gated = Registry.All()
                .Select(module => module.DocumentDialog?.ServiceType)
                .Where(type => type != null)
                .Distinct()
                .Select(type => _db.Services.GetService(type))
                .OfType<PrimeERP.Application.Legacy.Inventory.IPermissionGated>()
                .Select(service => service.PermissionKey)
                .Distinct()
                .Where(key => !defined.Contains(key))
                .ToList();

            Assert.True(gated.Count == 0,
                "خدمات تسأل عن مفاتيح غير مُعرَّفة في PermissionKeys — تُرفض دائماً: " + string.Join(", ", gated));
        }
    }
}
