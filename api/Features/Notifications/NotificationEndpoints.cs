using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PrimeERP.Api.Infrastructure;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Application.Services.Integration;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Api.Features.Notifications
{
    public sealed record AlertEventOption(string Key, string Title);

    /// <summary>
    /// قناة الإشعار اللحظي: لا تحمل بيانات، «changed» فقط — والعميل يقرأ تنبيهاته بصلاحياته من /api/notifications،
    /// فلا يصل أحداً ما لا يحقّ له. الرمز في access_token لأن WebSocket لا يحمل ترويسة Authorization.
    /// </summary>
    [Authorize]
    public sealed class NotificationsHub : Hub { }

    public sealed class SignalRPush : INotificationPush
    {
        private readonly IHubContext<NotificationsHub> _hub;
        public SignalRPush(IHubContext<NotificationsHub> hub) => _hub = hub;
        public void Notify() => _ = _hub.Clients.All.SendAsync("changed");
    }

    public static class NotificationEndpoints
    {
        public const string HubPath = "/hubs/notifications";

        public static RouteGroupBuilder MapNotifications(this RouteGroupBuilder api)
        {
            var notifications = api.MapGroup("/notifications").WithTags("Notifications").RequireAuthorization();

            notifications.MapGet("/", (INotificationService service, int? take) => service.Mine(take ?? 30).ToHttp(items => items));
            notifications.MapGet("/count", (INotificationService service) => service.UnreadCount().ToHttp(count => count));
            notifications.MapPost("/read", (INotificationService service, List<int> ids) => service.MarkRead(ids).ToHttp());

            // أحداث القواعد لقائمة الاختيار في صفحة قواعد التنبيه
            api.MapGet("/alerts/events", (IPermissionService access) =>
                    !access.Can($"{PermissionKeys.GateOf("AlertRules", "AlertRules")}.View") ? Results.StatusCode(StatusCodes.Status403Forbidden)
                    : Results.Ok(AlertEvents.All().Select(e => new AlertEventOption(e.Key, e.Title)).ToList()))
                .WithTags("Notifications").RequireAuthorization();

            // مفتاح ربطٍ جديد للسجل: نصّه مرةً واحدة
            api.MapPost("/api-keys/{id:int}/issue", (int id, IApiKeyService keys) => keys.Issue(id).ToHttp(key => new { key }))
                .WithTags("Integration").RequireAuthorization();

            // الفاتورة الإلكترونية: تقديمٌ للجهة المفعّلة، والرد يُحفظ ويُعاد معرّفه
            api.MapPost("/einvoices/invoices/{id:int}/submit", async (int id, IEInvoiceService einvoices) =>
                    (await einvoices.SubmitAsync(id)).ToHttp(s => new { url = s.Qr ?? s.Uuid, s.Uuid, s.Status }))
                .WithTags("EInvoice").RequireAuthorization();

            // الدفع الإلكتروني: رابطٌ للفاتورة، وإشعار البوابة بلا جلسة يُقبل بتوقيعه وحده
            api.MapPost("/payments/invoices/{id:int}/link", async (int id, decimal? amount, IPaymentLinkService payments) =>
                    (await payments.RequestAsync(id, amount)).ToHttp(link => link))
                .WithTags("Payments").RequireAuthorization();

            api.MapPost("/payments/{provider}/callback", async (string provider, HttpRequest request, IPaymentLinkService payments) =>
                {
                    using var reader = new StreamReader(request.Body);
                    var body = await reader.ReadToEndAsync();
                    return payments.Confirm(provider, body,
                        request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                        request.Query.ToDictionary(q => q.Key, q => q.Value.ToString())).IsSuccess ? Results.Ok() : Results.Unauthorized();
                })
                .WithTags("Payments").AllowAnonymous();

            return api;
        }
    }
}
