using System;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>تنبيهٌ داخلي يراه من له صلاحية صفحته</summary>
    public class Notification : BaseModel
    {
        public string Event          { get; set; }
        public string Title          { get; set; }
        public string Body           { get; set; }
        public string PageKey        { get; set; }
        public int?   RecordId       { get; set; }
        public string PermissionKey  { get; set; }
        public string Severity       { get; set; } = "info";
        public string DedupKey       { get; set; }   // لا يتكرر التنبيه نفسه في يومه
    }

    /// <summary>قراءة مستخدمٍ لتنبيه</summary>
    public class NotificationRead
    {
        public int Id             { get; set; }
        public int NotificationId { get; set; }
        public int UserId         { get; set; }
    }

    /// <summary>قاعدة: حدثٌ ← قنوات ← مستلمون</summary>
    public class AlertRule : BaseModel
    {
        public string  Code       { get; set; }
        public string  Name       { get; set; }
        public string  Event      { get; set; }
        public string  Channels   { get; set; } = "InApp";   // InApp,Email,WhatsApp,Webhook مفصولةً بفاصلة
        public string  Recipients { get; set; }              // بريد/هاتف/عنوان، مفصولةً بفاصلة
        public decimal Threshold  { get; set; }              // معامل الحدث (أيام الصلاحية، أيام التأخر…)
        public bool    IsActive   { get; set; } = true;
    }

    /// <summary>رسالةٌ صادرة لقناةٍ خارجية — صندوق الصادر وسجلّ الربط معاً</summary>
    public class OutboxMessage : BaseModel
    {
        public int?      RuleId        { get; set; }
        public string    Event         { get; set; }
        public string    Channel       { get; set; }
        public string    Target        { get; set; }
        public string    Subject       { get; set; }
        public string    Body          { get; set; }
        public string    Status        { get; set; } = OutboxStatus.Pending;
        public int       Attempts      { get; set; }
        public DateTime  NextAttemptAt { get; set; }
        public DateTime? SentAt        { get; set; }
        public string    LastError     { get; set; }
        public string    DedupKey      { get; set; }
    }

    /// <summary>مفتاح نظامٍ خارجي: يعمل بصفة مستخدمٍ قائم (دوره وصلاحياته وفرعه)، ولا يُحفظ إلا بصمته</summary>
    public class ApiKey : BaseModel
    {
        public string    Code       { get; set; }
        public string    Name       { get; set; }
        public int       UserId     { get; set; }
        public string    Prefix     { get; set; }   // أول المفتاح للتعرّف عليه
        public string    KeyHash    { get; set; }   // SHA-256 للمفتاح
        public DateTime? ExpiresAt  { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public bool      IsActive   { get; set; } = true;
    }

    /// <summary>طلب دفعٍ إلكتروني لفاتورة: رابطٌ من البوابة، وتأكيدها يُنشئ سند القبض باسم من طلبه</summary>
    public class PaymentRequest : BaseModel
    {
        public string    Provider    { get; set; }
        public int       InvoiceId   { get; set; }
        public string    InvoiceNo   { get; set; }
        public int       CustomerId  { get; set; }
        public decimal   Amount      { get; set; }
        public string    Currency    { get; set; }
        public string    Status      { get; set; } = PaymentStatus.Pending;
        public string    Reference   { get; set; }   // مرجع البوابة
        public string    Url         { get; set; }
        public int       RequestedBy { get; set; }
        public DateTime? PaidAt      { get; set; }
        public int?      VoucherId   { get; set; }
        public string    LastError   { get; set; }
    }

    /// <summary>تقديم فاتورةٍ للجهة الضريبية: ما أُرسل وما رُدّ، والرمز الذي يُطبع</summary>
    public class EInvoiceSubmission : BaseModel
    {
        public int       InvoiceId    { get; set; }
        public string    InvoiceNo    { get; set; }
        public string    Provider     { get; set; }
        public string    Status       { get; set; } = EInvoiceStatus.Pending;
        public string    Uuid         { get; set; }
        public string    SubmissionId { get; set; }
        public string    LongId       { get; set; }
        public string    Qr           { get; set; }
        public string    LastError    { get; set; }
        public DateTime? SubmittedAt  { get; set; }
    }

    public static class EInvoiceStatus
    {
        public const string Pending = "Pending", Accepted = "Accepted", Rejected = "Rejected";
    }

    public static class PaymentStatus
    {
        public const string Pending = "Pending", Paid = "Paid", Failed = "Failed";
    }

    public static class OutboxStatus
    {
        public const string Pending = "Pending", Sending = "Sending", Sent = "Sent", Failed = "Failed";
    }
}
