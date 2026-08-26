using System.Collections.Generic;
using System.Linq;
using PrimeERP.Composition.Definitions;

namespace PrimeERP.Composition.Registry
{
    public class ModuleRegistry : IModuleRegistry
    {
        private readonly Dictionary<string, ModuleDefinition> _modules = new();

        public void Register(ModuleDefinition module) => _modules[module.Key] = module;

        public ModuleDefinition Get(string key) => _modules.TryGetValue(key, out var m) ? m : null;

        public IReadOnlyList<ModuleDefinition> All() => _modules.Values.ToList();
    }
}
