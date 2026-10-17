using System;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Application.Services.Integration
{
    /// <summary>
    /// كل حفظٍ يمرّ بسجلّ التدقيق، فهو مصدر أحداث الصفحات: «&lt;الصفحة&gt;.Insert/Update/Delete».
    /// يُكتب التدقيق أولاً، والتنبيه لا يُفشل الحفظ أبداً.
    /// </summary>
    public sealed class AlertingAuditLogger : IAuditLogger
    {
        private readonly IAuditLogger _inner;
        private readonly Lazy<Alerts> _alerts;

        public AlertingAuditLogger(IAuditLogger inner, Lazy<Alerts> alerts)
        {
            _inner = inner;
            _alerts = alerts;
        }

        public void Log(string tableName, int recordId, AuditAction action, object oldValue = null, object newValue = null, string details = null)
        {
            _inner.Log(tableName, recordId, action, oldValue, newValue, details);

            try
            {
                var @event = $"{tableName}.{action}";
                if (!_alerts.Value.Wanted(@event)) return;

                _alerts.Value.Raise(new AlertMessage(@event, AlertEvents.TitleOf(@event),
                    LocalizationService.Get("Str.Alert.ByUser", recordId, AppSession.DisplayName ?? AppSession.Username ?? ""),
                    PageKey: tableName, RecordId: recordId, PermissionKey: $"{PermissionKeys.GateOf(tableName, tableName)}.View"));
            }
            catch (Exception ex)
            {
                _inner.Log("AlertRules", 0, AuditAction.Update, details: ex.Message);
            }
        }
    }
}
