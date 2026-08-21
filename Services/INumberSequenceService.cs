using System.Data.Common;

namespace PrimeERP.Services
{
    public interface INumberSequenceService
    {
        /// <summary>يستهلك الرقم التالي للمفتاح ويحفظه — مثال: "JE-2026-00001".</summary>
        string Next(string key);

        /// <summary>نفس Next أعلاه لكن عبر (conn,tx) خدمة مستدعية — تستخدمه JournalService.Create(conn,tx,...) (يخدم FiscalPeriodService.CloseYear) لتوليد رقم القيد بلا فتح اتصال/معاملة جديدة تُعلِّق (deadlock) على SQLite ضمن معاملة خارجية قائمة.</summary>
        string Next(DbConnection conn, DbTransaction tx, string key);

        /// <summary>يعرض الرقم التالي بلا استهلاكه (للعرض قبل الحفظ الفعلي).</summary>
        string Peek(string key);
    }
}
