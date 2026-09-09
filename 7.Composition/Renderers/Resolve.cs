using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>
    /// حلّ نموذج العرض والخدمة: بالمصنع إن أُعلن، وإلا بالنوع كما كان. موضعٌ واحد يستورده كل مُصيِّر —
    /// كانت سبعة مواضع تستدعي GetRequiredService بالنوع مباشرةً، فلا مكان لوحدةٍ لا يكفيها نوعها.
    /// </summary>
    internal static class Resolve
    {
        internal static object ViewModel(ModuleDefinition definition, IServiceProvider services) =>
            definition.ViewModelFactory != null
                ? definition.ViewModelFactory(services)
                : services.GetRequiredService(definition.ViewModelType);

        internal static object Service(DialogDefinition dialog, IServiceProvider services) =>
            dialog.ServiceFactory != null ? dialog.ServiceFactory(services) : services.GetRequiredService(dialog.ServiceType);

        internal static object Service(DocumentDialogDefinition document, IServiceProvider services) =>
            document.ServiceFactory != null ? document.ServiceFactory(services) : services.GetRequiredService(document.ServiceType);
    }
}
