using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface INotificationRepository
    {
        int Insert(Notification notification);
        bool Exists(string dedupKey);
        /// <summary>تنبيهات من يحمل إحدى الصلاحيات، الأحدث أولاً، بحالة قراءته</summary>
        List<(Notification Item, bool IsRead)> For(int userId, IReadOnlyCollection<string> permissions, int take);
        int UnreadCount(int userId, IReadOnlyCollection<string> permissions);
        void MarkRead(int userId, IEnumerable<int> ids);
    }

    public class NotificationRepository : RepositoryBase<Notification>, INotificationRepository
    {
        protected override string TableName => "Notifications";

        public int Insert(Notification notification) => Add(notification);

        public bool Exists(string dedupKey) => Any(q => q.Where(n => n.DedupKey == dedupKey));

        public List<(Notification Item, bool IsRead)> For(int userId, IReadOnlyCollection<string> permissions, int take) =>
            Scope(null, db =>
            {
                var read = RowsOf<NotificationRead>(db, "NotificationReads").Where(r => r.UserId == userId).Select(r => r.NotificationId);
                return Visible(Rows(db), permissions)
                    .OrderByDescending(n => n.Id).Take(take)
                    .Select(n => new { n, IsRead = read.Contains(n.Id) })
                    .AsNoTracking().ToList()
                    .Select(x => (x.n, x.IsRead)).ToList();
            });

        public int UnreadCount(int userId, IReadOnlyCollection<string> permissions) =>
            Scope(null, db =>
            {
                var read = RowsOf<NotificationRead>(db, "NotificationReads").Where(r => r.UserId == userId).Select(r => r.NotificationId);
                return Visible(Rows(db), permissions).Count(n => !read.Contains(n.Id));
            });

        public void MarkRead(int userId, IEnumerable<int> ids) =>
            Write(db =>
            {
                var wanted = ids.Distinct().ToList();
                var done = RowsOf<NotificationRead>(db, "NotificationReads")
                    .Where(r => r.UserId == userId && wanted.Contains(r.NotificationId)).Select(r => r.NotificationId).ToHashSet();
                SetOf<NotificationRead>(db, "NotificationReads").AddRange(wanted.Where(id => !done.Contains(id))
                    .Select(id => new NotificationRead { NotificationId = id, UserId = userId }));
                return 0;
            });

        private static IQueryable<Notification> Visible(IQueryable<Notification> rows, IReadOnlyCollection<string> permissions) =>
            permissions == null ? rows : rows.Where(n => n.PermissionKey == null || permissions.Contains(n.PermissionKey));
    }

    public interface IApiKeyRepository
    {
        ApiKey ByHash(string hash);
        void SetKey(int id, string prefix, string hash);
        void Touch(int id, DateTime at);
    }

    public class ApiKeyRepository : RepositoryBase<ApiKey>, IApiKeyRepository
    {
        protected override string TableName => "ApiKeys";

        public ApiKey ByHash(string hash) => One(q => q.Where(k => k.KeyHash == hash && k.IsActive));

        public void SetKey(int id, string prefix, string hash) =>
            Set(k => k.Id == id, s => s.SetProperty(k => k.Prefix, prefix).SetProperty(k => k.KeyHash, hash));

        public void Touch(int id, DateTime at) => Set(k => k.Id == id, s => s.SetProperty(k => k.LastUsedAt, at));
    }

    public interface IPaymentRepository
    {
        int Insert(PaymentRequest payment);
        PaymentRequest GetById(int id, PrimeDbContext db = null);
        List<PaymentRequest> OfInvoice(int invoiceId);
        void SetLink(int id, string url, string reference);
        /// <summary>يُعلّمه مدفوعاً مرةً واحدة: false إن سبق</summary>
        bool MarkPaid(int id, DateTime at, string reference);
        void SetVoucher(int id, int voucherId);
        void Fail(int id, string error);
    }

    public class PaymentRepository : RepositoryBase<PaymentRequest>, IPaymentRepository
    {
        protected override string TableName => "PaymentRequests";

        public int Insert(PaymentRequest payment) => Add(payment);

        public List<PaymentRequest> OfInvoice(int invoiceId) => Fetch(q => q.Where(p => p.InvoiceId == invoiceId).OrderByDescending(p => p.Id));

        public void SetLink(int id, string url, string reference) =>
            Set(p => p.Id == id, s => s.SetProperty(p => p.Url, url).SetProperty(p => p.Reference, reference));

        public bool MarkPaid(int id, DateTime at, string reference) =>
            Set(p => p.Id == id && p.Status != PaymentStatus.Paid, s => s.SetProperty(p => p.Status, PaymentStatus.Paid)
                .SetProperty(p => p.PaidAt, at).SetProperty(p => p.Reference, p => reference ?? p.Reference)) == 1;

        public void SetVoucher(int id, int voucherId) => Set(p => p.Id == id, s => s.SetProperty(p => p.VoucherId, voucherId));

        public void Fail(int id, string error) => Set(p => p.Id == id, s => s.SetProperty(p => p.LastError, error));
    }

    public interface IEInvoiceRepository
    {
        int Insert(EInvoiceSubmission submission);
        /// <summary>آخر تقديمٍ للفاتورة</summary>
        EInvoiceSubmission Latest(int invoiceId);
        void Update(EInvoiceSubmission submission);
    }

    public class EInvoiceRepository : RepositoryBase<EInvoiceSubmission>, IEInvoiceRepository
    {
        protected override string TableName => "EInvoiceSubmissions";

        public int Insert(EInvoiceSubmission submission) => Add(submission);

        public EInvoiceSubmission Latest(int invoiceId) => One(q => q.Where(s => s.InvoiceId == invoiceId).OrderByDescending(s => s.Id));

        public void Update(EInvoiceSubmission submission) => Modify(submission);
    }

    public interface IOutboxRepository
    {
        int Enqueue(OutboxMessage message);
        bool Exists(string dedupKey);
        /// <summary>يحجز المستحقّ بجملةٍ واحدة فلا يرسله مُرسِلان</summary>
        List<OutboxMessage> Claim(DateTime now, int take);
        void Sent(int id, DateTime at);
        void Failed(int id, string error, DateTime? retryAt);
        (List<OutboxMessage> Items, int Total) GetPaged(int page, int pageSize, string status);
    }

    public class OutboxRepository : RepositoryBase<OutboxMessage>, IOutboxRepository
    {
        protected override string TableName => "OutboxMessages";

        public int Enqueue(OutboxMessage message) => Add(message);

        public bool Exists(string dedupKey) => Any(q => q.Where(m => m.DedupKey == dedupKey));

        /// <summary>المستحقّ، أو المحجوز الذي انقضت مهلته (مُرسِلٌ أُغلق في منتصف الإرسال)</summary>
        public List<OutboxMessage> Claim(DateTime now, int take)
        {
            var lease = now.AddMinutes(10);
            var due = Fetch(q => q.Where(m => (m.Status == OutboxStatus.Pending || m.Status == OutboxStatus.Sending) && m.NextAttemptAt <= now)
                .OrderBy(m => m.Id).Take(take));
            return due.Where(m => Set(x => x.Id == m.Id && x.Status == m.Status && x.NextAttemptAt == m.NextAttemptAt,
                s => s.SetProperty(x => x.Status, OutboxStatus.Sending).SetProperty(x => x.NextAttemptAt, lease)) == 1).ToList();
        }

        public void Sent(int id, DateTime at) =>
            Set(m => m.Id == id, s => s.SetProperty(m => m.Status, OutboxStatus.Sent).SetProperty(m => m.SentAt, at)
                .SetProperty(m => m.Attempts, m => m.Attempts + 1).SetProperty(m => m.LastError, (string)null));

        public void Failed(int id, string error, DateTime? retryAt) =>
            Set(m => m.Id == id, s => s.SetProperty(m => m.Status, retryAt == null ? OutboxStatus.Failed : OutboxStatus.Pending)
                .SetProperty(m => m.NextAttemptAt, m => retryAt ?? m.NextAttemptAt)
                .SetProperty(m => m.Attempts, m => m.Attempts + 1).SetProperty(m => m.LastError, error));

        public (List<OutboxMessage> Items, int Total) GetPaged(int page, int pageSize, string status) =>
            Page(page, pageSize, q => string.IsNullOrEmpty(status) ? q : q.Where(m => m.Status == status), q => q.OrderByDescending(m => m.Id));
    }
}
