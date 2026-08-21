using System;
using System.Data.Common;
using PrimeERP.Database;
using Db = PrimeERP.Core.Database.DbHelper;

namespace PrimeERP.Services
{
    /// <summary>
    /// يولّد أرقاماً متسلسلة فريدة لكل مفتاح (قيود يومية، فواتير...) بصيغة Prefix-Year-Number، مع تصفير
    /// سنوي اختياري. منطق "هل نُصفّر لأن السنة تغيّرت؟" هنا (الخدمة)، لا في NumberSequenceRepository —
    /// الأخيرة SQL خام ↔ صف فقط. أول استخدام لأي مفتاح ينشئ صفّه تلقائياً بإعدادات افتراضية
    /// (البادئة = المفتاح نفسه، 5 خانات، تصفير سنوي) — تُعدَّل لاحقاً عبر شاشة إعدادات عند الحاجة.
    /// </summary>
    public class NumberSequenceService : INumberSequenceService
    {
        public static readonly NumberSequenceService Instance = new();

        public string Peek(string key)
        {
            NumberSequenceRepository.EnsureRow(key);
            var row = NumberSequenceRepository.GetRow(key);
            return Format(row.Prefix, DateTime.Now.Year, row.NextNumber, row.Padding);
        }

        public string Next(string key)
        {
            NumberSequenceRepository.EnsureRow(key);

            return Db.RunTransaction((conn, tx) =>
            {
                var row = NumberSequenceRepository.GetRow(conn, tx, key);
                var year = DateTime.Now.Year;
                var number = row.ResetYearly && row.LastYear != year ? 1 : row.NextNumber;

                NumberSequenceRepository.UpdateNext(conn, tx, key, number + 1, year);

                return Format(row.Prefix, year, number, row.Padding);
            });
        }

        public string Next(DbConnection conn, DbTransaction tx, string key)
        {
            NumberSequenceRepository.EnsureRow(conn, tx, key);

            var row = NumberSequenceRepository.GetRow(conn, tx, key);
            var year = DateTime.Now.Year;
            var number = row.ResetYearly && row.LastYear != year ? 1 : row.NextNumber;

            NumberSequenceRepository.UpdateNext(conn, tx, key, number + 1, year);

            return Format(row.Prefix, year, number, row.Padding);
        }

        private static string Format(string prefix, int year, int number, int padding) =>
            $"{prefix}-{year}-{number.ToString("D" + padding)}";
    }
}
