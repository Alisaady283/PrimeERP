using System.Collections.Generic;
using System.Linq;
using PrimeERP.Composition.Definitions;

namespace PrimeERP.Composition.Registry
{
    public class ModuleRegistry : IModuleRegistry
    {
        private readonly Dictionary<string, ModuleDefinition> _modules = new();

        /// <summary>
        /// بيان النسخة: مفاتيح الوحدات المسموحة. فارغ = النظام كاملاً، وهو الحال الافتراضي. يُضبَط عند
        /// الإقلاع من الإعدادات، فتُتجاهَل كل وحدة خارجه — نسخةٌ مخصّصة بلا بناءٍ ثانٍ.
        /// </summary>
        public IReadOnlyCollection<string> Manifest { get; set; }

        public void Register(ModuleDefinition module)
        {
            if (Manifest != null && !Manifest.Contains(module.Key)) return;

            _modules[module.Key] = module;
        }

        public ModuleDefinition Get(string key) => _modules.TryGetValue(key, out var m) ? m : null;

        public IReadOnlyList<ModuleDefinition> All() => _modules.Values.ToList();

        public IReadOnlyList<ModuleDefinition> VisibleFor(bool simplifiedFlow) =>
            _modules.Values
                .Where(m => m.FlowScope == FlowScope.Both
                         || (simplifiedFlow ? m.FlowScope == FlowScope.SimplifiedOnly
                                            : m.FlowScope == FlowScope.FullCycleOnly))
                .ToList();
    }
}
