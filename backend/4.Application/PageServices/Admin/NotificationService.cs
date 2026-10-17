using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Application.PageServices.Admin
{
    public sealed record NotificationItem(int Id, string Event, string Title, string Body, string PageKey, int? RecordId,
        string Severity, DateTime CreatedAt, bool IsRead);

    public interface INotificationService
    {
        Result<List<NotificationItem>> Mine(int take = 30);
        Result<int> UnreadCount();
        Result MarkRead(IEnumerable<int> ids);
    }

    /// <summary>تنبيهات المستخدم: ما تسمح به صلاحياته وحدها، وقراءته له وحده</summary>
    public sealed class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notifications;

        public NotificationService(INotificationRepository notifications) => _notifications = notifications;

        public Result<List<NotificationItem>> Mine(int take = 30) =>
            !AppSession.IsAuthenticated && !AppSession.BypassPermissions ? Result.Ok(new List<NotificationItem>()) :
            Result.Ok(_notifications.For(AppSession.UserId, Scope(), Math.Clamp(take, 1, 100))
                .Select(n => new NotificationItem(n.Item.Id, n.Item.Event, n.Item.Title, n.Item.Body, n.Item.PageKey, n.Item.RecordId,
                    n.Item.Severity, n.Item.CreatedAt, n.IsRead)).ToList());

        public Result<int> UnreadCount() =>
            Result.Ok(!AppSession.IsAuthenticated && !AppSession.BypassPermissions ? 0 : _notifications.UnreadCount(AppSession.UserId, Scope()));

        public Result MarkRead(IEnumerable<int> ids)
        {
            _notifications.MarkRead(AppSession.UserId, ids ?? Enumerable.Empty<int>());
            return Result.Ok();
        }

        /// <summary>null للمطوّر بلا قيود، وإلا صلاحياته</summary>
        private static IReadOnlyCollection<string> Scope() => AppSession.BypassPermissions ? null : AppSession.Permissions;
    }
}
