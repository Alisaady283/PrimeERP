using System;
using System.Collections.Generic;
using System.Data;
using PrimeERP.Data.Core;

namespace PrimeERP.Data.Migrations
{
    // بذور SeedDefaults القديمة (1000/1100/1210...) → الصيغة الرسمية القصيرة (1/11/1201...). أي كود آخر
    // (مولَّد فعلياً عبر BuildChildCode بعد البذور) يُعاد ترقيمه بنفس القاعدة: بادئة الأب الجديدة + لاحقته
    // القديمة كما هي، بلا حاجة لخريطة يدوية له.
    public static class AccountCodeRenumberMigration
    {
        private static readonly Dictionary<string, string> SeedRenameMap = new()
        {
            ["1000"] = "1",  ["1100"] = "11",  ["1110"] = "1101", ["1200"] = "12",
            ["1210"] = "1201", ["1220"] = "1202", ["1230"] = "1203", ["1240"] = "1204",
            ["2000"] = "2",  ["2100"] = "21",  ["2110"] = "2101", ["2200"] = "22",
            ["3000"] = "3",  ["3100"] = "31",  ["3200"] = "32",
            ["4000"] = "4",  ["4100"] = "41",  ["4200"] = "42",
            ["5000"] = "5",  ["5100"] = "51",  ["5200"] = "52",
        };

        public static void Apply()
        {
            var accounts = DbHelper.Query("SELECT Id, Code, ParentCode FROM Accounts ORDER BY Level, Code");
            if (accounts.Rows.Count == 0) return;

            var newCodeOf = new Dictionary<string, string>();
            foreach (DataRow row in accounts.Rows)
            {
                var oldCode = row["Code"].ToString();
                var oldParent = row["ParentCode"] == DBNull.Value ? null : row["ParentCode"].ToString();

                if (SeedRenameMap.TryGetValue(oldCode, out var seeded))
                {
                    newCodeOf[oldCode] = seeded;
                    continue;
                }

                var newParent = oldParent != null && newCodeOf.TryGetValue(oldParent, out var np) ? np : oldParent;
                var suffix = oldParent != null && oldCode.StartsWith(oldParent) ? oldCode.Substring(oldParent.Length) : oldCode;
                newCodeOf[oldCode] = (newParent ?? "") + suffix;
            }

            DbHelper.RunTransaction((conn, tx) =>
            {
                // مرحلة مؤقتة: تفادي أي تصادم UNIQUE عابر بين كود جديد وكود قديم لم يُعاد ترقيمه بعد.
                foreach (DataRow row in accounts.Rows)
                {
                    using var cmd = DbHelper.CreateCommand(conn, tx, "UPDATE Accounts SET Code=@c WHERE Id=@id",
                        DbHelper.Params(("@c", "~mig~" + row["Id"]), ("@id", row["Id"])));
                    cmd.ExecuteNonQuery();
                }

                foreach (DataRow row in accounts.Rows)
                {
                    var oldCode = row["Code"].ToString();
                    var oldParent = row["ParentCode"] == DBNull.Value ? null : row["ParentCode"].ToString();
                    var newCode = newCodeOf[oldCode];
                    var newParent = oldParent == null ? null : newCodeOf[oldParent];

                    using var cmd = DbHelper.CreateCommand(conn, tx, "UPDATE Accounts SET Code=@c, ParentCode=@p WHERE Id=@id",
                        DbHelper.Params(("@c", newCode), ("@p", (object)newParent ?? DBNull.Value), ("@id", row["Id"])));
                    cmd.ExecuteNonQuery();
                }

                foreach (var kv in newCodeOf)
                {
                    if (kv.Key == kv.Value) continue;

                    foreach (var table in new[] { "Customers", "Suppliers", "JournalEntryLines" })
                    {
                        using var cmd = DbHelper.CreateCommand(conn, tx, $"UPDATE {table} SET AccountCode=@n WHERE AccountCode=@o",
                            DbHelper.Params(("@n", kv.Value), ("@o", kv.Key)));
                        cmd.ExecuteNonQuery();
                    }

                    // إعدادات النظام (Accounts.Customers، إلخ) — InsertIfMissing لا تُحدِّث قيماً مزروعة سابقاً بالكود القديم.
                    using var settingCmd = DbHelper.CreateCommand(conn, tx, "UPDATE AppSettings SET Value=@n WHERE Category='Accounts' AND Value=@o",
                        DbHelper.Params(("@n", kv.Value), ("@o", kv.Key)));
                    settingCmd.ExecuteNonQuery();
                }
            });
        }
    }
}
