using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using PrimeERP.Data.Core;
using PrimeERP.Data.Schema;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Platform.Audit
{
    /// <summary>
    /// سجلّ تدقيق كامل (قيم قديمة/جديدة كـ JSON) — قطعة جديدة أوسع من Core/AuditLogger.cs المبسّطة.
    /// لا يفشل أبداً: أي خطأ أثناء التسجيل يُكتب في ملف احتياطي ولا يوقف العملية الأصلية.
    /// </summary>
    public class AuditLogger : IAuditLogger
    {
        private static readonly HashSet<string> IgnoredColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash", "Salt", "RowVersion"
        };

        private const int MaxJsonLength = 4000;

        private static readonly string FallbackLogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "audit-fallback.log");

        private bool _tableEnsured;

        public void CreateTable()
        {
            SchemaBuilder.Table("AuditLog")
                .Id()
                .Text("TableName", 100, required: true)
                .Int("RecordId", nullable: false)
                .Int("Action", nullable: false)
                .Text("OldValues")
                .Text("NewValues")
                .Int("UserId", nullable: true)
                .Text("UserName", 100)
                .DateCol("Timestamp")
                .Text("IpAddress", 50)
                .Create();
        }

        /// <summary>يسجّل عملية كتابة. oldValue/newValue أي كائن — يُحوَّل لـ JSON بعد تجاهل الأعمدة الحساسة واقتطاعه لو تجاوز الحد.</summary>
        public void Log(string tableName, int recordId, AuditAction action,
                               object oldValue = null, object newValue = null, string details = null)
        {
            try
            {
                EnsureTable();

                var oldJson = SerializeSafe(oldValue) ?? details;
                var newJson = SerializeSafe(newValue);

                Db.Execute(
                    @"INSERT INTO AuditLog (TableName, RecordId, Action, OldValues, NewValues, UserId, UserName, Timestamp)
                      VALUES (@t, @r, @a, @old, @new, @uid, @u, @ts)",
                    Db.Params(
                        ("@t", tableName), ("@r", recordId), ("@a", (int)action),
                        ("@old", oldJson), ("@new", newJson),
                        ("@uid", AppSession.UserId == 0 ? null : (object)AppSession.UserId),
                        ("@u", AppSession.Username ?? "Admin"),
                        ("@ts", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))));
            }
            catch (Exception ex)
            {
                WriteFallback(tableName, recordId, action, ex);
            }
        }

        private void EnsureTable()
        {
            if (_tableEnsured) return;
            CreateTable();
            _tableEnsured = true;
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
                // لو حتى الكتابة في الملف الاحتياطي فشلت — لا نوقف العملية الأصلية بأي حال
            }
        }
    }
}
