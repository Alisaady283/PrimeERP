using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Net;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Platform
{
    /// <summary>طھط³ط¬ظٹظ„ ط®ط¯ظ…ط§طھ ط§ظ„ظ…ظ†طµظ‘ط© â€” ظٹط³طھط¯ط¹ظٹظ‡ ظƒظ„ ظ…ط¶ظٹظپ: ط³ط·ط­ ط§ظ„ظ…ظƒطھط¨ ظˆط§ظ„ط®ط§ط¯ظ…</summary>
    public static class PlatformServices
    {
        public static IServiceCollection AddPlatform(this IServiceCollection services)
        {
            services.AddSingleton<PermissionService>();
            services.AddSingleton<IPermissionService>(sp => sp.GetRequiredService<PermissionService>());
            services.AddSingleton<IPermissionAdminService>(sp => sp.GetRequiredService<PermissionService>());
            services.AddSingleton<ISettingsProvider, SettingsProvider>();
            services.AddSingleton<IAuditLogger, AuditLogger>();
            services.AddSingleton<ILocalizationService, LocalizationAdapter>();
            services.AddSingleton<IHttpGateway, HttpGateway>();
            return services;
        }
    }
}
