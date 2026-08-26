using System.Collections.Generic;
using PrimeERP.Composition.Definitions;

namespace PrimeERP.Composition.Registry
{
    /// <summary>سجل الوحدات المُفعَّلة — 8.Modules يسجّل، 6.UI (AppSidebar/التنقل) يقرأ فقط عبر المفتاح.</summary>
    public interface IModuleRegistry
    {
        void Register(ModuleDefinition module);
        ModuleDefinition Get(string key);
        IReadOnlyList<ModuleDefinition> All();
    }
}
