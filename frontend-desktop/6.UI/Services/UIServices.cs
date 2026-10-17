using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.UI.Services
{
    /// <summary>نقطة وصول واحدة لحاوية DI</summary>
    public static class UIServices
    {
        public static IServiceProvider Provider { get; private set; }

        public static void Initialize(IServiceProvider provider) => Provider = provider;

        public static IPermissionService Permissions => Provider.GetRequiredService<IPermissionService>();

        public static PrimeERP.Platform.Design.IIdentityService Identity =>
            Provider?.GetService(typeof(PrimeERP.Platform.Design.IIdentityService)) as PrimeERP.Platform.Design.IIdentityService;
    }
}
