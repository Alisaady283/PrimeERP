using System.Collections.Generic;
using PrimeERP.Composition.Definitions;

namespace PrimeERP.Composition.Registry
{
    /// <summary>سجل الوحدات المُفعَّلة</summary>
    public interface IModuleRegistry
    {
        System.Collections.Generic.IReadOnlyCollection<string> Manifest { get; set; }

        void Register(ModuleDefinition module);
        ModuleDefinition Get(string key);
        IReadOnlyList<ModuleDefinition> All();

        IReadOnlyList<ModuleDefinition> VisibleFor(bool simplifiedFlow);
    }
}
