using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace PrimeERP.Tests.Architecture
{
    /// <summary>
    /// نسخة انعكاسية (IL مُصرَّف فعلياً) من فحوصات حدود الطبقات في Tools/ArchitectureCheck/check.sh § 1 —
    /// تكمّلها لا تكررها: check.sh يفحص نص المصدر (using) فيفلت منه أي إشارة بالاسم الكامل المؤهَّل بلا
    /// using مطابق، بينما هذا يفحص التوقيعات المُصرَّفة فعلياً (قاعدة/واجهات/حقول/خصائص/دوال) — لا يمكن أن
    /// يفلت منه خرق حقيقي وصل للـIL بغض النظر عن أسلوب كتابته في المصدر.
    /// </summary>
    public class LayerBoundaryTests
    {
        private static readonly Assembly MainAssembly = typeof(PrimeERP.Domain.Entities.Account).Assembly;

        [Fact]
        public void Data_DoesNotReferenceApplicationOrHigherLayers() =>
            AssertNoViolations("PrimeERP.Data", ns =>
                ns.StartsWith("PrimeERP.Application") || ns.StartsWith("PrimeERP.Design") ||
                ns.StartsWith("PrimeERP.UI") || ns.StartsWith("PrimeERP.Composition") || ns.StartsWith("PrimeERP.Modules"));

        [Fact]
        public void Application_DoesNotReferenceDesignOrHigherLayers() =>
            AssertNoViolations("PrimeERP.Application", ns =>
                ns.StartsWith("PrimeERP.Design") || ns.StartsWith("PrimeERP.UI") ||
                ns.StartsWith("PrimeERP.Composition") || ns.StartsWith("PrimeERP.Modules"));

        [Fact]
        public void UI_DoesNotSkipToDataOrReferenceHigherLayers() =>
            AssertNoViolations("PrimeERP.UI", ns =>
                ns.StartsWith("PrimeERP.Data") || ns.StartsWith("PrimeERP.Composition") || ns.StartsWith("PrimeERP.Modules"));

        [Fact]
        public void Composition_DoesNotReferenceModules() =>
            AssertNoViolations("PrimeERP.Composition", ns => ns.StartsWith("PrimeERP.Modules"));

        [Fact]
        public void Platform_OnlyReferencesDataCoreOrSchemaAndDomain() =>
            AssertNoViolations("PrimeERP.Platform", ns =>
                ns.StartsWith("PrimeERP.Application") || ns.StartsWith("PrimeERP.Design") ||
                ns.StartsWith("PrimeERP.UI") || ns.StartsWith("PrimeERP.Composition") || ns.StartsWith("PrimeERP.Modules") ||
                (ns.StartsWith("PrimeERP.Data") && !ns.StartsWith("PrimeERP.Data.Core") && !ns.StartsWith("PrimeERP.Data.Schema")));

        [Fact]
        public void Domain_IsPure() =>
            AssertNoViolations("PrimeERP.Domain", ns => ns.StartsWith("PrimeERP.") && !ns.StartsWith("PrimeERP.Domain"));

        private static void AssertNoViolations(string layerNamespacePrefix, Func<string, bool> isForbiddenNamespace)
        {
            var violations = new List<string>();

            var types = MainAssembly.GetTypes()
                .Where(t => t.Namespace != null && t.Namespace.StartsWith(layerNamespacePrefix))
                .Where(t => !t.Name.Contains('<')); // يستبعد الأنواع المُولَّدة آلياً (closures/async state machines)

            foreach (var type in types)
                foreach (var referenced in ReferencedTypes(type))
                    if (referenced.Namespace != null && isForbiddenNamespace(referenced.Namespace))
                        violations.Add($"{type.FullName} -> {referenced.FullName}");

            var distinct = violations.Distinct().OrderBy(v => v).ToList();
            Assert.True(distinct.Count == 0, $"انتهاكات حدود الطبقة {layerNamespacePrefix}:\n" + string.Join("\n", distinct));
        }

        private static IEnumerable<Type> ReferencedTypes(Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            if (type.BaseType != null)
                foreach (var t in Unwrap(type.BaseType)) yield return t;

            foreach (var i in type.GetInterfaces())
                foreach (var t in Unwrap(i)) yield return t;

            foreach (var f in type.GetFields(flags))
                foreach (var t in Unwrap(f.FieldType)) yield return t;

            foreach (var p in type.GetProperties(flags))
                foreach (var t in Unwrap(p.PropertyType)) yield return t;

            foreach (var m in type.GetMethods(flags))
            {
                foreach (var param in m.GetParameters())
                    foreach (var t in Unwrap(param.ParameterType)) yield return t;
                foreach (var t in Unwrap(m.ReturnType)) yield return t;
            }

            foreach (var c in type.GetConstructors(flags))
                foreach (var param in c.GetParameters())
                    foreach (var t in Unwrap(param.ParameterType)) yield return t;
        }

        private static IEnumerable<Type> Unwrap(Type type)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                foreach (var arg in type.GetGenericArguments())
                    foreach (var t in Unwrap(arg)) yield return t;
                yield return type.GetGenericTypeDefinition();
            }
            else if (type.IsArray)
            {
                foreach (var t in Unwrap(type.GetElementType())) yield return t;
            }
            else
            {
                yield return type;
            }
        }
    }
}
