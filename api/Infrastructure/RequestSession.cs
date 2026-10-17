using Microsoft.Extensions.Caching.Memory;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Api.Infrastructure
{
    /// <summary>لكل طلبٍ مستخدمه ولغته: AppSession و LocalizationService في نطاق الطلب وحده، والصلاحيات من القاعدة بذاكرةٍ قصيرة</summary>
    public sealed class RequestSession
    {
        private static readonly TimeSpan PermissionsLifetime = TimeSpan.FromSeconds(60);

        private readonly RequestDelegate _next;

        public RequestSession(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, IPermissionService permissions, IMemoryCache cache)
        {
            using var session = AppSession.Enter();
            using var language = LocalizationService.Enter(LanguageOf(context.Request));

            var user = context.User;
            if (user.Identity?.IsAuthenticated == true && int.TryParse(user.FindFirst(SessionClaims.UserId)?.Value, out var userId))
            {
                var granted = cache.GetOrCreate(("permissions", userId), entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = PermissionsLifetime;
                    return permissions.GetUserPermissions(userId).ToArray();
                });

                AppSession.SignIn(userId,
                    user.FindFirst(SessionClaims.Username)?.Value,
                    user.FindFirst(SessionClaims.DisplayName)?.Value,
                    int.TryParse(user.FindFirst(SessionClaims.RoleId)?.Value, out var roleId) ? roleId : 0,
                    user.FindFirst(SessionClaims.RoleName)?.Value,
                    granted,
                    int.TryParse(user.FindFirst(SessionClaims.BranchId)?.Value, out var branchId) ? branchId : null);
            }

            await _next(context);
        }

        private static AppLanguage LanguageOf(HttpRequest request) =>
            request.Headers.AcceptLanguage.ToString().StartsWith("en", StringComparison.OrdinalIgnoreCase) ? AppLanguage.En : AppLanguage.Ar;
    }
}
