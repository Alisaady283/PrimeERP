using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace PrimeERP.Platform.Audit
{
    /// <summary>سجلّ التدقيق بقيمة قبل وبعد</summary>
    public class AuditLogger : IAuditLogger
    {
        private readonly IAuditStore _store;

        public AuditLogger(IAuditStore store) => _store = store;

        private static readonly HashSet<string> IgnoredColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash", "Salt", "RowVersion"
        };

        private const int MaxJsonLength = 4000;

        private static readonly string FallbackLogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "audit-fallback.log");

        public void Log(string tableName, int recordId, AuditAction action,
                               object oldValue = null, object newValue = null, string details = null)
        {
            try
            {
                _store.Insert(new AuditEntry
                {
                    TableName = tableName,
                    RecordId  = recordId,
                    Action    = ((int)action).ToString(),
                    OldValues = SerializeSafe(oldValue) ?? details,
                    NewValues = SerializeSafe(newValue),
                    UserId    = AppSession.UserId == 0 ? null : AppSession.UserId,
                    UserName  = AppSession.Username ?? "Admin",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                WriteFallback(tableName, recordId, action, ex);
            }
        }

        private static string SerializeSafe(object value)
        {
            if (value == null) return null;

            try
            {
                var filtered = FilterIgnoredProperties(value);
                var json = JsonSerializer.Serialize(filtered);

                if (json.Length > MaxJsonLength)
                    json = json.Substring(0, MaxJsonLength) + "…[مقتطع]";

                return json;
            }
            catch
            {
                return null;
            }
        }

        private static Dictionary<string, object> FilterIgnoredProperties(object value)
        {
            var dict = new Dictionary<string, object>();

            foreach (var prop in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IgnoredColumns.Contains(prop.Name)) continue;
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;

                try { dict[prop.Name] = prop.GetValue(value); }
                catch { /* تجاهل أي خاصية يفشل قراءتها — لا نوقف بقية التسجيل */ }
            }

            return dict;
        }

        private void WriteFallback(string tableName, int recordId, AuditAction action, Exception ex)
        {
            try
            {
                var dir = Path.GetDirectoryName(FallbackLogPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] فشل تسجيل Audit — " +
                           $"Table={tableName} RecordId={recordId} Action={action} Error={ex.Message}{Environment.NewLine}";
                File.AppendAllText(FallbackLogPath, line);
            }
            catch
            {
            }
        }
    }
}
